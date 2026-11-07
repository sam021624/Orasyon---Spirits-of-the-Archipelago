using System;
using UnityEngine;

namespace Orasyon.Battle
{
    [Serializable]
    public sealed class ParallaxLayer
    {
        [Tooltip("Two copies of the same image placed side by side so the layer can wrap seamlessly.")]
        public RectTransform first;
        public RectTransform second;
        [Tooltip("1 = moves with the ground, smaller = farther away.")]
        public float speed = 1f;
    }

    /// <summary>Scrolls layered background images at different speeds while the player travels.</summary>
    public sealed class ParallaxBackground : MonoBehaviour
    {
        [SerializeField] private ParallaxLayer[] layers;
        [SerializeField] private float layerWidth = 1920f;

        private float offset;

        private void Start() => Apply();

        /// <summary>Scroll by <paramref name="pixels"/> (positive = travel to the right).</summary>
        public void Scroll(float pixels)
        {
            offset += pixels;
            Apply();
        }

        private void Apply()
        {
            if (layers == null) return;
            foreach (var layer in layers)
            {
                if (layer.first == null || layer.second == null) continue;
                float x = -Mathf.Repeat(offset * layer.speed, layerWidth);
                layer.first.anchoredPosition  = new Vector2(x, layer.first.anchoredPosition.y);
                layer.second.anchoredPosition = new Vector2(x + layerWidth, layer.second.anchoredPosition.y);
            }
        }
    }
}
