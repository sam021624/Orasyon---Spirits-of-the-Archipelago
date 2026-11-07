using UnityEngine;
using UnityEngine.UI;

namespace Orasyon.UI
{
    /// <summary>
    /// Gentle Lissajous drift + twinkle for the firefly motes on the title screen.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class FireflyDrift : MonoBehaviour
    {
        [SerializeField] private float horizontalDrift = 55f;
        [SerializeField] private float verticalDrift = 32f;
        [SerializeField] private float driftSpeed = 0.18f;
        [SerializeField] private float twinkleSpeed = 1.4f;
        [SerializeField] private float minAlpha = 0.05f;
        [SerializeField] private float maxAlpha = 0.55f;

        private RectTransform rectTransform;
        private Image image;
        private Vector2 origin;
        private Color baseColor;
        private float phase;
        private float time;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            image = GetComponent<Image>();
            origin = rectTransform.anchoredPosition;
            phase = Random.Range(0f, 100f);
            baseColor = image != null ? image.color : Color.white;
        }

        private void Update()
        {
            time += Time.unscaledDeltaTime;

            float x = Mathf.Sin(time * driftSpeed + phase) * horizontalDrift;
            float y = Mathf.Cos(time * driftSpeed * 0.77f + phase * 1.31f) * verticalDrift;
            rectTransform.anchoredPosition = origin + new Vector2(x, y);

            if (image != null)
            {
                float wave = 0.5f + 0.5f * Mathf.Sin(time * twinkleSpeed + phase * 2.1f);
                Color color = baseColor;
                color.a = Mathf.Lerp(minAlpha, maxAlpha, wave);
                image.color = color;
            }
        }
    }
}
