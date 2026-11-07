using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Orasyon.Battle
{
    /// <summary>
    /// The 4x4 letter board. Handles selection (touching tiles only), word validity,
    /// removing used tiles (remaining tiles fall, new letters drop in from above), and scrambling.
    /// </summary>
    public sealed class LetterGrid : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField] private int        rows = 4;
        [SerializeField] private int        cols = 4;
        [SerializeField] private float      cellSize = 92f;
        [SerializeField] private float      spacing  = 8f;
        [SerializeField] private GameObject tilePrefab;

        [Header("Playability")]
        [Tooltip("After used tiles are replaced, re-roll the new letters until the board has at least this many words.")]
        [SerializeField] private int minWordsAfterRefill = 4;

        private LetterTileData[,]            tiles;
        private readonly List<LetterTileData> path = new List<LetterTileData>();
        private readonly System.Random        rng  = new System.Random();

        /// <summary>Fired whenever the selected word changes (select, undo, clear, refill).</summary>
        public event Action OnSelectionChanged;

        public int Rows => rows;
        public int Cols => cols;

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
                    d.Row = r;
                    d.Col = c;
                    d.IsSelected = false;
                    d.IsHighlighted = false;

                    Vector2 pos = CellPosition(r, c);
                    if (dropIn)
                    {
                        // Lower rows start lower so the whole board rains in top-down, staggered by column
                        d.View.Place(pos + Vector2.up * (DropHeight + (rows - r) * 40f + c * 25f));
                        d.View.MoveTo(pos);
                    }
                    else
                    {
                        d.View.Place(pos);
                    }
                    d.View.Refresh();
                }

            Changed();
        }

        public char[,] GetLetters()
        {
            EnsureTiles();
            var b = new char[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    b[r, c] = tiles[r, c].Letter;
            return b;
        }

        // ── Selection ────────────────────────────────────────────────────────
        public void OnTileClicked(LetterTileData tile)
        {
            int index = path.IndexOf(tile);
            if (index >= 0)
            {
                // Clicking a selected tile removes it and everything after it
                for (int i = path.Count - 1; i >= index; i--)
                {
                    path[i].IsSelected = false;
                    path[i].View.Refresh();
                    path.RemoveAt(i);
                }
                Changed();
                return;
            }

            if (path.Count == 0 || IsAdjacent(path[path.Count - 1], tile))
            {
                tile.IsSelected = true;
                path.Add(tile);
                tile.View.Refresh();
                Changed();
            }
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

        /// <summary>
        /// Removes the selected tiles from the board and returns the word they spelled.
        /// Tiles above fall down; new random letters drop in from the top.
        /// </summary>
        public string ConsumeSelection()
        {
            if (path.Count == 0) return "";
            string word = CurrentWord;
            var removed = new HashSet<LetterTileData>(path);
            path.Clear();

            var newTiles = new List<LetterTileData>();
            for (int c = 0; c < cols; c++)
            {
                var keep = new List<LetterTileData>();
                var gone = new List<LetterTileData>();
                for (int r = rows - 1; r >= 0; r--)
                {
                    var d = tiles[r, c];
                    if (removed.Contains(d)) gone.Add(d); else keep.Add(d);
                }

                int row = rows - 1;
                foreach (var d in keep)
                {
                    tiles[row, c] = d;
                    d.Row = row;
                    d.View.MoveTo(CellPosition(row, c));
                    row--;
                }

                int k = 0;
                foreach (var d in gone)
                {
                    tiles[row, c] = d;
                    d.Row = row;
                    d.IsSelected = false;
                    d.IsHighlighted = false;
                    d.Letter = BoardGenerator.RandomLetter(rng);
                    Vector2 pos = CellPosition(row, c);
                    d.View.Place(pos + Vector2.up * (DropHeight + k * 40f));
                    d.View.MoveTo(pos);
                    newTiles.Add(d);
                    row--;
                    k++;
                }
            }

            // Keep the board playable: re-roll only the new letters if too few words remain
            for (int attempt = 0; attempt < 30 && BoardGenerator.CountWords(GetLetters(), minWordsAfterRefill) < minWordsAfterRefill; attempt++)
                foreach (var d in newTiles) d.Letter = BoardGenerator.RandomLetter(rng);

            foreach (var d in tiles) d.View.Refresh();
            Changed();
            return word;
        }

        /// <summary>Shuffles the letters already on the board until it has at least <paramref name="minWords"/> words.</summary>
        public void Scramble(int minWords)
        {
            EnsureTiles();
            foreach (var t in path) t.IsSelected = false;
            path.Clear();

            var letters = tiles.Cast<LetterTileData>().Select(t => t.Letter).ToList();
            var board = new char[rows, cols];
            for (int attempt = 0; attempt < 25; attempt++)
            {
                letters = letters.OrderBy(_ => rng.Next()).ToList();
                for (int i = 0; i < letters.Count; i++) board[i / cols, i % cols] = letters[i];
                if (BoardGenerator.CountWords(board, minWords) >= minWords) break;
            }

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    var d = tiles[r, c];
                    d.Letter = board[r, c];
                    d.IsHighlighted = false;
                    d.View.Pop();
                    d.View.Refresh();
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

        private static bool IsAdjacent(LetterTileData a, LetterTileData b)
        {
            return Mathf.Abs(a.Row - b.Row) <= 1 && Mathf.Abs(a.Col - b.Col) <= 1 && !(a.Row == b.Row && a.Col == b.Col);
        }

        private void Changed() => OnSelectionChanged?.Invoke();
    }
}
