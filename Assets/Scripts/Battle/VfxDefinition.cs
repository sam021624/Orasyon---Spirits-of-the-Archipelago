using UnityEngine;

namespace Orasyon.Battle
{
    /// <summary>One sprite-sheet visual effect (bolt, impact, smoke...). Created from Assets/Sprites/VFX by the editor tools.</summary>
    [CreateAssetMenu(menuName = "Orasyon/VFX Definition", fileName = "NewVfx")]
    public sealed class VfxDefinition : ScriptableObject
    {
        public FrameClip clip = new FrameClip();
        [Tooltip("Height of the effect on screen in UI pixels (reference resolution 1920x1080).")]
        public float size = 260f;
        [Tooltip("Frames per second.")]
        public float fps = 14f;
        public bool  loop;
        [Tooltip("Tall effects (lightning) sit on the ground point instead of being centered.")]
        public bool  anchorBottom;
        public Color tint = Color.white;

        public float Duration => clip != null && !clip.IsEmpty ? clip.Length / Mathf.Max(1f, fps) : 0f;
    }
}
