using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Orasyon.Battle
{
    /// <summary>
    /// The five treasure slots under the potion shelf, one per final boss.
    /// Owned treasures show their icon and name; clicking one shows what it does.
    /// </summary>
    public sealed class TreasureShelf : MonoBehaviour
    {
        [Serializable]
        public sealed class Slot
        {
            public Image           icon;
            public TextMeshProUGUI label;
            public Button          button;
        }

        [SerializeField] private Slot[] slots;

        public event Action<TreasureDefinition> OnTreasureClicked;

        private void Start()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i;
                if (slots[i].button != null) slots[i].button.onClick.AddListener(() => Clicked(index));
            }
            Refresh();
        }

        public void Refresh()
        {
            if (slots == null) return;
            for (int i = 0; i < slots.Length; i++)
            {
                var t = TreasureAt(i);
                bool owned = PlayerProgress.Has(t);
                if (slots[i].icon != null)
                {
                    slots[i].icon.sprite = owned ? t.icon : null;
                    slots[i].icon.enabled = owned;
                }
                if (slots[i].label != null)  slots[i].label.text = owned ? t.displayName : "";
                if (slots[i].button != null) slots[i].button.interactable = owned;
            }
        }

        /// <summary>Where a treasure lands when it flies to the shelf.</summary>
        public RectTransform SlotTarget(TreasureDefinition t)
        {
            if (t == null || slots == null || t.slotIndex >= slots.Length) return null;
            return slots[t.slotIndex].icon != null ? slots[t.slotIndex].icon.rectTransform : null;
        }

        private static TreasureDefinition TreasureAt(int slot) =>
            PlayerProgress.All.FirstOrDefault(t => t.slotIndex == slot);

        private void Clicked(int index)
        {
            var t = TreasureAt(index);
            if (PlayerProgress.Has(t)) OnTreasureClicked?.Invoke(t);
        }
    }
}
