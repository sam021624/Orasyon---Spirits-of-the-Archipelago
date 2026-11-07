using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Orasyon.Battle
{
    /// <summary>
    /// Shows HP as a row of hearts (Bookworm style). Each heart is worth hpPerHeart and drains partially.
    /// Put this on an object with a HorizontalLayoutGroup.
    /// </summary>
    public sealed class HeartBar : MonoBehaviour
    {
        [SerializeField] private Sprite heartSprite;
        [SerializeField] private float  hpPerHeart = 10f;
        [SerializeField] private float  heartSize  = 30f;
        [Tooltip("Enemy side: hearts drain from the outer edge inward.")]
        [SerializeField] private bool   mirrored;
        [SerializeField] private Color  fullColor  = Color.white;
        [SerializeField] private Color  emptyColor = new Color(0.22f, 0.10f, 0.10f, 0.85f);

        private readonly List<Image> fills = new List<Image>();

        public void Set(int hp, int maxHp)
        {
            int count = Mathf.Max(1, Mathf.CeilToInt(maxHp / hpPerHeart));
            while (fills.Count < count) CreateHeart();

            for (int i = 0; i < fills.Count; i++)
            {
                fills[i].transform.parent.gameObject.SetActive(i < count);
                fills[i].fillAmount = Mathf.Clamp01((hp - i * hpPerHeart) / hpPerHeart);
            }
        }

        private void CreateHeart()
        {
            var slot = new GameObject("Heart", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            slot.transform.SetParent(transform, false);
            var empty = slot.GetComponent<Image>();
            empty.sprite = heartSprite;
            empty.color = emptyColor;
            empty.raycastTarget = false;
            var layout = slot.GetComponent<LayoutElement>();
            layout.preferredWidth = layout.preferredHeight = heartSize;
            ((RectTransform)slot.transform).sizeDelta = Vector2.one * heartSize;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(slot.transform, false);
            var rt = (RectTransform)fillGo.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var fill = fillGo.GetComponent<Image>();
            fill.sprite = heartSprite;
            fill.color = fullColor;
            fill.raycastTarget = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = mirrored ? (int)Image.OriginHorizontal.Right : (int)Image.OriginHorizontal.Left;
            fills.Add(fill);
        }
    }
}
