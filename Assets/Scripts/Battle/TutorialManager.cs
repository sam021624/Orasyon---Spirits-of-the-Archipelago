using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Orasyon.Battle
{
    /// <summary>
    /// First-battle tutorial: welcome → letter grid → spell the sample word → press Attack → tips.
    /// Locks the board/buttons through BattleManager until each step allows them.
    /// </summary>
    public sealed class TutorialManager : MonoBehaviour
    {
        [Header("Popup")]
        [SerializeField] private GameObject      tutorialPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private Button          nextButton;
        [SerializeField] private TextMeshProUGUI nextButtonLabel;

        [Header("Battle")]
        [SerializeField] private BattleManager battle;
        [SerializeField] private LetterGrid    letterGrid;
        [Tooltip("The bottom UI (buffs, grid, info). Hidden until the grid is introduced.")]
        [SerializeField] private CanvasGroup   bottomGroup;

        [Header("Sample Word")]
        [SerializeField] private string       sampleWord = "WORD";
        [Tooltip("Row,Col of each letter of the sample word on BattleManager's first board.")]
        [SerializeField] private Vector2Int[] sampleWordPositions;

        [SerializeField] private float fadeDuration = 0.35f;

        private enum Step { Welcome, Grid, Spell, Attack, Tips, Done }

        private Step step;
        private bool watchingForWord;
        private bool waitingForTips;

        private void Start()
        {
            battle.SetTutorialLocks(lockGrid: true, lockActions: true);
            if (bottomGroup != null) bottomGroup.alpha = 0f;

            nextButton.onClick.AddListener(OnNext);
            battle.OnPlayerAttack += HandleAttack;
            battle.OnTurnFinished += HandleTurnFinished;

            Show(Step.Welcome);
        }

        private void OnDestroy()
        {
            if (battle == null) return;
            battle.OnPlayerAttack -= HandleAttack;
            battle.OnTurnFinished -= HandleTurnFinished;
        }

        private void Show(Step s)
        {
            step = s;
            tutorialPanel.SetActive(s != Step.Done);
            nextButton.gameObject.SetActive(true);

            switch (s)
            {
                case Step.Welcome:
                    Set("Welcome, Albularyo",
                        "Strange spirits have appeared across the land. Your book of <b>Orasyon</b> turns words into power.\n\n" +
                        "Spell words from the letter grid to strike the creatures in your path.",
                        "Got it!");
                    break;

                case Step.Grid:
                    Set("The Letter Grid",
                        "Build a word by clicking <b>any</b> letters, in any order. Words need at least <b>3 letters</b>.\n\n" +
                        "Only real words light up the <b>Attack</b> button. Longer words and rare letters hit harder.",
                        "Show me!");
                    if (bottomGroup != null) StartCoroutine(Fade(bottomGroup, 1f));
                    break;

                case Step.Spell:
                    Set("Your First Word",
                        $"Spell <b>{sampleWord}</b> by clicking the green tiles in order.\n\n" +
                        "Click a selected tile again to undo it.",
                        "OK");
                    SetSampleHighlight(true);
                    break;

                case Step.Attack:
                    Set("A Real Word!",
                        $"<b>{sampleWord}</b> is in your book, so the tiles glow. Press <b>Attack</b>: the Albularyo will walk up and strike!",
                        "");
                    nextButton.gameObject.SetActive(false);
                    battle.SetTutorialLocks(lockGrid: false, lockActions: false);
                    break;

                case Step.Tips:
                    Set("Keep Fighting",
                        "Used tiles are replaced with <b>new letters</b> right away, so your board is always full.\n" +
                        "Words of <b>5+ letters</b> earn potions on the left shelf. Click one to drink it.\n" +
                        "The right panel lists your foe's <b>attacks</b>: watch for poison, stun and critical hits. Green potions cure them.\n" +
                        "Words of 5+ letters become <b>spells</b>. Stuck? <b>Scramble</b> shuffles your tiles, and <b>Hint</b> shows a word (3 per chapter).",
                        "Let's fight!");
                    break;
            }
        }

        private void OnNext()
        {
            switch (step)
            {
                case Step.Welcome: Show(Step.Grid); break;
                case Step.Grid:    Show(Step.Spell); break;
                case Step.Spell:
                    tutorialPanel.SetActive(false);
                    battle.SetTutorialLocks(lockGrid: false, lockActions: true);
                    watchingForWord = true;
                    break;
                case Step.Tips:    Show(Step.Done); break;
            }
        }

        private void Update()
        {
            if (!watchingForWord || letterGrid == null) return;
            if (string.Equals(letterGrid.CurrentWord, sampleWord, System.StringComparison.OrdinalIgnoreCase))
            {
                watchingForWord = false;
                SetSampleHighlight(false);
                Show(Step.Attack);
            }
        }

        private void HandleAttack(string word)
        {
            if (step != Step.Attack) return;
            tutorialPanel.SetActive(false);
            waitingForTips = true;
        }

        private void HandleTurnFinished()
        {
            if (!waitingForTips) return;
            waitingForTips = false;
            Show(Step.Tips);
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private void Set(string title, string body, string next)
        {
            if (titleText != null) titleText.text = title;
            if (bodyText != null) bodyText.text = body;
            if (nextButtonLabel != null) nextButtonLabel.text = next;
        }

        private void SetSampleHighlight(bool on)
        {
            if (letterGrid == null || sampleWordPositions == null) return;
            foreach (var p in sampleWordPositions) letterGrid.SetHighlight(p.x, p.y, on);
        }

        private IEnumerator Fade(CanvasGroup group, float to)
        {
            float from = group.alpha;
            for (float t = 0f; t < 1f; t += Time.deltaTime / fadeDuration)
            {
                group.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
            group.alpha = to;
        }
    }
}
