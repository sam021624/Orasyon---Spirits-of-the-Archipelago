using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Orasyon.Battle
{
    /// <summary>
    /// Treasures the player has collected, saved in PlayerPrefs so they carry across chapters and sessions.
    /// Starting a new game from the main menu clears them.
    /// </summary>
    public static class PlayerProgress
    {
        private const string Key = "orasyon.treasures";

        private static List<string>         owned;
        private static TreasureDefinition[] all;

        /// <summary>Every treasure in the game (Resources/Treasures), in shelf order.</summary>
        public static IReadOnlyList<TreasureDefinition> All
        {
            get
            {
                if (all == null)
                    all = Resources.LoadAll<TreasureDefinition>("Treasures").OrderBy(t => t.slotIndex).ToArray();
                return all;
            }
        }

        public static IEnumerable<TreasureDefinition> Owned => All.Where(Has);

        public static bool Has(TreasureDefinition t)
        {
            Load();
            return t != null && owned.Contains(t.id);
        }

        /// <summary>Adds the treasure. Returns false if it was already owned.</summary>
        public static bool Grant(TreasureDefinition t)
        {
            if (t == null || Has(t)) return false;
            owned.Add(t.id);
            Save();
            return true;
        }

        public static int BonusMaxHP => Owned.Sum(t => t.bonusHearts) * TreasureDefinition.HpPerHeart;

        public static float DamageMultiplier => 1f + Owned.Sum(t => t.damageBonusPercent) / 100f;

        public static void ResetAll()
        {
            Load();
            owned.Clear();
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }

        private static void Load()
        {
            if (owned != null) return;
            owned = PlayerPrefs.GetString(Key, "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }

        private static void Save()
        {
            PlayerPrefs.SetString(Key, string.Join(",", owned));
            PlayerPrefs.Save();
        }
    }
}
