using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Orasyon.Battle
{
    /// <summary>
    /// The 4x4 letter board, Bookworm style: click any tiles in any order to spell a word.
    /// Tiles used in an attack are removed; by default new letters drop into the freed slots right after the
    /// attack (see <see cref="RefillAfterEachWord"/>), so the board is always full. In Classic mode the gaps
    /// stay until no word can be made. Scramble shuffles the tiles on the board.
    /// </summary>
    public sealed class LetterGrid : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private int        rows = 4;
        [SerializeField] private int        cols = 4;
        [SerializeField] private float      cellSize = 92f;
        [SerializeField] private float      spacing  = 8f;
        [SerializeField] private GameObject tilePrefab;

        private LetterTileData[,]             tiles;
        private readonly List<LetterTileData> path = new List<LetterTileData>();
        private readonly System.Random        rng  = new System.Random();

        /// <summary>Fired whenever the selected word or the board changes.</summary>
        public event Action OnSelectionChanged;

        public int Rows => rows;
        public int Cols => cols;

        public const string ClassicRefillPref = "orasyon.classicRefill";

        /// <summary>
        /// True (default): used tiles are replaced with new letters right after each word.
        /// Set the PlayerPrefs int "orasyon.classicRefill" to 1 for the old behavior (gaps until no word can be made).
        /// </summary>
        public static bool RefillAfterEachWord => PlayerPrefs.GetInt(ClassicRefillPref, 0) == 0;

        public string CurrentWord
        {
            get
            {
                var sb = new StringBuilder(path.Count);
                foreach (var t in path) sb.Append(t.Letter);
                return sb.ToString();
            }
        }

        public bool CurrentWordIsValid => path.Count >= WordDictionary.MinLength && WordDictionary.IsWord(CurrentWord);

        public string GetCurrentWord() => CurrentWord;

        /// <summary>Letters of the tiles still on the board.</summary>
        public IEnumerable<char> RemainingLetters
        {
            get
            {
                EnsureTiles();
                foreach (var t in tiles)
                    if (t.IsSelectable) yield return t.Letter;   // locked tiles and holes cannot be used
            }
        }

        public int RemainingCount => RemainingLetters.Count();

        /// <summary>True if at least one valid word can still be made from the remaining tiles.</summary>
        public bool HasAnyWord() => BoardGenerator.CountWords(RemainingLetters, 1) >= 1;

        // ── Board setup ──────────────────────────────────────────────────────
        public void GenerateBoard(string plantWord, int minWords, bool dropIn)
        {
            SetBoard(BoardGenerator.Generate(rows, cols, plantWord, minWords, rng), dropIn);
        }

        public void SetBoard(char[,] letters, bool dropIn)
        {
            EnsureTiles();
            path.Clear();

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    var d = tiles[r, c];
                    d.Letter = letters[r, c];
                    d.IsSelected = false;
                    d.IsHighlighted = false;
                    d.IsEmpty = false;
                    d.Kind = TileKind.Normal;
                    d.TurnsLeft = 0;
                    d.View.Refresh();

                    Vector2 pos = CellPosition(r, c);
                    if (dropIn)
                    {
                        d.View.Place(pos + Vector2.up * (DropHeight + (rows - r) * 40f + c * 25f));
                        d.View.MoveTo(pos);
                    }
                    else
                    {
                        d.View.Place(pos);
                    }
                }

            Changed();
        }

        /// <summary>Board letters, with '\0' for empty gaps.</summary>
        public char[,] GetLetters()
        {
            EnsureTiles();
            var b = new char[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    b[r, c] = tiles[r, c].IsEmpty ? '\0' : tiles[r, c].Letter;
            return b;
        }

        // ── Selection ────────────────────────────────────────────────────────
        public void OnTileClicked(LetterTileData tile)
        {
            if (!tile.IsSelectable) return;   // gaps, holes and locked tiles

            int index = path.IndexOf(tile);
            if (index >= 0)
            {
                // Clicking a selected tile removes it and every letter after it
                for (int i = path.Count - 1; i >= index; i--)
                {
                    path[i].IsSelected = false;
                    path[i].View.Refresh();
                    path.RemoveAt(i);
                }
                Changed();
                return;
            }

            // Any tile, any order
            tile.IsSelected = true;
            path.Add(tile);
            tile.View.Refresh();
            Changed();
        }

        /// <summary>The tiles of the current word, in the order they were picked.</summary>
        public IReadOnlyList<LetterTileData> SelectedTiles => path;

        /// <summary>
        /// Sends one tile of the current word back to the board (tapping it in the word preview). The other
        /// letters stay in the word in their order. Ignored while the board is locked (tutorial, enemy turn).
        /// </summary>
        public void RemoveFromSelection(int pathIndex)
        {
            if (pathIndex < 0 || pathIndex >= path.Count) return;
            var group = GetComponentInParent<CanvasGroup>();
            if (group != null && !group.interactable) return;

            var tile = path[pathIndex];
            path.RemoveAt(pathIndex);
            tile.IsSelected = false;
            tile.View.Refresh();
            tile.View.Pop();
            Changed();
        }

        public void ClearSelection()
        {
            foreach (var t in path)
            {
                t.IsSelected = false;
                t.View.Refresh();
            }
            path.Clear();
            Changed();
        }

        /// <summary>Removes the selected tiles from the board (leaving gaps) and returns the word they spelled.</summary>
        public string ConsumeSelection()
        {
            if (path.Count == 0) return "";
            string word = CurrentWord;

            foreach (var t in path)
            {
                t.IsSelected = false;
                t.IsHighlighted = false;
                t.IsEmpty = true;
                t.Kind = TileKind.Normal;
                t.TurnsLeft = 0;
                t.View.Refresh();
            }
            path.Clear();
            Changed();
            return word;
        }

        /// <summary>Shuffles the letters of the tiles still on the board. Gaps stay where they are.</summary>
        public void Scramble()
        {
            EnsureTiles();
            ClearSelectionSilently();

            var filled = tiles.Cast<LetterTileData>().Where(t => !t.IsEmpty).ToList();
            if (filled.Count < 2) return;

            var original = filled.Select(t => (t.Letter, t.Kind, t.TurnsLeft)).ToList();
            var shuffled = original;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                shuffled = original.OrderBy(_ => rng.Next()).ToList();
                if (!shuffled.SequenceEqual(original)) break;   // make sure something actually moves
            }

            for (int i = 0; i < filled.Count; i++)
            {
                filled[i].Letter = shuffled[i].Letter;
                filled[i].Kind = shuffled[i].Kind;
                filled[i].TurnsLeft = shuffled[i].TurnsLeft;
                filled[i].IsHighlighted = false;
                filled[i].View.Pop();
                filled[i].View.Refresh();
            }
            Changed();
        }

        /// <summary>
        /// Drops new letters into the empty gaps. The finished board is kept playable: a healthy vowel mix,
        /// at most two rare letters, at least one everyday word, and <paramref name="minWords"/> words in total.
        /// </summary>
        public int RefillEmpty(int minWords)
        {
            EnsureTiles();
            var empties = tiles.Cast<LetterTileData>().Where(t => t.IsEmpty && t.Kind != TileKind.Void).ToList();
            LastRefilled.Clear();
            if (empties.Count == 0) return 0;

            var kept = tiles.Cast<LetterTileData>().Where(t => !t.IsEmpty).Select(t => t.Letter).ToList();
            bool found = false;

            // 1) a few fully random fills
            for (int attempt = 0; attempt < 12 && !found; attempt++)
            {
                foreach (var t in empties) t.Letter = BoardGenerator.RandomLetter(rng);
                found = IsGoodFill(empties, minWords);
            }

            // 2) otherwise nudge the new tiles toward a healthy vowel mix and re-check
            for (int attempt = 0; attempt < 40 && !found; attempt++)
            {
                int vowels = kept.Count(BoardGenerator.IsVowel);
                for (int i = 0; i < empties.Count; i++)
                {
                    char c = BoardGenerator.BalancedLetter(rng, vowels, kept.Count + empties.Count);
                    empties[i].Letter = c;
                    if (BoardGenerator.IsVowel(c)) vowels++;
                }
                found = IsGoodFill(empties, minWords);
            }

            int k = 0;
            foreach (var t in empties)
            {
                LastRefilled.Add(t);
                t.IsEmpty = false;
                t.Kind = TileKind.Normal;
                t.TurnsLeft = 0;
                t.IsSelected = false;
                t.IsHighlighted = false;
                t.View.Refresh();
                Vector2 pos = CellPosition(t.Row, t.Col);
                t.View.Place(pos + Vector2.up * (DropHeight + k++ * 30f));
                t.View.MoveTo(pos);
            }
            Changed();
            return empties.Count;
        }

        private bool IsGoodFill(List<LetterTileData> fresh, int minWords)
        {
            var all = tiles.Cast<LetterTileData>().Where(t => !t.IsEmpty || fresh.Contains(t)).Select(t => t.Letter).ToList();
            return BoardGenerator.IsHealthyMix(all)
                && BoardGenerator.HasCommonWord(all)
                && BoardGenerator.CountWords(all, minWords) >= minWords;
        }

        // ── Special and bad tiles ────────────────────────────────────────────
        /// <summary>The tiles that received new letters in the last <see cref="RefillEmpty"/>.</summary>
        public readonly List<LetterTileData> LastRefilled = new List<LetterTileData>();

        public IEnumerable<LetterTileData> AllTiles { get { EnsureTiles(); return tiles.Cast<LetterTileData>(); } }

        public int Index(int row, int col) => row * cols + col;

        public LetterTileData TileAt(int index) { EnsureTiles(); return tiles[index / cols, index % cols]; }

        public LetterTileData FindFirstOfKind(TileKind kind) => AllTiles.FirstOrDefault(t => t.Kind == kind && !t.IsEmpty);

        public int CountKind(TileKind kind) => AllTiles.Count(t => t.Kind == kind);

        /// <summary>Good special tiles currently on the board.</summary>
        public int CountSpecials => AllTiles.Count(t => !t.IsEmpty && TileKindInfo.IsGood(t.Kind));

        /// <summary>Bad tiles on the board (holes from Void are not counted).</summary>
        public int CountBadTiles => AllTiles.Count(t => !t.IsEmpty && TileKindInfo.IsBad(t.Kind));

        /// <summary>Random distinct tile indices (row * cols + col) of tiles passing <paramref name="filter"/>.</summary>
        public List<int> PickTiles(int count, Func<LetterTileData, bool> filter)
        {
            return AllTiles.Where(filter).Select(t => Index(t.Row, t.Col)).OrderBy(_ => rng.Next()).Take(count).ToList();
        }

        /// <summary>
        /// Changes what a tile is. Bad kinds are never placed on a tile that is part of the current word.
        /// <paramref name="turns"/> is how many enemy turns a Smoked / Locked / Hex tile lasts (0 = until cleared).
        /// Void turns the tile into a permanent hole.
        /// </summary>
        public void SetKind(int index, TileKind kind, int turns = 0)
        {
            var t = TileAt(index);
            if (TileKindInfo.IsBad(kind) && t.IsSelected) return;

            if (kind == TileKind.Void)
            {
                t.IsSelected = false;
                t.IsHighlighted = false;
                t.IsEmpty = true;
                t.Kind = TileKind.Void;
                t.TurnsLeft = 0;
            }
            else
            {
                if (t.IsEmpty) return;
                t.Kind = kind;
                t.TurnsLeft = turns;
            }
            t.View.Refresh();
            t.View.Pop();
            Changed();
        }

        /// <summary>Puts a good special tile on a random plain tile. Respects the limit of specials on the board.</summary>
        public bool TryAddSpecial(TileKind kind, bool onlyRefilled = false)
        {
            if (CountSpecials >= TileKindInfo.MaxSpecials) return false;
            var source = onlyRefilled ? (IEnumerable<LetterTileData>)LastRefilled : AllTiles;
            var pool = source.Where(t => !t.IsEmpty && t.Kind == TileKind.Normal && !t.IsSelected && !t.IsHighlighted).ToList();
            if (pool.Count == 0) return false;
            var tile = pool[rng.Next(pool.Count)];
            SetKind(Index(tile.Row, tile.Col), kind);
            return true;
        }

        /// <summary>Call at the end of every enemy turn: Smoked, Locked and Hex tiles count down and wear off.</summary>
        public int TickTileTimers()
        {
            int expired = 0;
            foreach (var t in AllTiles)
            {
                if (t.IsEmpty || t.TurnsLeft <= 0) continue;
                if (t.Kind != TileKind.Smoked && t.Kind != TileKind.Locked && t.Kind != TileKind.Hex) continue;
                t.TurnsLeft--;
                if (t.TurnsLeft <= 0) { t.Kind = TileKind.Normal; expired++; }
                t.View.Refresh();
            }
            if (expired > 0) Changed();
            return expired;
        }

        /// <summary>
        /// Removes bad tiles. The Warding potion clears Smoked and Locked tiles only;
        /// the Orasyon prayer also clears Burning and Hex tiles. Holes (Void) stay until the fight ends.
        /// </summary>
        public int ClearBadTiles(bool smokeAndLockOnly = false)
        {
            int n = 0;
            foreach (var t in AllTiles)
            {
                if (t.IsEmpty) continue;
                bool clear = t.Kind == TileKind.Smoked || t.Kind == TileKind.Locked
                          || (!smokeAndLockOnly && (t.Kind == TileKind.Burning || t.Kind == TileKind.Hex));
                if (!clear) continue;
                t.Kind = TileKind.Normal;
                t.TurnsLeft = 0;
                t.View.Refresh();
                t.View.Pop();
                n++;
            }
            if (n > 0) Changed();
            return n;
        }

        // ── Hints ────────────────────────────────────────────────────────────
        /// <summary>
        /// Picks a word to suggest from the tiles on the board: the longest common word if there is one,
        /// otherwise the longest valid word. Returns null if no word can be made.
        /// </summary>
        public string FindHintWord(int maxLength = 5)
        {
            var words = BoardGenerator.FindWords(RemainingLetters);
            if (words.Count == 0) return null;

            // Hints are a nudge, not a free big attack: everyday words of at most maxLength letters,
            // a random one among the longest of those so the same board does not always give the same hint.
            var common = words.Where(w => w.Length <= maxLength && WordDictionary.IsCommon(w)).ToList();
            if (common.Count > 0)
            {
                int best = common.Max(w => w.Length);
                var top = common.Where(w => w.Length == best).ToList();
                return top[rng.Next(top.Count)];
            }
            return words.Where(w => w.Length <= maxLength).OrderByDescending(w => w.Length).FirstOrDefault();
        }

        /// <summary>Clears the current selection and lights up tiles that spell <paramref name="word"/>.</summary>
        public void HighlightWord(string word)
        {
            EnsureTiles();
            ClearSelectionSilently();
            ClearHighlights();
            var used = new HashSet<LetterTileData>();
            foreach (char ch in word.ToUpperInvariant())
            {
                var tile = tiles.Cast<LetterTileData>().FirstOrDefault(t => t.IsSelectable && t.Letter == ch && !used.Contains(t));
                if (tile == null) continue;
                used.Add(tile);
                tile.IsHighlighted = true;
                tile.View.Pop();
                tile.View.Refresh();
            }
            Changed();
        }

        // ── Tutorial helpers ─────────────────────────────────────────────────
        public void SetHighlight(int row, int col, bool on)
        {
            if (tiles == null) return;
            tiles[row, col].IsHighlighted = on;
            tiles[row, col].View.Refresh();
        }

        public void ClearHighlights()
        {
            if (tiles == null) return;
            foreach (var t in tiles)
            {
                t.IsHighlighted = false;
                t.View.Refresh();
            }
        }

        // ── Internal ─────────────────────────────────────────────────────────
        private float DropHeight => rows * (cellSize + spacing) + 40f;

        private Vector2 CellPosition(int r, int c)
        {
            float step = cellSize + spacing;
            return new Vector2((c - (cols - 1) * 0.5f) * step, ((rows - 1) * 0.5f - r) * step);
        }

        private void ClearSelectionSilently()
        {
            foreach (var t in path)
            {
                t.IsSelected = false;
                t.View.Refresh();
            }
            path.Clear();
        }

        private void EnsureTiles()
        {
            if (tiles != null) return;

            tiles = new LetterTileData[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    var go = Instantiate(tilePrefab, transform);
                    go.name = $"Tile_{r}_{c}";
                    var rt = (RectTransform)go.transform;
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(cellSize, cellSize);

                    var data = new LetterTileData { Row = r, Col = c, Letter = 'A' };
                    go.GetComponent<LetterTile>().Bind(data, this);
                    tiles[r, c] = data;
                }
        }

        private void Changed() => OnSelectionChanged?.Invoke();
    }
}
