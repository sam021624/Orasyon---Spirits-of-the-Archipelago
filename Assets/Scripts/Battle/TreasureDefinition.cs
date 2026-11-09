using UnityEngine;

namespace Orasyon.Battle
{
    /// <summary>
    /// A permanent item dropped by a chapter boss (like Bookworm's treasures).
    /// Owned treasures stay with the player for the rest of the adventure.
    /// Put assets in Assets/Resources/Treasures so PlayerProgress can find them.
    /// </summary>
    [CreateAssetMenu(menuName = "Orasyon/Treasure", fileName = "NewTreasure")]
    public sealed class TreasureDefinition : ScriptableObject
    {
        public const int HpPerHeart = 10;

        [Tooltip("Save id. Don't change it after release or saved games lose the item.")]
        public string id = "treasure";
        public string displayName = "Treasure";
        [TextArea(2, 4)] public string description;
        public Sprite icon;

        [Tooltip("Which of the 5 shelf slots it sits in (one per final boss).")]
        [Range(0, 4)] public int slotIndex;

        [Header("Bonuses")]
        [Tooltip("Extra maximum hearts.")]
        public int bonusHearts;
        [Tooltip("Extra damage on every word, in percent (20 = +20%).")]
        public int damageBonusPercent;
        [Tooltip("Extra hints in every chapter.")]
        public int bonusHints;
        [Tooltip("Warding potions you start every battle chapter with.")]
        public int startingWardingPotions;
        [Tooltip("Potions are earned with words this many letters shorter (1 = a 4-letter word already earns a Healing Potion).")]
        public int potionLengthBonus;

        public string EffectText
        {
            get
            {
                var parts = new System.Collections.Generic.List<string>();
                if (bonusHearts > 0) parts.Add($"+{bonusHearts} heart{(bonusHearts > 1 ? "s" : "")}");
                if (damageBonusPercent > 0) parts.Add($"+{damageBonusPercent}% damage");
                if (bonusHints > 0) parts.Add($"+{bonusHints} hints per chapter");
                if (startingWardingPotions > 0) parts.Add($"start with {startingWardingPotions} Warding Potion{(startingWardingPotions > 1 ? "s" : "")}");
                if (potionLengthBonus > 0) parts.Add("potions from shorter words");
                return string.Join(", ", parts);
            }
        }
    }
}
