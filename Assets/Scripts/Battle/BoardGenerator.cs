using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

                bool healthy = IsHealthyMix(Letters(board));
                int count = healthy ? CountWords(Letters(board), minWords) : 0;
                if (healthy && count >= minWords && HasCommonWord(Letters(board))) return board;
                if (count > bestCount) { best = board; bestCount = count; }
            }
            return best;
        }

        // ── Board quality (always-refill boards must stay playable) ──────────
        public const float MinVowelRatio = 0.30f;
        public const float MaxVowelRatio = 0.45f;
        public const int   MaxRareLetters = 2;   // J, X, Z, K per board

        public static bool IsVowel(char c)
        {
            c = char.ToUpperInvariant(c);
            return c == 'A' || c == 'E' || c == 'I' || c == 'O' || c == 'U';
        }

        public static bool IsRare(char c)
        {
            c = char.ToUpperInvariant(c);
            return c == 'J' || c == 'X' || c == 'Z' || c == 'K' || c == 'Q';
        }

        /// <summary>Vowel ratio between 30% and 45% and at most two rare letters.</summary>
        public static bool IsHealthyMix(IEnumerable<char> letters)
        {
            int total = 0, vowels = 0, rare = 0;
            foreach (char c in letters)
            {
                total++;
                if (IsVowel(c)) vowels++;
                else if (IsRare(c)) rare++;
            }
            if (total == 0) return true;
            float ratio = vowels / (float)total;
            return ratio >= MinVowelRatio && ratio <= MaxVowelRatio && rare <= MaxRareLetters;
        }

        /// <summary>True if an everyday word of at least <paramref name="minLength"/> letters can be made.</summary>
        public static bool HasCommonWord(IEnumerable<char> letters, int minLength = 3)
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
            foreach (var word in WordDictionary.CommonWords)
            {
                if (word.Length < minLength || word.Length > total) continue;

                bool ok = true;
                foreach (char c in word)
                    if (++used[c - 'A'] > available[c - 'A']) { ok = false; break; }

                foreach (char c in word) used[c - 'A'] = 0;
                if (ok) return true;
            }
            return false;
        }

        private const string FallbackVowels     = "EEAAOIU";
        private const string FallbackConsonants = "TNRSLDCHMPB";

        /// <summary>A letter that nudges the vowel ratio toward the healthy range (used when random fills keep failing).</summary>
        public static char BalancedLetter(System.Random rng, int vowelsNow, int totalAfter)
        {
            int minV = Mathf.CeilToInt(MinVowelRatio * totalAfter);
            int maxV = Mathf.FloorToInt(MaxVowelRatio * totalAfter);
            if (vowelsNow < minV) return FallbackVowels[rng.Next(FallbackVowels.Length)];
            if (vowelsNow >= maxV) return FallbackConsonants[rng.Next(FallbackConsonants.Length)];
            char c;
            int guard = 0;
            do { c = RandomLetter(rng); } while (IsRare(c) && guard++ < 10);
            return c;
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
