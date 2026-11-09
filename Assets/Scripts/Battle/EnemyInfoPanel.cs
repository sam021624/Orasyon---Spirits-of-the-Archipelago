using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Orasyon.Battle
{
    /// <summary>
    /// Right-panel info card for the current enemy: portrait, weakness words, its moves as icons
    /// (hover a move for its full description), and a short description of the creature.
    /// </summary>
    public sealed class EnemyInfoPanel : MonoBehaviour
    {
        [SerializeField] private RawImage        portrait;
        [SerializeField] private TextMeshProUGUI nameLabel;
        [Tooltip("Layout container (vertical) that receives one icon row per enemy move.")]
        [SerializeField] private RectTransform   attackList;
        [SerializeField] private TextMeshProUGUI weaknessLabel;
        [SerializeField] private TextMeshProUGUI descriptionLabel;

        private string lore = "";

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

            if (weaknessLabel != null)
                weaknessLabel.text = enemy.themeWords != null && enemy.themeWords.Length > 0
                    ? "<color=#E8E0C0>Weak to these words (+50% damage):</color>\n" + string.Join(", ", enemy.themeWords)
                    : "";

            lore = enemy.description ?? "";
            ShowLore();
            BuildMoves(enemy, next);
        }

        private void ShowLore()
        {
            if (descriptionLabel == null) return;
            descriptionLabel.fontStyle = FontStyles.Normal;
            descriptionLabel.text = lore;
        }

        private void ShowMoveDetail(EnemyAttack a)
        {
            if (descriptionLabel == null) return;
            descriptionLabel.fontStyle = FontStyles.Normal;
            descriptionLabel.text = a.Describe();
        }

        private void BuildMoves(CharacterDefinition enemy, EnemyAttack next)
        {
            if (attackList == null) return;
            for (int i = attackList.childCount - 1; i >= 0; i--) Destroy(attackList.GetChild(i).gameObject);

            var font = nameLabel != null ? nameLabel.font : null;
            foreach (var a in enemy.GetAttacks())
                BuildRow(a, a == next, font);
        }

        private void BuildRow(EnemyAttack a, bool isNext, TMP_FontAsset font)
        {
            var row = new GameObject("Move", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(attackList, false);

            var bg = row.GetComponent<Image>();
            bg.color = isNext ? new Color(0.55f, 0.14f, 0.12f, 0.55f) : new Color(1f, 1f, 1f, 0.05f);
            bg.raycastTarget = true;   // so the row can be hovered

            var hl = row.GetComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(5, 8, 2, 2);
            hl.spacing = 7;
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlWidth = hl.childControlHeight = true;
            hl.childForceExpandWidth = hl.childForceExpandHeight = false;
            row.GetComponent<LayoutElement>().minHeight = 30;
            row.GetComponent<LayoutElement>().preferredHeight = 30;

            MoveIcons.Thumb(row.transform, a, 26f);

            // name (takes the free space), then the chips on the right
            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            nameGo.transform.SetParent(row.transform, false);
            var name = nameGo.GetComponent<TextMeshProUGUI>();
            if (font != null) name.font = font;
            name.text = (isNext ? "<color=#FF8A7A>></color> " : "") + (a.isSignature ? "<color=#FFD700>(!)</color> " : "") + a.name;
            name.fontSize = 19;
            name.fontStyle = FontStyles.Bold;
            name.color = new Color(0.97f, 0.91f, 0.76f);
            name.alignment = TextAlignmentOptions.MidlineLeft;
            name.enableWordWrapping = false;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.raycastTarget = false;
            nameGo.GetComponent<LayoutElement>().flexibleWidth = 1f;

            MoveIcons.AddChips(row.transform, a, font, 22f, 18f);

            var hover = row.AddComponent<MoveRowHover>();
            hover.Setup(() => ShowMoveDetail(a), ShowLore);
        }
    }

    /// <summary>Hovering a move row shows that move's full description in the text box below.</summary>
    public sealed class MoveRowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Action onEnter, onExit;
        public void Setup(Action enter, Action exit) { onEnter = enter; onExit = exit; }
        public void OnPointerEnter(PointerEventData _) => onEnter?.Invoke();
        public void OnPointerExit(PointerEventData _)  => onExit?.Invoke();
    }
}
