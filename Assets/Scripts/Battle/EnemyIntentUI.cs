using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Orasyon.Battle
{
    /// <summary>
    /// Shows the enemy's NEXT move above its head as icons ([picture] Name [claw 2-3] [effect]), like Bookworm
    /// Adventures, so the player can plan for it. Bosses mark their signature move with (!). Lives in the same
    /// layer as the characters; the banner has a horizontal layout group and sizes itself to its content.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class EnemyIntentUI : MonoBehaviour
    {
        [Tooltip("The enemy sprite this banner floats above.")]
        [SerializeField] private RectTransform   target;
        [SerializeField] private Image           background;
        [Tooltip("The move's name.")]
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private float           maxY = 410f;   // keeps it below the top bar for tall enemies

        private CanvasGroup group;
        private bool        visible;
        private readonly List<GameObject> dynamicChildren = new List<GameObject>();

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        public void Show(EnemyAttack attack)
        {
            if (attack == null) { Hide(); return; }
            visible = true;

            foreach (var g in dynamicChildren) if (g != null) Destroy(g);
            dynamicChildren.Clear();

            // [picture] Name [chips...]
            var thumb = MoveIcons.Thumb(transform, attack, 34f);
            thumb.transform.SetSiblingIndex(0);
            dynamicChildren.Add(thumb);

            label.text = (attack.isSignature ? "<color=#FFD700>(!)</color> " : "") + attack.name;
            label.fontStyle = FontStyles.Bold;
            label.enableWordWrapping = false;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.transform.SetSiblingIndex(1);

            MoveIcons.AddChips(transform, attack, label.font, 26f, 21f, dynamicChildren);

            if (background != null)
                background.color = attack.isSignature ? new Color(0.35f, 0.08f, 0.08f, 0.92f) : new Color(0.08f, 0.05f, 0.04f, 0.88f);
        }

        public void Hide() => visible = false;

        private void Update()
        {
            if (group == null) return;
            float goal = visible ? 1f : 0f;
            group.alpha = Mathf.MoveTowards(group.alpha, goal, Time.unscaledDeltaTime * 6f);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            var rt = (RectTransform)transform;
            float bob = Mathf.Sin(Time.unscaledTime * 3f) * 4f;
            float y = Mathf.Min(target.anchoredPosition.y + target.sizeDelta.y + 8f, maxY) + bob;
            rt.anchoredPosition = new Vector2(Mathf.Clamp(target.anchoredPosition.x, -640f, 640f), y);
        }
    }
}
