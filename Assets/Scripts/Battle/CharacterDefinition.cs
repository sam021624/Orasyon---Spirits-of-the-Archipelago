using UnityEngine;

namespace Orasyon.Battle
{
    /// <summary>
    /// Data for one combatant (the player or an enemy): stats plus animation clips.
    /// Each clip is one sprite-sheet strip (see <see cref="FrameClip"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "Orasyon/Character Definition", fileName = "NewCharacter")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [Header("Info")]
        public string displayName = "Spirit";
        public bool   isBoss;

        [Header("Stats")]
        public int    maxHP        = 50;
        [Tooltip("Used only when the Attacks list below is empty.")]
        public int    attackDamage = 8;
        [Tooltip("Used only when the Attacks list below is empty.")]
        public string attackName   = "Strike";

        [Header("Attacks (enemies pick one at random each turn, by weight)")]
        public EnemyAttack[] attacks;

        /// <summary>The attack list, or a single plain attack built from attackName / attackDamage.</summary>
        public EnemyAttack[] GetAttacks()
        {
            if (attacks != null && attacks.Length > 0) return attacks;
            return new[] { new EnemyAttack { name = attackName, minDamage = attackDamage, maxDamage = attackDamage } };
        }

        [Header("Info Panel")]
        [TextArea(3, 6)]
        public string   description;
        [Tooltip("Words this creature is weak to (+50% damage). One is planted on each new board.")]
        public string[] themeWords;

        [Header("Presentation")]
        [Tooltip("On-screen size of the sprite in UI pixels (reference resolution 1920x1080).")]
        public float spriteSize = 300f;
        [Tooltip("Lift above the ground line, e.g. for flying creatures.")]
        public float hoverHeight;
        public float framesPerSecond = 10f;
        [Tooltip("True if the artwork looks to the right (the hero). Enemies on the right side of the screen are mirrored automatically when they do.")]
        public bool  spriteFacesRight;

        [Header("Animation Clips (sprite sheets)")]
        public FrameClip idle = new FrameClip();
        [Tooltip("Optional. Played while moving; flipped automatically to face the direction of travel.")]
        public FrameClip walk = new FrameClip();
        public FrameClip attack = new FrameClip();
        [Tooltip("Optional. Used by the player for 5-6 letter words.")]
        public FrameClip cast = new FrameClip();
        [Tooltip("Optional. If empty, a red flash is used instead.")]
        public FrameClip hurt = new FrameClip();
        [Tooltip("Optional. If empty, the sprite fades out instead.")]
        public FrameClip death = new FrameClip();
        [Tooltip("Optional (hero). Lightning strike for 7+ letter words.")]
        public FrameClip kidlat = new FrameClip();
        [Tooltip("Optional (hero). Flurry of strikes for the Orasyon special.")]
        public FrameClip flurry = new FrameClip();
    }
}
