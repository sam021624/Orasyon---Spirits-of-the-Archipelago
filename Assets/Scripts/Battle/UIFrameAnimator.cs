using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Orasyon.Battle
{
    /// <summary>
    /// Plays sprite-sheet animations (<see cref="FrameClip"/>) on a UI RawImage.
    /// Loops the idle clip and returns to it after one-shot clips (attack, hurt).
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class UIFrameAnimator : MonoBehaviour
    {
        [SerializeField] private float framesPerSecond = 10f;
        [Tooltip("Optional: loop this character's idle animation automatically (e.g. on the title screen).")]
        [SerializeField] private CharacterDefinition playIdleOnStart;

        private RawImage  image;
        private FrameClip idleClip;
        private FrameClip currentClip;
        private int       frame;
        private float     timer;
        private bool      looping;
        private bool      holdLastFrame;
        private Coroutine flashRoutine;

        public float FramesPerSecond
        {
            get => framesPerSecond;
            set => framesPerSecond = Mathf.Max(1f, value);
        }

        private void Awake()
        {
            image = GetComponent<RawImage>();
        }

        private void Start()
        {
            if (playIdleOnStart != null && currentClip == null)
            {
                FramesPerSecond = playIdleOnStart.framesPerSecond;
                SetIdle(playIdleOnStart.idle);
            }
        }

        private float RateOf(FrameClip clip) => clip != null && clip.fps > 0f ? clip.fps : framesPerSecond;

        /// <summary>Sets the looping idle clip and starts playing it.</summary>
        public void SetIdle(FrameClip clip)
        {
            idleClip = clip;
            Play(clip, loop: true, hold: false);
        }

        /// <summary>Plays a clip once, then returns to idle (or holds the last frame).</summary>
        public void PlayOnce(FrameClip clip, bool holdLast = false)
        {
            if (clip == null || clip.IsEmpty) return;
            Play(clip, loop: false, hold: holdLast);
        }

        /// <summary>Loops a clip (e.g. walking) without changing the idle clip.</summary>
        public void PlayLoop(FrameClip clip)
        {
            if (clip == null || clip.IsEmpty) return;
            Play(clip, loop: true, hold: false);
        }

        public void ReturnToIdle() => Play(idleClip, loop: true, hold: false);

        /// <summary>How long a clip takes at its frame rate.</summary>
        public float Duration(FrameClip clip)
        {
            return clip == null || clip.IsEmpty ? 0f : clip.Length / RateOf(clip);
        }

        public void SetAlpha(float alpha)
        {
            if (image == null) image = GetComponent<RawImage>();
            Color c = image.color;
            c.a = alpha;
            image.color = c;
        }

        public void Flash(Color tint, float duration)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine(tint, duration));
        }

        public IEnumerator FadeTo(float alpha, float duration)
        {
            float start = image.color.a;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                SetAlpha(Mathf.Lerp(start, alpha, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            SetAlpha(alpha);
        }

        // ── Internal ─────────────────────────────────────────────────────────
        private void Play(FrameClip clip, bool loop, bool hold)
        {
            if (image == null) image = GetComponent<RawImage>();
            currentClip   = clip;
            looping       = loop;
            holdLastFrame = hold;
            frame = 0;
            timer = 0f;
            ShowFrame();
        }

        private void Update()
        {
            if (currentClip == null || currentClip.IsEmpty) return;

            timer += Time.deltaTime;
            float frameTime = 1f / RateOf(currentClip);
            while (timer >= frameTime)
            {
                timer -= frameTime;
                frame++;

                if (frame >= currentClip.Length)
                {
                    if (looping)
                    {
                        frame = 0;
                    }
                    else if (holdLastFrame)
                    {
                        frame = currentClip.Length - 1;
                        ShowFrame();
                        currentClip = null;   // stop advancing, keep the last frame
                        return;
                    }
                    else
                    {
                        Play(idleClip, loop: true, hold: false);
                        return;
                    }
                }
            }
            ShowFrame();
        }

        private void ShowFrame()
        {
            if (currentClip == null || currentClip.IsEmpty) return;
            currentClip.Apply(image, frame);
        }

        private IEnumerator FlashRoutine(Color tint, float duration)
        {
            float alpha = image.color.a;
            tint.a = alpha;
            image.color = tint;
            yield return new WaitForSeconds(duration);
            image.color = new Color(1f, 1f, 1f, alpha);
            flashRoutine = null;
        }
    }
}
