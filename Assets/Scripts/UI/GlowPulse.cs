using UnityEngine;
using UnityEngine.UI;

namespace Orasyon.UI
{
    /// <summary>
    /// Soft pulsing glow used behind the moon and the title.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class GlowPulse : MonoBehaviour
    {
        [SerializeField] private float pulseSpeed = 0.35f;
        [SerializeField] private float minAlpha = 0.35f;
        [SerializeField] private float maxAlpha = 0.75f;
        [SerializeField] private float scaleAmount = 0.035f;

        private Image image;
        private Color baseColor;
        private Vector3 baseScale;
        private float time;

        private void Awake()
        {
            image = GetComponent<Image>();
            baseColor = image.color;
            baseScale = transform.localScale;
            time = Random.Range(0f, 10f);
        }

        private void Update()
        {
            time += Time.unscaledDeltaTime;
            float wave = 0.5f + 0.5f * Mathf.Sin(time * pulseSpeed * Mathf.PI * 2f);

            Color color = baseColor;
            color.a = Mathf.Lerp(minAlpha, maxAlpha, wave) * baseColor.a;
            image.color = color;

            float scale = 1f + Mathf.Lerp(-scaleAmount, scaleAmount, wave);
            transform.localScale = baseScale * scale;
        }
    }
}
