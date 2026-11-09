using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Orasyon.Battle
{
    /// <summary>
    /// Plays sprite-sheet effects on the battle stage using a small pool of RawImages.
    /// Positions use the same space as BattleManager's effect helpers (anchored to bottom-center of the layer).
    /// </summary>
    public sealed class VfxPlayer : MonoBehaviour
    {
        [Tooltip("Where effects are spawned (the stage's Characters layer).")]
        [SerializeField] private RectTransform layer;

        private readonly Stack<RawImage> pool = new Stack<RawImage>();

        public void SetLayer(RectTransform value) => layer = value;

        /// <summary>Starts an effect and returns how long it lasts. Null definitions are ignored.</summary>
        public float Play(VfxDefinition def, Vector2 position, float scale = 1f, bool flipX = false)
        {
            if (!IsValid(def) || layer == null) return 0f;
            StartCoroutine(PlayRoutine(def, position, scale, flipX));
            return def.Duration;
        }

        /// <summary>Plays an effect and waits until it ends.</summary>
        public IEnumerator PlayRoutine(VfxDefinition def, Vector2 position, float scale = 1f, bool flipX = false)
        {
            if (!IsValid(def) || layer == null) yield break;
            var img = Rent(def, position, scale, flipX);
            int frames = def.clip.Length;
            float fps = Mathf.Max(1f, def.fps);

            for (float t = 0f; ; t += Time.deltaTime)
            {
                int f = (int)(t * fps);
                if (f >= frames)
                {
                    if (!def.loop) break;
                    f %= frames;
                }
                def.clip.Apply(img, f);
                yield return null;
            }
            Return(img);
        }

        /// <summary>Moves an effect from one point to another (looping its frames), then removes it.</summary>
        public IEnumerator TravelRoutine(VfxDefinition def, Vector2 from, Vector2 to, float duration, float scale = 1f)
        {
            if (!IsValid(def) || layer == null) yield break;
            var img = Rent(def, from, scale, flipX: to.x < from.x);
            int frames = def.clip.Length;
            float fps = Mathf.Max(1f, def.fps);

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                img.rectTransform.anchoredPosition = Vector2.Lerp(from, to, t / duration);
                def.clip.Apply(img, (int)(t * fps) % frames);
                yield return null;
            }
            Return(img);
        }

        // ── Internal ─────────────────────────────────────────────────────────
        private static bool IsValid(VfxDefinition def) => def != null && def.clip != null && !def.clip.IsEmpty;

        private RawImage Rent(VfxDefinition def, Vector2 position, float scale, bool flipX)
        {
            RawImage img = null;
            while (pool.Count > 0 && img == null) img = pool.Pop();
            if (img == null)
            {
                var go = new GameObject("VFX", typeof(RectTransform), typeof(RawImage));
                img = go.GetComponent<RawImage>();
                img.raycastTarget = false;
            }

            var rt = img.rectTransform;
            rt.SetParent(layer, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, def.anchorBottom ? 0f : 0.5f);
            rt.anchoredPosition = position;
            float h = def.size * scale;
            rt.sizeDelta = new Vector2(h * def.clip.Aspect, h);
            rt.localScale = new Vector3(flipX ? -1f : 1f, 1f, 1f);
            rt.SetAsLastSibling();
            img.color = def.tint;
            def.clip.Apply(img, 0);
            img.gameObject.SetActive(true);
            return img;
        }

        private void Return(RawImage img)
        {
            if (img == null) return;
            img.gameObject.SetActive(false);
            pool.Push(img);
        }
    }
}
