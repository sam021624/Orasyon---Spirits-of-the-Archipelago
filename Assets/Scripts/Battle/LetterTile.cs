using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Orasyon.Battle
{
    /// <summary>
    /// One clickable letter tile. Slides toward its target cell (used when tiles fall in),
    /// and colours itself based on selection / tutorial highlight / hover.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class LetterTile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("References")]
        [SerializeField] private Image           background;
        [SerializeField] private Image           highlightRing;
        [SerializeField] private TextMeshProUGUI letterLabel;

        [Header("Colors")]
        [SerializeField] private Color normalColor         = new Color(0.96f, 0.89f, 0.70f);
        [SerializeField] private Color hoverColor          = new Color(1.00f, 0.96f, 0.82f);
        [SerializeField] private Color selectedColor       = new Color(0.55f, 0.43f, 0.28f);
        [SerializeField] private Color tutorialColor       = new Color(0.62f, 0.90f, 0.55f);
        [SerializeField] private Color letterColor         = new Color(0.28f, 0.16f, 0.07f);
        [SerializeField] private Color selectedLetterColor = new Color(0.96f, 0.89f, 0.70f);

        [Header("Motion")]
        [SerializeField] private float fallSpeed = 1600f;

        private LetterTileData data;
        private LetterGrid     grid;
        private RectTransform  rect;
        private Vector2        target;
        private bool           hovered;
        private float          pop = 1f;

        public LetterTileData Data => data;

        private void Awake()
        {
            rect = (RectTransform)transform;
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        public void Bind(LetterTileData tileData, LetterGrid owner)
        {
            if (rect == null) rect = (RectTransform)transform;
            data = tileData;
            grid = owner;
            data.View = this;
            Refresh();
        }

        /// <summary>Teleport to a position (e.g. above the grid before falling).</summary>
        public void Place(Vector2 position)
        {
            if (rect == null) rect = (RectTransform)transform;
            rect.anchoredPosition = position;
            target = position;
        }

        /// <summary>Slide to a position over time.</summary>
        public void MoveTo(Vector2 position) => target = position;

        public void Pop() => pop = 0f;

        public void Refresh()
        {
            if (data == null) return;
            if (letterLabel != null) letterLabel.text = data.Letter.ToString();

            Color bg;
            if (data.IsSelected)          bg = selectedColor;
            else if (data.IsHighlighted)  bg = tutorialColor;
            else if (hovered)             bg = hoverColor;
            else                          bg = normalColor;

            if (background != null)  background.color = bg;
            if (letterLabel != null) letterLabel.color = data.IsSelected ? selectedLetterColor : letterColor;
            if (highlightRing != null) highlightRing.enabled = data.IsHighlighted && !data.IsSelected;
        }

        private void Update()
        {
            if (rect.anchoredPosition != target)
                rect.anchoredPosition = Vector2.MoveTowards(rect.anchoredPosition, target, fallSpeed * Time.deltaTime);

            if (pop < 1f)
            {
                pop = Mathf.Min(1f, pop + Time.deltaTime * 5f);
                float eased = 1f - (1f - pop) * (1f - pop);
                rect.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, eased);
            }
        }

        private void OnClick()
        {
            if (data != null && grid != null) grid.OnTileClicked(data);
        }

        public void OnPointerEnter(PointerEventData _) { hovered = true;  Refresh(); }
        public void OnPointerExit(PointerEventData _)  { hovered = false; Refresh(); }
    }
}
