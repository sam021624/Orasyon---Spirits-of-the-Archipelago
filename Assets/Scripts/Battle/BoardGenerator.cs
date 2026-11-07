using System.Collections.Generic;
using System.Linq;

namespace Orasyon.Battle
{
    /// <summary>
    /// Builds letter boards and checks which words can be made from them.
    /// Bookworm rules: any tiles can be used, in any order, each tile once per word.
    /// </summary>
    public static class BoardGenerator
    {
        public static char RandomLetter(System.Random rng) => LetterScoring.RandomLetter(rng);

        /// <summary>
        /// Random board containing the letters of <paramref name="plantWord"/> (if given),
        /// retried until at least <paramref name="minWords"/> words can be made from it.
        /// </summary>
        public static char[,] Generate(int rows, int cols, string plantWord, int minWords, System.Random rng)
        {
            char[,] best = null;
            int bestCount = -1;
            int size = rows * cols;

            for (int attempt = 0; attempt < 40; attempt++)
            {
                var cells = Enumerable.Range(0, size).OrderBy(_ => rng.Next()).ToList();
                var board = new char[rows, cols];
                int k = 0;

                if (!string.IsNullOrEmpty(plantWord) && plantWord.Length <= size)
                    foreach (char ch in plantWord.ToUpperInvariant())
                    {
                        int i = cells[k++];
                        board[i / cols, i % cols] = ch;
                    }

                for (; k < size; k++)
                {
                    int i = cells[k];
                    board[i / cols, i % cols] = RandomLetter(rng);
                }

                int count = CountWords(Letters(board), minWords);
                if (count >= minWords) return board;
                if (count > bestCount) { best = board; bestCount = count; }
            }
            return best;
        }

        /// <summary>The letters on a board, skipping empty cells ('\0').</summary>
        public static IEnumerable<char> Letters(char[,] board)
        {
            foreach (char c in board)
                if (c != '\0') yield return c;
        }

        /// <summary>How many distinct words can be made from these letters (stops early at <paramref name="stopAt"/>).</summary>
        public static int CountWords(IEnumerable<char> letters, int stopAt)
        {
            int n = 0;
            foreach (var _ in Formable(letters))
                if (++n >= stopAt) break;
            return n;
        }

        /// <summary>All words that can be made from these letters, longest first.</summary>
        public static List<string> FindWords(IEnumerable<char> letters, int max = int.MaxValue)
        {
            return Formable(letters).OrderByDescending(w => w.Length).ThenBy(w => w).Take(max).ToList();
        }

        private static IEnumerable<string> Formable(IEnumerable<char> letters)
        {
            var available = new int[26];
            int total = 0;
            foreach (char raw in letters)
            {
                char c = char.ToUpperInvariant(raw);
                if (c < 'A' || c > 'Z') continue;
                available[c - 'A']++;
                total++;
            }

            var used = new int[26];
            foreach (var word in WordDictionary.Words)
            {
                if (word.Length > total) continue;

                bool ok = true;
                foreach (char c in word)
                    if (++used[c - 'A'] > available[c - 'A']) { ok = false; break; }

                foreach (char c in word) used[c - 'A'] = 0;
                if (ok) yield return word;
            }
        }
    }
}
