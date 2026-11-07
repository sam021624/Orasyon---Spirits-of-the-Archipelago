using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Orasyon.Battle
{
    /// <summary>
    /// Builds letter boards and checks how many valid words they contain.
    /// Words are formed from tiles that touch (8 directions), each tile used once per word.
    /// </summary>
    public static class BoardGenerator
    {
        private static readonly int[] DR = { -1, -1, -1, 0, 0, 1, 1, 1 };
        private static readonly int[] DC = { -1, 0, 1, -1, 1, -1, 0, 1 };

        public static char RandomLetter(System.Random rng) => LetterScoring.RandomLetter(rng);

        /// <summary>
        /// Random board with <paramref name="plantWord"/> placed along a connected path (if given),
        /// retried until it contains at least <paramref name="minWords"/> valid words.
        /// </summary>
        public static char[,] Generate(int rows, int cols, string plantWord, int minWords, System.Random rng)
        {
            char[,] best = null;
            int bestCount = -1;

            for (int attempt = 0; attempt < 80; attempt++)
            {
                var board = new char[rows, cols];
                if (!string.IsNullOrEmpty(plantWord) && plantWord.Length <= rows * cols)
                    TryPlant(board, plantWord.ToUpperInvariant(), rng);

                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                        if (board[r, c] == '\0') board[r, c] = RandomLetter(rng);

                int count = CountWords(board, minWords);
                if (count >= minWords) return board;
                if (count > bestCount) { best = board; bestCount = count; }
            }
            return best;
        }

        /// <summary>Counts distinct valid words on the board, stopping early at <paramref name="stopAt"/>.</summary>
        public static int CountWords(char[,] board, int stopAt)
        {
            var found = new HashSet<string>();
            Search(board, found, stopAt);
            return found.Count;
        }

        /// <summary>All valid words on the board, longest first.</summary>
        public static List<string> FindWords(char[,] board, int max = int.MaxValue)
        {
            var found = new HashSet<string>();
            Search(board, found, max);
            return found.OrderByDescending(w => w.Length).ThenBy(w => w).ToList();
        }

        // ── Internal ─────────────────────────────────────────────────────────
        private static bool TryPlant(char[,] board, string word, System.Random rng)
        {
            int rows = board.GetLength(0), cols = board.GetLength(1);
            var visited = new bool[rows, cols];
            var path = new List<Vector2Int>();

            var starts = new List<Vector2Int>();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    starts.Add(new Vector2Int(r, c));
            starts = starts.OrderBy(_ => rng.Next()).ToList();

            foreach (var s in starts)
            {
                if (Walk(s.x, s.y, 0))
                {
                    for (int i = 0; i < path.Count; i++)
                        board[path[i].x, path[i].y] = word[i];
                    return true;
                }
            }
            return false;

            bool Walk(int r, int c, int index)
            {
                visited[r, c] = true;
                path.Add(new Vector2Int(r, c));
                if (index == word.Length - 1) return true;

                foreach (int d in Enumerable.Range(0, 8).OrderBy(_ => rng.Next()))
                {
                    int nr = r + DR[d], nc = c + DC[d];
                    if (nr >= 0 && nr < rows && nc >= 0 && nc < cols && !visited[nr, nc] && Walk(nr, nc, index + 1))
                        return true;
                }

                visited[r, c] = false;
                path.RemoveAt(path.Count - 1);
                return false;
            }
        }

        private static void Search(char[,] board, HashSet<string> found, int stopAt)
        {
            WordDictionary.EnsureLoaded();
            int rows = board.GetLength(0), cols = board.GetLength(1);
            var visited = new bool[rows, cols];
            var sb = new StringBuilder();

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    Dfs(r, c);
                    if (found.Count >= stopAt) return;
                }

            void Dfs(int r, int c)
            {
                if (found.Count >= stopAt) return;
                visited[r, c] = true;
                sb.Append(board[r, c]);

                string s = sb.ToString();
                if (WordDictionary.IsPrefix(s))
                {
                    if (s.Length >= WordDictionary.MinLength && WordDictionary.IsWord(s))
                        found.Add(s);

                    for (int d = 0; d < 8; d++)
                    {
                        int nr = r + DR[d], nc = c + DC[d];
                        if (nr >= 0 && nr < rows && nc >= 0 && nc < cols && !visited[nr, nc])
                            Dfs(nr, nc);
                    }
                }

                sb.Length--;
                visited[r, c] = false;
            }
        }
    }
}
