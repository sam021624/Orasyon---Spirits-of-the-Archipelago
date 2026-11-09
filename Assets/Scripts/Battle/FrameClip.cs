using System;
using UnityEngine;
using UnityEngine.UI;

namespace Orasyon.Battle
{
    /// <summary>
    /// An animation stored as one horizontal sprite sheet (all frames side by side, equal width).
    /// Frames are shown by moving a RawImage's uvRect, so every frame keeps the same size and pivot.
    /// </summary>
    [Serializable]
    public sealed class FrameClip
    {
        public Texture2D sheet;
        [Min(1)] public int frames = 1;
        [Tooltip("0 = use the animator's frame rate.")]
        public float fps;

        /// <summary>Number of frames (0 when no sheet is assigned).</summary>
        public int Length => sheet != null ? Mathf.Max(1, frames) : 0;

        public bool IsEmpty => Length == 0;

        /// <summary>Width / height of one frame.</summary>
        public float Aspect => sheet != null ? (sheet.width / (float)Length) / sheet.height : 1f;

        public FrameClip() { }

        public FrameClip(Texture2D sheet, int frames, float fps = 0f)
        {
            this.sheet = sheet;
            this.frames = Mathf.Max(1, frames);
            this.fps = fps;
        }

        /// <summary>Normalized texture rectangle of a frame (inset half a texel so neighbours never bleed in).</summary>
        public Rect Uv(int index)
        {
            int n = Length;
            if (n == 0) return new Rect(0f, 0f, 1f, 1f);
            index = Mathf.Clamp(index, 0, n - 1);
            float w = 1f / n;
            float inset = 0.5f / sheet.width;
            return new Rect(index * w + inset, 0f, w - 2f * inset, 1f);
        }

        /// <summary>Shows one frame on a RawImage.</summary>
        public void Apply(RawImage image, int index)
        {
            if (image == null) return;
            if (sheet == null) { image.texture = null; return; }
            image.texture = sheet;
            image.uvRect = Uv(index);
        }
    }
}
