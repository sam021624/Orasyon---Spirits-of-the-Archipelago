using UnityEngine;

namespace Orasyon.UI
{
    /// <summary>
    /// Makes a decorative letter tile breathe, sway and bob while keeping its authored pose.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class FloatingLetterTile : MonoBehaviour
    {
        [SerializeField] private float bobHeight = 11f;
        [SerializeField] private float bobSpeed = 0.75f;
        [SerializeField] private float swayDegrees = 4.5f;
        [SerializeField] private float swaySpeed = 0.55f;
        [SerializeField] private float phaseOffset;

        private RectTransform rectTransform;
        private Vector2 basePosition;
        private float baseRotation;
        private float time;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            basePosition = rectTransform.anchoredPosition;
            baseRotation = rectTransform.localEulerAngles.z;
            time = phaseOffset;
        }

        private void Update()
        {
            time += Time.unscaledDeltaTime;

            float bob = Mathf.Sin(time * bobSpeed * Mathf.PI * 2f) * bobHeight;
            float sway = Mathf.Sin(time * swaySpeed * Mathf.PI * 2f + 1.1f) * swayDegrees;

            rectTransform.anchoredPosition = basePosition + new Vector2(0f, bob);
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, baseRotation + sway);
        }
    }
}
