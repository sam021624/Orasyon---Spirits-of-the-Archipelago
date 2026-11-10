using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Orasyon.Battle
{
    public enum Sfx
    {
        Click, Hover, Select, Deselect, Reject, HitLight, HitMedium, HitHeavy, EnemyAttack, Hurt, Heal, Potion,
        Orasyon, Treasure, Victory, Defeat, Star, WordOfPower, Scramble, Hint, EnemyDeath
    }

    public enum MusicTrack { None, Menu, Story, Battle, Boss }

    /// <summary>
    /// Sound for the whole game. Every sound is synthesized at start-up (no audio files, so nothing to license):
    /// short effects are rendered on the spot, the music loops are rendered on a background thread.
    /// If a clip exists at Resources/Audio/Sfx/{Name} or Resources/Audio/Music/{Track}, it is used instead.
    /// Volumes and mute switches come from <see cref="GameSettings"/>.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private const int Rate = 22050;
        private const int VoiceCount = 6;

        private static AudioManager instance;

        private AudioSource musicSource;
        private AudioSource[] voices;
        private int nextVoice;
        private readonly Dictionary<Sfx, AudioClip[]> sfx = new Dictionary<Sfx, AudioClip[]>();
        private readonly Dictionary<MusicTrack, AudioClip> music = new Dictionary<MusicTrack, AudioClip>();
        private readonly Dictionary<MusicTrack, Task<float[]>> pending = new Dictionary<MusicTrack, Task<float[]>>();
        private MusicTrack wanted = MusicTrack.None;
        private MusicTrack playing = MusicTrack.None;
        private readonly float[] lastPlayed = new float[Enum.GetValues(typeof(Sfx)).Length];

        // ── Public API ───────────────────────────────────────────────────────
        public static void Play(Sfx id, float volume = 1f)
        {
            if (instance != null) instance.PlayInternal(id, volume);
        }

        public static void SetMusic(MusicTrack track)
        {
            if (instance != null) instance.wanted = track;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (instance != null) return;
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AudioManager>();
        }

        // ── Setup ────────────────────────────────────────────────────────────
        private void Awake()
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }

            BuildEffects();
            foreach (MusicTrack t in Enum.GetValues(typeof(MusicTrack)))
            {
                if (t == MusicTrack.None) continue;
                var file = Resources.Load<AudioClip>("Audio/Music/" + t);
                if (file != null) { music[t] = file; continue; }
                var track = t;
                pending[t] = Task.Run(() => RenderMusic(track));
            }

            GameSettings.Changed += ApplySettings;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplySettings();
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void OnDestroy()
        {
            GameSettings.Changed -= ApplySettings;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (instance == this) instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            string n = scene.name;
            wanted = n == "MainMenu" ? MusicTrack.Menu : n == "Story" ? MusicTrack.Story : n.StartsWith("Level") ? MusicTrack.Battle : wanted;
            Time.timeScale = 1f;

            // Every button in the scene gets click and hover sounds
            foreach (var root in scene.GetRootGameObjects())
                foreach (var b in root.GetComponentsInChildren<Button>(true))
                    if (b.GetComponent<UiSfx>() == null) b.gameObject.AddComponent<UiSfx>();
        }

        private void ApplySettings()
        {
            musicSource.volume = GameSettings.MusicOn ? GameSettings.MusicVolume * 0.6f : 0f;
        }

        private void Update()
        {
            // Pick up finished background renders
            if (pending.Count > 0)
            {
                MusicTrack done = MusicTrack.None;
                foreach (var kv in pending)
                {
                    if (!kv.Value.IsCompleted) continue;
                    done = kv.Key;
                    break;
                }
                if (done != MusicTrack.None)
                {
                    var task = pending[done];
                    pending.Remove(done);
                    if (!task.IsFaulted)
                    {
                        var data = task.Result;
                        var clip = AudioClip.Create("music_" + done, data.Length, 1, Rate, false);
                        clip.SetData(data, 0);
                        music[done] = clip;
                    }
                }
            }

            if (wanted != playing)
            {
                if (wanted == MusicTrack.None) { musicSource.Stop(); playing = wanted; }
                else if (music.TryGetValue(wanted, out var clip))
                {
                    musicSource.clip = clip;
                    musicSource.Play();
                    playing = wanted;
                }
            }
        }

        // ── Playback ─────────────────────────────────────────────────────────
        private void PlayInternal(Sfx id, float volume)
        {
            if (!GameSettings.SfxOn || !sfx.TryGetValue(id, out var clips) || clips.Length == 0) return;
            float now = Time.unscaledTime;
            if (now - lastPlayed[(int)id] < 0.04f) return;   // stops a sound stacking on itself
            lastPlayed[(int)id] = now;

            var v = voices[nextVoice];
            nextVoice = (nextVoice + 1) % VoiceCount;
            v.pitch = 1f + UnityEngine.Random.Range(-0.04f, 0.04f);
            v.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)], GameSettings.SfxVolume * volume);
        }

        // ── Effects ──────────────────────────────────────────────────────────
        private void Add(Sfx id, params float[][] variants)
        {
            var file = Resources.Load<AudioClip>("Audio/Sfx/" + id);
            if (file != null) { sfx[id] = new[] { file }; return; }
            var clips = new AudioClip[variants.Length];
            for (int i = 0; i < variants.Length; i++)
            {
                clips[i] = AudioClip.Create("sfx_" + id + i, variants[i].Length, 1, Rate, false);
                clips[i].SetData(variants[i], 0);
            }
            sfx[id] = clips;
        }

        private void BuildEffects()
        {
            Add(Sfx.Click,      Synth.Tone(660, 0.07f, 0.5f, Synth.Wave.Sine, 880), Synth.Tone(780, 0.07f, 0.5f, Synth.Wave.Sine, 980));
            Add(Sfx.Hover,      Synth.Tone(900, 0.04f, 0.18f, Synth.Wave.Sine));
            Add(Sfx.Select,     Synth.Gong(523.25f, 0.18f, 0.5f), Synth.Gong(587.33f, 0.18f, 0.5f), Synth.Gong(659.25f, 0.18f, 0.5f), Synth.Gong(783.99f, 0.18f, 0.5f));
            Add(Sfx.Deselect,   Synth.Tone(420, 0.09f, 0.35f, Synth.Wave.Sine, 300));
            Add(Sfx.Reject,     Synth.Mix(Synth.Tone(160, 0.22f, 0.55f, Synth.Wave.Square, 110), Synth.Tone(166, 0.22f, 0.4f, Synth.Wave.Square, 114)));
            Add(Sfx.HitLight,   Synth.Mix(Synth.Noise(0.12f, 0.6f, 2500), Synth.Tone(240, 0.12f, 0.5f, Synth.Wave.Sine, 120)));
            Add(Sfx.HitMedium,  Synth.Mix(Synth.Noise(0.2f, 0.5f, 3500), Synth.Gong(392f, 0.4f, 0.5f), Synth.Tone(180, 0.2f, 0.6f, Synth.Wave.Sine, 70)));
            Add(Sfx.HitHeavy,   Synth.Mix(Synth.Noise(0.35f, 0.7f, 5000), Synth.Gong(293.66f, 0.8f, 0.6f), Synth.Gong(587.33f, 0.7f, 0.4f), Synth.Tone(120, 0.35f, 0.8f, Synth.Wave.Sine, 40)));
            Add(Sfx.EnemyAttack, Synth.Mix(Synth.Noise(0.25f, 0.5f, 1800), Synth.Tone(200, 0.25f, 0.5f, Synth.Wave.Saw, 90)));
            Add(Sfx.Hurt,       Synth.Mix(Synth.Tone(220, 0.25f, 0.6f, Synth.Wave.Saw, 110), Synth.Noise(0.15f, 0.4f, 1500)));
            Add(Sfx.Heal,       Synth.Seq(0.09f, 0.3f, 523.25f, 659.25f, 783.99f, 1046.5f));
            Add(Sfx.Potion,     Synth.Seq(0.07f, 0.28f, 392f, 523.25f, 659.25f));
            Add(Sfx.Orasyon,    Synth.Mix(Synth.Seq(0.1f, 0.35f, 392f, 493.88f, 587.33f, 783.99f, 987.77f), Synth.Tone(98f, 1.0f, 0.4f, Synth.Wave.Sine)));
            Add(Sfx.Treasure,   Synth.Seq(0.12f, 0.4f, 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f));
            Add(Sfx.Victory,    Synth.Seq(0.16f, 0.5f, 392f, 523.25f, 659.25f, 783.99f, 1046.5f));
            Add(Sfx.Defeat,     Synth.Seq(0.25f, 0.5f, 392f, 349.23f, 293.66f, 196f));
            Add(Sfx.Star,       Synth.Seq(0.05f, 0.4f, 880f, 1318.5f));
            Add(Sfx.WordOfPower, Synth.Mix(Synth.Seq(0.06f, 0.35f, 659.25f, 880f, 1318.5f), Synth.Noise(0.3f, 0.2f, 6000)));
            Add(Sfx.Scramble,   Synth.Noise(0.25f, 0.35f, 3000));
            Add(Sfx.Hint,       Synth.Seq(0.08f, 0.3f, 783.99f, 987.77f));
            Add(Sfx.EnemyDeath, Synth.Mix(Synth.Tone(300, 0.6f, 0.5f, Synth.Wave.Saw, 50), Synth.Noise(0.4f, 0.3f, 1200)));
        }

        // ── Music ────────────────────────────────────────────────────────────
        /// <summary>
        /// A short loop in a pentatonic scale with gong-like plucks over a drone, loosely in the spirit of kulintang
        /// music. Runs on a worker thread, so it must not touch any Unity API.
        /// </summary>
        private static float[] RenderMusic(MusicTrack track)
        {
            float bpm; float[] scale; float density; float droneNote; float level;
            switch (track)
            {
                case MusicTrack.Boss:   bpm = 132; scale = new[] { 0f, 1f, 5f, 7f, 8f };   density = 0.75f; droneNote = 55f;    level = 0.9f; break;
                case MusicTrack.Battle: bpm = 108; scale = new[] { 0f, 2f, 4f, 7f, 9f };   density = 0.55f; droneNote = 65.41f; level = 0.8f; break;
                case MusicTrack.Story:  bpm = 66;  scale = new[] { 0f, 3f, 5f, 7f, 10f };  density = 0.30f; droneNote = 73.42f; level = 0.7f; break;
                default:                bpm = 78;  scale = new[] { 0f, 2f, 4f, 7f, 9f };   density = 0.40f; droneNote = 65.41f; level = 0.75f; break;
            }

            const int bars = 8;
            float beat = 60f / bpm;
            float eighth = beat / 2f;
            int steps = bars * 8;
            int total = Mathf.RoundToInt(steps * eighth * Rate);
            var buf = new float[total];
            var rng = new System.Random((int)track * 7919 + 13);
            float root = 261.63f * (track == MusicTrack.Boss ? 0.5f : 1f);

            // Drone: two soft sines a fifth apart. Whole periods fit the loop so it joins without a click.
            float seconds = total / (float)Rate;
            float d1 = Mathf.Round(droneNote * seconds) / seconds, d2 = Mathf.Round(droneNote * 1.5f * seconds) / seconds;
            for (int i = 0; i < total; i++)
            {
                float t = i / (float)Rate;
                float swell = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * t / seconds * 2f);
                buf[i] += (Mathf.Sin(2f * Mathf.PI * d1 * t) * 0.16f + Mathf.Sin(2f * Mathf.PI * d2 * t) * 0.07f) * swell;
            }

            // Motif of 16 steps, repeated with small changes
            int[] motif = new int[16];
            int degree = 2;
            for (int i = 0; i < motif.Length; i++)
            {
                degree = Mathf.Clamp(degree + rng.Next(-2, 3), 0, 9);
                motif[i] = rng.NextDouble() < density ? degree : -1;
            }
            motif[0] = 0;

            for (int s = 0; s < steps; s++)
            {
                int m = motif[s % motif.Length];
                if (s >= 16 && rng.NextDouble() < 0.2) m = rng.NextDouble() < 0.5 ? -1 : Mathf.Clamp(m + 1, 0, 9);
                int start = Mathf.RoundToInt(s * eighth * Rate);

                if (m >= 0)
                {
                    float semi = scale[m % 5] + 12f * (m / 5);
                    Pluck(buf, start, root * Mathf.Pow(2f, semi / 12f), 0.22f * level, 1.2f);
                }
                // Low gong on the downbeat of every bar, a lighter knock on beat three
                if (s % 8 == 0) Pluck(buf, start, droneNote * 2f, 0.28f * level, 2.2f);
                else if (s % 8 == 4) Pluck(buf, start, droneNote * 3f, 0.14f * level, 1.2f);
                if (track == MusicTrack.Boss && s % 2 == 0) Pluck(buf, start, droneNote, 0.18f, 0.35f);
            }

            // Keep it from clipping
            float peak = 0.0001f;
            for (int i = 0; i < total; i++) peak = Mathf.Max(peak, Mathf.Abs(buf[i]));
            float g = 0.8f / peak;
            for (int i = 0; i < total; i++) buf[i] *= g;
            return buf;
        }

        /// <summary>Adds a struck-gong note (sine plus soft overtones, quick attack, exponential decay). Wraps at the loop end.</summary>
        private static void Pluck(float[] buf, int start, float freq, float amp, float decaySeconds)
        {
            int len = Mathf.Min(Mathf.RoundToInt(decaySeconds * 2.5f * Rate), buf.Length);
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Min(1f, t * 400f) * Mathf.Exp(-t * 4.6f / decaySeconds);
                float w = Mathf.Sin(2f * Mathf.PI * freq * t)
                        + 0.35f * Mathf.Sin(2f * Mathf.PI * freq * 2.01f * t) * Mathf.Exp(-t * 8f)
                        + 0.2f  * Mathf.Sin(2f * Mathf.PI * freq * 3.5f * t) * Mathf.Exp(-t * 14f);
                buf[(start + i) % buf.Length] += w * env * amp;
            }
        }

        // ── Synthesis helpers (used on the main thread for effects) ──────────
        private static class Synth
        {
            public enum Wave { Sine, Square, Saw }

            private static float Osc(Wave w, float phase)
            {
                switch (w)
                {
                    case Wave.Square: return Mathf.Sin(phase) >= 0 ? 0.6f : -0.6f;
                    case Wave.Saw:    return ((phase / (2f * Mathf.PI)) % 1f) * 2f - 1f;
                    default:          return Mathf.Sin(phase);
                }
            }

            /// <summary>A tone with an optional pitch slide to <paramref name="endFreq"/>.</summary>
            public static float[] Tone(float freq, float dur, float amp, Wave wave, float endFreq = 0f)
            {
                int n = Mathf.RoundToInt(dur * Rate);
                var d = new float[n];
                float phase = 0f;
                for (int i = 0; i < n; i++)
                {
                    float p = i / (float)n;
                    float f = endFreq > 0f ? Mathf.Lerp(freq, endFreq, p) : freq;
                    phase += 2f * Mathf.PI * f / Rate;
                    float env = Mathf.Min(1f, i / (Rate * 0.004f)) * (1f - p) * (1f - p);
                    d[i] = Osc(wave, phase) * env * amp;
                }
                return d;
            }

            public static float[] Gong(float freq, float dur, float amp)
            {
                int n = Mathf.RoundToInt(dur * Rate);
                var d = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)Rate;
                    float env = Mathf.Min(1f, t * 600f) * Mathf.Exp(-t * 5f / Mathf.Max(0.1f, dur));
                    d[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * freq * 2.76f * t) * Mathf.Exp(-t * 12f)) * env * amp;
                }
                return d;
            }

            /// <summary>White noise run through a one-pole low-pass at <paramref name="cutoff"/> Hz, fading out.</summary>
            public static float[] Noise(float dur, float amp, float cutoff)
            {
                int n = Mathf.RoundToInt(dur * Rate);
                var d = new float[n];
                var rng = new System.Random(n + (int)cutoff);
                float a = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / Rate), y = 0f;
                for (int i = 0; i < n; i++)
                {
                    float p = i / (float)n;
                    y += a * (((float)rng.NextDouble() * 2f - 1f) - y);
                    d[i] = y * (1f - p) * (1f - p) * amp * 1.6f;
                }
                return d;
            }

            /// <summary>Notes played one after another (a rising chime or a falling sting).</summary>
            public static float[] Seq(float step, float amp, params float[] freqs)
            {
                float tail = 0.35f;
                int n = Mathf.RoundToInt((step * (freqs.Length - 1) + tail) * Rate);
                var d = new float[n];
                for (int k = 0; k < freqs.Length; k++)
                {
                    var g = Gong(freqs[k], tail + 0.1f, amp);
                    int start = Mathf.RoundToInt(k * step * Rate);
                    for (int i = 0; i < g.Length && start + i < n; i++) d[start + i] += g[i];
                }
                return d;
            }

            public static float[] Mix(params float[][] parts)
            {
                int n = 0;
                foreach (var p in parts) n = Mathf.Max(n, p.Length);
                var d = new float[n];
                foreach (var p in parts) for (int i = 0; i < p.Length; i++) d[i] += p[i];
                for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(d[i], -0.95f, 0.95f);
                return d;
            }
        }
    }

    /// <summary>Added to every button by <see cref="AudioManager"/>: a soft tick on hover and a click on press.</summary>
    public sealed class UiSfx : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        public void OnPointerEnter(PointerEventData e)
        {
            var b = GetComponent<Button>();
            if (b != null && b.interactable) AudioManager.Play(Sfx.Hover);
        }

        public void OnPointerClick(PointerEventData e)
        {
            var b = GetComponent<Button>();
            if (b != null && b.interactable) AudioManager.Play(Sfx.Click);
        }
    }
}
