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

        public string EffectText
        {
            get
            {
                string s = "";
                if (bonusHearts > 0) s += $"+{bonusHearts} heart{(bonusHearts > 1 ? "s" : "")}";
                if (damageBonusPercent > 0) s += (s.Length > 0 ? ", " : "") + $"+{damageBonusPercent}% damage";
                return s;
            }
        }
    }
}
