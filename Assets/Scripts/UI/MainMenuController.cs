using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Orasyon.UI
{
    /// <summary>
    /// Drives the Orasyon title screen: intro fade, title bob, how-to-play panel,
    /// and the transition into the first battle scene.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Scene Flow")]
        [SerializeField] private string firstSceneName = "Level1";

        [Header("References")]
        [SerializeField] private RectTransform titleGroup;
        [SerializeField] private CanvasGroup menuGroup;
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private GameObject howToRoot;
        [SerializeField] private CanvasGroup howToGroup;
        [SerializeField] private GameObject firstSelected;

        [Header("Motion")]
        [SerializeField] private float titleBobHeight = 8f;
        [SerializeField] private float titleBobSpeed = 1.2f;
        [SerializeField] private float introFadeDuration = 0.9f;
        [SerializeField] private float menuFadeDuration = 0.4f;
        [SerializeField] private float fadeOutDuration = 0.55f;

        private Vector2 titleBasePosition;
        private float bobTime;
        private bool busy;
        private bool howToVisible;
        private Coroutine howToRoutine;

        private void Awake()
        {
            if (titleGroup != null)
            {
                titleBasePosition = titleGroup.anchoredPosition;
            }

            if (howToRoot != null)
            {
                howToRoot.SetActive(false);
            }

            if (fadeGroup != null)
            {
                fadeGroup.alpha = 1f;
                fadeGroup.blocksRaycasts = true;
            }
        }

        private void Start()
        {
            StartCoroutine(IntroRoutine());
            Select(firstSelected);
        }

        private void Update()
        {
            if (titleGroup != null)
            {
                bobTime += Time.unscaledDeltaTime;
                float offset = Mathf.Sin(bobTime * titleBobSpeed) * titleBobHeight;
                titleGroup.anchoredPosition = titleBasePosition + new Vector2(0f, offset);
            }

            if (UnityEngine.InputSystem.Keyboard.current != null
                && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame
                && howToVisible)
            {
                HideHowTo();
            }
        }

        // ---- Button handlers -------------------------------------------------

        public void StartGame()
        {
            if (busy)
            {
                return;
            }

            busy = true;
            StartCoroutine(LoadSceneRoutine());
        }

        public void ShowHowTo()
        {
            if (busy || howToRoot == null)
            {
                return;
            }

            howToVisible = true;
            howToRoot.SetActive(true);
            if (menuGroup != null)
            {
                menuGroup.interactable = false;
                menuGroup.blocksRaycasts = false;
            }

            if (howToRoutine != null)
            {
                StopCoroutine(howToRoutine);
            }

            howToRoutine = StartCoroutine(FadeCanvas(howToGroup, 0f, 1f, 0.22f));
        }

        public void HideHowTo()
        {
            if (!howToVisible || howToRoot == null)
            {
                return;
            }

            howToVisible = false;
            if (howToRoutine != null)
            {
                StopCoroutine(howToRoutine);
            }

            howToRoutine = StartCoroutine(HideHowToRoutine());
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---- Routines --------------------------------------------------------

        private IEnumerator IntroRoutine()
        {
            if (menuGroup != null)
            {
                menuGroup.alpha = 0f;
            }

            yield return FadeCanvas(fadeGroup, 1f, 0f, introFadeDuration);
            yield return FadeCanvas(menuGroup, 0f, 1f, menuFadeDuration);

            if (fadeGroup != null)
            {
                fadeGroup.blocksRaycasts = false;
            }
        }

        private IEnumerator HideHowToRoutine()
        {
            yield return FadeCanvas(howToGroup, howToGroup != null ? howToGroup.alpha : 1f, 0f, 0.18f);

            if (howToRoot != null)
            {
                howToRoot.SetActive(false);
            }

            if (menuGroup != null)
            {
                menuGroup.interactable = true;
                menuGroup.blocksRaycasts = true;
            }
        }

        private IEnumerator LoadSceneRoutine()
        {
            if (fadeGroup != null)
            {
                fadeGroup.blocksRaycasts = true;
            }

            yield return FadeCanvas(fadeGroup, fadeGroup != null ? fadeGroup.alpha : 0f, 1f, fadeOutDuration);

            if (!string.IsNullOrEmpty(firstSceneName))
            {
                SceneManager.LoadScene(firstSceneName);
            }
        }

        private static IEnumerator FadeCanvas(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null)
            {
                yield break;
            }

            group.alpha = from;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            group.alpha = to;
        }

        private static void Select(GameObject target)
        {
            if (target == null || EventSystem.current == null)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(target);
        }
    }
}
