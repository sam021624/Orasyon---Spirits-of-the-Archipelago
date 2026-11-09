using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Orasyon.Battle
{
    /// <summary>
    /// One clickable letter tile. Slides toward its target cell (used when tiles fall in),
    /// and colours itself based on its kind (special / bad), selection, tutorial highlight and hover.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class LetterTile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("References")]
        [SerializeField] private Image           background;
        [SerializeField] private Image           highlightRing;
        [SerializeField] private TextMeshProUGUI letterLabel;
        [Tooltip("Small text in the top-left corner naming the tile's effect, e.g. +20%.")]
        [SerializeField] private TextMeshProUGUI badgeLabel;
        [Tooltip("Small number in the bottom-right corner: enemy turns until the tile wears off.")]
        [SerializeField] private TextMeshProUGUI turnsLabel;

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
        private Button         button;
        private Vector2        target;
        private bool           hovered;
        private float          pop = 1f;

        public LetterTileData Data => data;

        private void Awake()
        {
            rect = (RectTransform)transform;
            button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
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
            if (button == null) button = GetComponent<Button>();

            bool isVoid = data.Kind == TileKind.Void;

            // Used tiles leave an empty gap on the board. A Void tile is a visible dark hole instead.
            bool visible = !data.IsEmpty || isVoid;
            if (gameObject.activeSelf != visible) gameObject.SetActive(visible);
            if (!visible) return;

            if (button != null) button.interactable = data.IsSelectable;

            // Letter: smoked tiles hide it, voids have none
            if (letterLabel != null)
            {
                letterLabel.text = isVoid ? "" : data.Kind == TileKind.Smoked ? "?" : data.Letter.ToString();
                letterLabel.color = data.IsSelected ? selectedLetterColor
                                  : data.Kind == TileKind.Locked ? new Color(0.70f, 0.62f, 0.55f)
                                  : letterColor;
            }

            // Background: selection > tutorial highlight > the tile's own colour (hover brightens normal tiles)
            Color bg;
            if (data.IsSelected)                 bg = selectedColor;
            else if (data.IsHighlighted)         bg = tutorialColor;
            else if (data.Kind != TileKind.Normal) bg = TileKindInfo.Fill(data.Kind);
            else if (hovered)                    bg = hoverColor;
            else                                 bg = normalColor;
            if (hovered && data.IsSelectable && !data.IsSelected && data.Kind != TileKind.Normal)
                bg = Color.Lerp(bg, Color.white, 0.25f);

            if (background != null)    background.color = bg;
            if (highlightRing != null) highlightRing.enabled = data.IsHighlighted && !data.IsSelected;

            if (badgeLabel != null)
            {
                string badge = TileKindInfo.Badge(data.Kind);
                badgeLabel.text = badge;
                badgeLabel.gameObject.SetActive(badge.Length > 0);
            }
            if (turnsLabel != null)
            {
                bool timed = data.TurnsLeft > 0 && data.Kind != TileKind.Normal && !isVoid;
                turnsLabel.text = timed ? data.TurnsLeft.ToString() : "";
                turnsLabel.gameObject.SetActive(timed);
            }
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
