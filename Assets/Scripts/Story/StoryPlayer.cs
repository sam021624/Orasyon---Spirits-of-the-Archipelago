using System.Collections;
using Orasyon.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Orasyon.Story
{
    /// <summary>
    /// Plays a StoryScript: title card, then lines of dialogue with a typewriter effect.
    /// Click / Space / Enter advances (first click finishes the current line). Skip jumps to the next scene.
    /// </summary>
    public sealed class StoryPlayer : MonoBehaviour
    {
        [Tooltip("Played when the Story scene is opened directly (e.g. from the editor).")]
        [SerializeField] private StoryScript defaultScript;

        [Header("Stage")]
        [SerializeField] private RawImage            background;
        [SerializeField] private UIFrameAnimator     albularyo;
        [SerializeField] private CharacterDefinition albularyoDefinition;
        [SerializeField] private CanvasGroup         lolaGroup;
        [Tooltip("Lola's sprite. Leave empty to use the glowing placeholder.")]
        [SerializeField] private Texture2D           lolaArt;
        [SerializeField] private RawImage            lolaImage;
        [SerializeField] private GameObject          lolaPlaceholder;

        [Header("Dialogue")]
        [SerializeField] private CanvasGroup     dialogueGroup;
        [SerializeField] private GameObject      namePlate;
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private TextMeshProUGUI continueHint;
        [SerializeField] private Button          advanceButton;
        [SerializeField] private Button          skipButton;

        [Header("Title Card")]
        [SerializeField] private CanvasGroup     titleGroup;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Header("Effects")]
        [SerializeField] private CanvasGroup flashGroup;
        [SerializeField] private CanvasGroup fadeGroup;

        [Header("Settings")]
        [SerializeField] private string albularyoName  = "Albularyo";
        [SerializeField] private string lolaName       = "Lola Isidra";
        [SerializeField] private float  charsPerSecond = 45f;

        private static readonly Color AlbularyoColor = new Color(0.60f, 0.93f, 0.62f);
        private static readonly Color LolaColor      = new Color(1.00f, 0.80f, 0.45f);

        private StoryScript script;
        private bool        advanceRequested;
        private bool        leaving;
        private Coroutine   lolaFade;

        private void Start()
        {
            script = StoryRouter.Consume() ?? defaultScript;

            if (albularyo != null && albularyoDefinition != null)
            {
                albularyo.FramesPerSecond = albularyoDefinition.framesPerSecond;
                albularyo.SetIdle(albularyoDefinition.idle);
            }

            bool hasArt = lolaArt != null;
            if (lolaImage != null) { lolaImage.texture = lolaArt; lolaImage.enabled = hasArt; }
            if (lolaPlaceholder != null) lolaPlaceholder.SetActive(!hasArt);

            if (background != null && script != null) background.texture = script.background;
            advanceButton.onClick.AddListener(() => advanceRequested = true);
            skipButton.onClick.AddListener(Finish);

            StartCoroutine(Run());
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
                advanceRequested = true;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
                Finish();

            if (continueHint != null && continueHint.gameObject.activeSelf)
            {
                var c = continueHint.color;
                c.a = 0.45f + 0.45f * Mathf.Sin(Time.time * 4f);
                continueHint.color = c;
            }
        }

        private IEnumerator Run()
        {
            dialogueGroup.alpha = 0f;
            titleGroup.alpha = 0f;
            continueHint.gameObject.SetActive(false);
            SetLolaVisible(script != null && script.lines != null && script.lines.Length > 0 && script.lines[0].lolaOnStage, instant: true);

            fadeGroup.alpha = 1f;
            yield return Fade(fadeGroup, 0f, 0.6f);

            if (script == null) { Finish(); yield break; }

            // Title card (click to skip)
            if (!string.IsNullOrEmpty(script.title))
            {
                titleText.text = script.title;
                subtitleText.text = script.subtitle;
                yield return Fade(titleGroup, 1f, 0.5f);
                advanceRequested = false;
                for (float t = 0f; t < 2.4f && !advanceRequested; t += Time.deltaTime) yield return null;
                yield return Fade(titleGroup, 0f, 0.4f);
            }

            yield return Fade(dialogueGroup, 1f, 0.3f);

            foreach (var line in script.lines)
            {
                if (line.flash || line.changeBackground != null)
                    yield return FlashRoutine(line.changeBackground);

                ShowSpeaker(line);
                yield return TypeLine(line.text);

                advanceRequested = false;
                continueHint.gameObject.SetActive(true);
                while (!advanceRequested) yield return null;
                continueHint.gameObject.SetActive(false);
            }

            Finish();
        }

        private void ShowSpeaker(StoryLine line)
        {
            switch (line.speaker)
            {
                case Speaker.Albularyo:
                    namePlate.SetActive(true);
                    nameLabel.text = albularyoName;
                    nameLabel.color = AlbularyoColor;
                    break;
                case Speaker.Lola:
                    namePlate.SetActive(true);
                    nameLabel.text = line.lolaOnStage ? lolaName : lolaName + "  <size=70%><i>(through the book)</i></size>";
                    nameLabel.color = LolaColor;
                    break;
                default:
                    namePlate.SetActive(false);
                    break;
            }
            bodyText.fontStyle = line.speaker == Speaker.Narrator ? FontStyles.Italic : FontStyles.Normal;

            // The speaker stands out; the listener dims a little
            if (albularyo != null)
            {
                var img = albularyo.GetComponent<RawImage>();
                img.color = line.speaker == Speaker.Lola ? new Color(0.6f, 0.6f, 0.68f, 1f) : Color.white;
            }
            SetLolaVisible(line.lolaOnStage, instant: false, dim: line.speaker != Speaker.Lola);
        }

        private IEnumerator TypeLine(string text)
        {
            bodyText.text = text;
            bodyText.ForceMeshUpdate();
            int total = bodyText.textInfo.characterCount;
            bodyText.maxVisibleCharacters = 0;
            advanceRequested = false;

            float shown = 0f;
            while (shown < total)
            {
                if (advanceRequested) break;   // first click completes the line
                shown += charsPerSecond * Time.deltaTime;
                bodyText.maxVisibleCharacters = Mathf.Min(total, (int)shown);
                yield return null;
            }
            bodyText.maxVisibleCharacters = total;
            advanceRequested = false;
        }

        private IEnumerator FlashRoutine(Texture2D newBackground)
        {
            yield return Fade(flashGroup, 1f, 0.25f);
            if (newBackground != null && background != null) background.texture = newBackground;
            yield return new WaitForSeconds(0.2f);
            yield return Fade(flashGroup, 0f, 0.7f);
        }

        private void SetLolaVisible(bool onStage, bool instant, bool dim = false)
        {
            if (lolaGroup == null) return;
            float target = onStage ? (dim ? 0.7f : 1f) : 0f;
            if (instant) { lolaGroup.alpha = target; return; }
            if (lolaFade != null) StopCoroutine(lolaFade);
            lolaFade = StartCoroutine(Fade(lolaGroup, target, 0.35f));
        }

        private void Finish()
        {
            if (leaving) return;
            leaving = true;
            StopAllCoroutines();
            StartCoroutine(LeaveRoutine());
        }

        private IEnumerator LeaveRoutine()
        {
            yield return Fade(fadeGroup, 1f, 0.6f);
            string next = script != null && !string.IsNullOrEmpty(script.nextScene) ? script.nextScene : "MainMenu";
            SceneManager.LoadScene(next);
        }

        private static IEnumerator Fade(CanvasGroup group, float to, float duration)
        {
            float from = group.alpha;
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                group.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }
            group.alpha = to;
        }
    }
}
