using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Orasyon.Battle
{
    /// <summary>Right-panel info card for the current enemy: portrait, weaknesses, its attack list and lore.</summary>
    public sealed class EnemyInfoPanel : MonoBehaviour
    {
        [SerializeField] private RawImage        portrait;
        [SerializeField] private TextMeshProUGUI nameLabel;
        [Tooltip("Lists every attack: damage, crit chance and status effect.")]
        [SerializeField] private TextMeshProUGUI attackLabel;
        [SerializeField] private TextMeshProUGUI weaknessLabel;
        [SerializeField] private TextMeshProUGUI descriptionLabel;

        public void Show(CharacterDefinition enemy, EnemyAttack next = null)
        {
            if (enemy == null) return;

            if (portrait != null)
            {
                portrait.uvRect = new Rect(0f, 0f, 1f, 1f);
                if (enemy.idle != null) enemy.idle.Apply(portrait, 0); else portrait.texture = null;
            }

            if (nameLabel != null)
                nameLabel.text = enemy.isBoss ? $"<color=#FF6B6B>BOSS</color>  {enemy.displayName}" : enemy.displayName;

            if (attackLabel != null)
                attackLabel.text = string.Join("\n", enemy.GetAttacks().Select(a =>
                    (a == next ? "<color=#FF6B6B><b>></b></color> " : "<color=#E8B04A>\u2022</color> ") + a.Describe()));

            if (weaknessLabel != null)
                weaknessLabel.text = enemy.themeWords != null && enemy.themeWords.Length > 0
                    ? "Weak to: " + string.Join(", ", enemy.themeWords)
                    : "";

            if (descriptionLabel != null)
                descriptionLabel.text = enemy.description;
        }
    }
}
