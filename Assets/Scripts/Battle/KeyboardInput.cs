using UnityEngine;
using UnityEngine.InputSystem;

namespace Orasyon.Battle
{
    /// <summary>
    /// Play with the keyboard: type letters to pick tiles, Backspace to take the last one back, Enter to attack.
    /// Tab scrambles, Space asks for a hint, 1/2/3 drink the healing/warding/power potion, / casts the Orasyon.
    /// Added to the scene by BattleManager. It does nothing while the board is locked (enemy turn, tutorial, pause).
    /// </summary>
    public sealed class KeyboardInput : MonoBehaviour
    {
        private BattleManager battle;
        private LetterGrid    grid;

        public void Init(BattleManager battleManager, LetterGrid letterGrid)
        {
            battle = battleManager;
            grid = letterGrid;
        }

        private void OnEnable()
        {
            if (Keyboard.current != null) Keyboard.current.onTextInput += OnText;
        }

        private void OnDisable()
        {
            if (Keyboard.current != null) Keyboard.current.onTextInput -= OnText;
        }

        private bool Active => battle != null && Time.timeScale > 0f && !battle.IsBattleOver;

        private void OnText(char c)
        {
            if (!Active) return;
            if (char.IsLetter(c))
            {
                if (grid != null) grid.TrySelectLetter(c);
                return;
            }
            switch (c)
            {
                case '1': battle.KeyPotion(BuffType.HealingPotion); break;
                case '2': battle.KeyPotion(BuffType.WardingPotion); break;
                case '3': battle.KeyPotion(BuffType.PowerPotion);   break;
                case '/': battle.KeyOrasyon(); break;
            }
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !Active) return;

            if (kb.backspaceKey.wasPressedThisFrame && grid != null) grid.RemoveLastSelected();
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) battle.KeyAttack();
            if (kb.tabKey.wasPressedThisFrame)   battle.KeyScramble();
            if (kb.spaceKey.wasPressedThisFrame) battle.KeyHint();
        }
    }
}
