using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Orasyon.Battle
{
    /// <summary>"YOUR TURN" / "ENEMY TURN" banner at the bottom of the battle stage. It pops in when the turn changes.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class TurnIndicator : MonoBehaviour
    {
        [SerializeField] private Image           background;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Color           playerColor = new Color(0.20f, 0.46f, 0.22f, 0.92f);
        [SerializeField] private Color           enemyColor  = new Color(0.58f, 0.16f, 0.14f, 0.92f);

        private CanvasGroup group;
        private Coroutine   routine;
        private bool        showingPlayer;
        private bool        visible;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        public void ShowPlayerTurn() => Show(true);
        public void ShowEnemyTurn()  => Show(false);

        public void Hide()
        {
            visible = false;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(FadeOut());
        }

        private void Show(bool player)
        {
            if (visible && showingPlayer == player) return;   // already showing this turn
            visible = true;
            showingPlayer = player;
            if (label != null) label.text = player ? "YOUR TURN" : "ENEMY TURN";
            if (background != null) background.color = player ? playerColor : enemyColor;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(PopIn());
        }

        private IEnumerator PopIn()
        {
            var rt = (RectTransform)transform;
            const float duration = 0.3f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float e = 1f - Mathf.Pow(1f - t / duration, 3f);
                group.alpha = e;
                rt.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, e);
                yield return null;
            }
            group.alpha = 1f;
            rt.localScale = Vector3.one;
            routine = null;
        }

        private IEnumerator FadeOut()
        {
            float start = group.alpha;
            const float duration = 0.2f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(start, 0f, t / duration);
                yield return null;
            }
            group.alpha = 0f;
            routine = null;
        }
    }
}
