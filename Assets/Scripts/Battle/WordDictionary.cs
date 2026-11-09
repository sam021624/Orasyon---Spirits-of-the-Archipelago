using System;
using System.Collections.Generic;
using UnityEngine;

namespace Orasyon.Battle
{
    /// <summary>
    /// Local English word list used to validate words (no internet needed).
    /// Loads Resources/Words/words.txt (ENABLE list, public domain) plus
    /// Resources/Words/custom_words.txt (project-specific words such as folklore names).
    /// Words are kept in one sorted array, so both exact lookups and prefix checks are binary searches.
    /// </summary>
    public static class WordDictionary
    {
        public const int MinLength = 3;

        private static string[] words;

        public static bool IsLoaded => words != null;
        public static int  Count    => words?.Length ?? 0;

        /// <summary>All words, sorted, uppercase.</summary>
        public static IReadOnlyList<string> Words
        {
            get { EnsureLoaded(); return words; }
        }

        public static void EnsureLoaded()
        {
            if (words != null) return;

            var list = new List<string>(180000);
            AddFrom(list, Resources.Load<TextAsset>("Words/words"));
            AddFrom(list, Resources.Load<TextAsset>("Words/custom_words"));
            list.Sort(StringComparer.Ordinal);

            // Remove duplicates (custom words may also be in the main list) and offensive words
            var blocked = Blocked;
            var unique = new List<string>(list.Count);
            for (int i = 0; i < list.Count; i++)
                if ((i == 0 || list[i] != list[i - 1]) && !blocked.Contains(list[i])) unique.Add(list[i]);

            words = unique.ToArray();
            if (words.Length == 0)
                Debug.LogError("[WordDictionary] No words loaded. Expected Assets/Resources/Words/words.txt");
        }

        private static HashSet<string> common;
        private static HashSet<string> blockedSet;

        /// <summary>Offensive words that are never playable (Resources/Words/blocked_words.txt, see blocked_words_SOURCE.txt).</summary>
        public static HashSet<string> Blocked
        {
            get
            {
                if (blockedSet == null)
                {
                    var list = new List<string>();
                    AddFrom(list, Resources.Load<TextAsset>("Words/blocked_words"));
                    blockedSet = new HashSet<string>(list);
                }
                return blockedSet;
            }
        }

        /// <summary>
        /// True for everyday words (Resources/Words/common_words.txt, ~8,000 of the most used English words).
        /// Used to pick hints the player will actually recognise.
        /// </summary>
        public static bool IsCommon(string word)
        {
            EnsureCommon();
            return !string.IsNullOrEmpty(word) && common.Contains(word.ToUpperInvariant());
        }

        /// <summary>The everyday-word list (uppercase), also used to guarantee a playable board.</summary>
        public static IReadOnlyCollection<string> CommonWords
        {
            get { EnsureCommon(); return common; }
        }

        private static void EnsureCommon()
        {
            if (common != null) return;
            var list = new List<string>();
            AddFrom(list, Resources.Load<TextAsset>("Words/common_words"));
            AddFrom(list, Resources.Load<TextAsset>("Words/custom_words"));   // folklore words count as known
            common = new HashSet<string>(list);
            common.ExceptWith(Blocked);
        }

        /// <summary>True if <paramref name="word"/> is a valid word of at least MinLength letters.</summary>
        public static bool IsWord(string word)
        {
            if (string.IsNullOrEmpty(word) || word.Length < MinLength) return false;
            EnsureLoaded();
            return Array.BinarySearch(words, word.ToUpperInvariant(), StringComparer.Ordinal) >= 0;
        }

        /// <summary>True if some valid word starts with <paramref name="prefix"/>.</summary>
        public static bool IsPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) return true;
            EnsureLoaded();
            prefix = prefix.ToUpperInvariant();
            int i = Array.BinarySearch(words, prefix, StringComparer.Ordinal);
            if (i >= 0) return true;
            i = ~i;
            return i < words.Length && words[i].StartsWith(prefix, StringComparison.Ordinal);
        }

        private static void AddFrom(List<string> list, TextAsset asset)
        {
            if (asset == null) return;
            foreach (var raw in asset.text.Split('\n'))
            {
                string w = raw.Trim().ToUpperInvariant();
                if (w.Length < MinLength) continue;
                bool ok = true;
                foreach (char ch in w)
                    if (ch < 'A' || ch > 'Z') { ok = false; break; }
                if (ok) list.Add(w);
            }
        }
    }
}
