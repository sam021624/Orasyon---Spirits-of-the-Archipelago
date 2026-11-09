using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Orasyon.Battle
{
    /// <summary>
    /// Shows the enemy's NEXT move above its head ("Staff Smash 3-4"), like Bookworm Adventures, so the player
    /// can plan for it. Bosses mark their signature move with a star. Lives in the same layer as the characters.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class EnemyIntentUI : MonoBehaviour
    {
        [Tooltip("The enemy sprite this banner floats above.")]
        [SerializeField] private RectTransform   target;
        [SerializeField] private Image           background;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private float           maxY = 410f;   // keeps it below the top bar for tall enemies

        private CanvasGroup group;
        private bool        visible;

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
            label.text = Describe(attack);
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
            rt.anchoredPosition = new Vector2(Mathf.Clamp(target.anchoredPosition.x, -700f, 700f), y);
        }

        private static string Describe(EnemyAttack a)
        {
            string effect = "";
            switch (a.effect)
            {
                case AttackEffect.Poison: effect = "poison"; break;
                case AttackEffect.Stun:   effect = "stun"; break;
                case AttackEffect.Weaken: effect = "weaken"; break;
                case AttackEffect.Guard:  effect = "guard"; break;
                case AttackEffect.Leech:  effect = "drain"; break;
            }

            string s = a.isSignature ? "<color=#FFD700><b>(!)</b></color> " : "";
            s += $"<b>{a.name}</b>";
            string dmg = a.DamageRange;
            if (!string.IsNullOrEmpty(dmg)) s += $"  <color=#E8B04A>{dmg}</color>";
            if (effect.Length > 0) s += $"  <color={AttackEffectColor(a)}>{effect}</color>";
            return s;
        }

        private static string AttackEffectColor(EnemyAttack a) => EnemyAttack.EffectColor(a.effect);
    }
}
