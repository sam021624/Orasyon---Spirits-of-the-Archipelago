using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Orasyon.UI
{
    /// <summary>
    /// Hover / press / keyboard-focus feedback for a menu button.
    /// Works with both mouse and keyboard navigation.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class MenuButtonFX : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        [Header("Targets")]
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image glow;

        [Header("Motion")]
        [SerializeField] private float hoverScale = 1.035f;
        [SerializeField] private float pressScale = 0.985f;
        [SerializeField] private float lerpSpeed = 16f;

        [Header("Colors")]
        [SerializeField] private Color hoverTint = new Color(1f, 0.93f, 0.72f, 1f);
        [SerializeField] private float hoverGlowAlpha = 0.55f;

        private RectTransform rectTransform;
        private Color baseBackgroundColor;
        private Color baseLabelColor;
        private Color baseGlowColor;
        private bool hovered;
        private bool pressed;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;

            if (background != null)
            {
                baseBackgroundColor = background.color;
            }

            if (label != null)
            {
                baseLabelColor = label.color;
            }

            if (glow != null)
            {
                baseGlowColor = glow.color;
                baseGlowColor.a = 0f;
                glow.color = baseGlowColor;
            }
        }

        private void OnDisable()
        {
            hovered = false;
            pressed = false;

            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
            }

            if (background != null)
            {
                background.color = baseBackgroundColor;
            }

            if (label != null)
            {
                label.color = baseLabelColor;
            }

            if (glow != null)
            {
                glow.color = baseGlowColor;
            }
        }

        private void Update()
        {
            float targetScale = pressed ? pressScale : (hovered ? hoverScale : 1f);
            float step = 1f - Mathf.Exp(-lerpSpeed * Time.unscaledDeltaTime);

            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, Vector3.one * targetScale, step);

            if (background != null)
            {
                Color target = hovered
                    ? Color.Lerp(baseBackgroundColor, baseBackgroundColor + new Color(0.16f, 0.13f, 0.08f, 0f), 1f)
                    : baseBackgroundColor;
                target.a = baseBackgroundColor.a;
                background.color = Color.Lerp(background.color, target, step);
            }

            if (label != null)
            {
                Color target = hovered ? hoverTint : baseLabelColor;
                label.color = Color.Lerp(label.color, target, step);
            }

            if (glow != null)
            {
                Color target = baseGlowColor;
                target.a = hovered ? hoverGlowAlpha : 0f;
                glow.color = Color.Lerp(glow.color, target, step);
            }
        }

        public void OnPointerEnter(PointerEventData eventData) => hovered = true;
        public void OnPointerExit(PointerEventData eventData) => hovered = false;
        public void OnPointerDown(PointerEventData eventData) => pressed = true;
        public void OnPointerUp(PointerEventData eventData) => pressed = false;
        public void OnSelect(BaseEventData eventData) => hovered = true;
        public void OnDeselect(BaseEventData eventData) => hovered = false;
    }
}
