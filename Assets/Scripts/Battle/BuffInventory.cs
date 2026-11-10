using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Orasyon.Battle
{
    /// <summary>Potions on the left-panel shelf, earned by spelling long words.</summary>
    public enum BuffType
    {
        HealingPotion, // red: restore HP
        WardingPotion, // green: block the next enemy attack
        PowerPotion    // blue: double damage on the next word
    }

    [Serializable]
    public sealed class BuffSlot
    {
        public BuffType        type;
        public string          displayName;
        [Tooltip("Words of at least this many letters award this buff (the longest matching rule wins).")]
        public int             minWordLength = 5;
        public Button          button;
        public TextMeshProUGUI countLabel;
        public CanvasGroup     group;
    }

    /// <summary>
    /// Left-panel potion shelf. Long words award potions; clicking one drinks it.
    /// The effect itself is applied by BattleManager (OnBuffUsed).
    /// The item slots below the shelf are reserved for boss drops (not implemented yet).
    /// </summary>
    public sealed class BuffInventory : MonoBehaviour
    {
        [SerializeField] private BuffSlot[] slots;
        [SerializeField] private int        maxStack = 9;

        private readonly Dictionary<BuffType, int> counts = new Dictionary<BuffType, int>();
        private bool interactable = true;

        public event Action<BuffType> OnBuffUsed;

        private void Awake()
        {
            foreach (var slot in slots)
            {
                counts[slot.type] = 0;
                var captured = slot;
                if (slot.button != null) slot.button.onClick.AddListener(() => Use(captured));
            }
            Refresh();
        }

        public bool Interactable
        {
            get => interactable;
            set { interactable = value; Refresh(); }
        }

        public int Count(BuffType type) => counts.TryGetValue(type, out int n) ? n : 0;

        /// <summary>Gives potions directly (treasure bonuses).</summary>
        public void Add(BuffType type, int amount)
        {
            if (amount <= 0) return;
            counts[type] = Mathf.Min(maxStack, Count(type) + amount);
            Refresh();
        }

        /// <summary>
        /// Awards the best buff this word length qualifies for. Returns null if none.
        /// With <paramref name="powerAllowed"/> off, the power potion is skipped and the next best one is given.
        /// </summary>
        public BuffSlot AwardForWord(int wordLength, bool powerAllowed = true)
        {
            BuffSlot best = null;
            foreach (var slot in slots)
            {
                if (!powerAllowed && slot.type == BuffType.PowerPotion) continue;
                if (wordLength >= slot.minWordLength && (best == null || slot.minWordLength > best.minWordLength))
                    best = slot;
            }
            if (best == null) return null;

            counts[best.type] = Mathf.Min(maxStack, Count(best.type) + 1);
            Refresh();
            if (best.button != null) StartCoroutine(Pulse((RectTransform)best.button.transform));
            return best;
        }

        /// <summary>Keyboard shortcut: drinks a potion of this type if there is one.</summary>
        public void TryUse(BuffType type)
        {
            foreach (var slot in slots)
                if (slot.type == type) { Use(slot); return; }
        }

        private void Use(BuffSlot slot)
        {
            if (!interactable || Count(slot.type) <= 0) return;
            counts[slot.type]--;
            Refresh();
            OnBuffUsed?.Invoke(slot.type);
        }

        private void Refresh()
        {
            if (slots == null) return;
            foreach (var slot in slots)
            {
                int n = Count(slot.type);
                if (slot.countLabel != null) slot.countLabel.text = "x" + n;
                if (slot.group != null) slot.group.alpha = n > 0 ? 1f : 0.45f;
                if (slot.button != null) slot.button.interactable = interactable && n > 0;
            }
        }

        private static IEnumerator Pulse(RectTransform target)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.4f)
            {
                target.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            target.localScale = Vector3.one;
        }
    }
}
