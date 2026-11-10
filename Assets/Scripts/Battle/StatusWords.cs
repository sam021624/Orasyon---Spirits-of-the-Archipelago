using System;
using System.Collections.Generic;
using UnityEngine;

namespace Orasyon.Battle
{
    public enum StatusEffect { Poison, Burn, Stun, Heal }

    /// <summary>A "Word of Power": spelling it exactly adds an effect on top of the normal damage.</summary>
    public sealed class StatusWord
    {
        public string       word;
        public StatusEffect effect;
        public int          turns;
        public int          power;

        public string Describe()
        {
            switch (effect)
            {
                case StatusEffect.Poison: return $"Poison {power}/turn for {turns} turns";
                case StatusEffect.Burn:   return $"Burn {power}/turn for {turns} turns";
                case StatusEffect.Stun:   return "Stuns the foe";
                default:                  return $"Heals {power}";
            }
        }
    }

    /// <summary>Loads Resources/Words/status_words.txt (WORD,EFFECT,TURNS,POWER per line, '#' starts a comment).</summary>
    public static class StatusWordDatabase
    {
        private static Dictionary<string, StatusWord> table;

        public static StatusWord Get(string word)
        {
            if (string.IsNullOrEmpty(word)) return null;
            Load();
            table.TryGetValue(word.ToUpperInvariant(), out var s);
            return s;
        }

        private static void Load()
        {
            if (table != null) return;
            table = new Dictionary<string, StatusWord>();
            var asset = Resources.Load<TextAsset>("Words/status_words");
            if (asset == null) return;

            foreach (var raw in asset.text.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                var p = line.Split(',');
                if (p.Length < 4) continue;
                if (!Enum.TryParse(p[1].Trim(), true, out StatusEffect fx)) continue;
                int.TryParse(p[2].Trim(), out int turns);
                int.TryParse(p[3].Trim(), out int power);
                string w = p[0].Trim().ToUpperInvariant();
                table[w] = new StatusWord { word = w, effect = fx, turns = turns, power = power };
            }
        }
    }
}
