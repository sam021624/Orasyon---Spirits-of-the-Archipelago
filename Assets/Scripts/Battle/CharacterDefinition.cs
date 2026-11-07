using UnityEngine;

namespace Orasyon.Battle
{
    /// <summary>
    /// Data for one combatant (the player or an enemy): stats plus animation frames.
    /// Frames are full textures (not sliced sprites) so every frame keeps the same size and pivot.
    /// </summary>
    [CreateAssetMenu(menuName = "Orasyon/Character Definition", fileName = "NewCharacter")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [Header("Info")]
        public string displayName = "Spirit";
        public bool   isBoss;

        [Header("Stats")]
        public int    maxHP        = 50;
        public int    attackDamage = 8;
        public string attackName   = "Strike";

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

        [Header("Animation Frames (in order)")]
        public Texture2D[] idle;
        [Tooltip("Optional. Faces right; flipped automatically when walking left.")]
        public Texture2D[] walk;
        public Texture2D[] attack;
        [Tooltip("Optional. Used by the player for long words.")]
        public Texture2D[] cast;
        [Tooltip("Optional. If empty, a red flash is used instead.")]
        public Texture2D[] hurt;
        [Tooltip("Optional. If empty, the sprite fades out instead.")]
        public Texture2D[] death;
    }
}
