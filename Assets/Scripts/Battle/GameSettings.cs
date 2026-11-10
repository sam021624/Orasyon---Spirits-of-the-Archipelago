using System;
using UnityEngine;

namespace Orasyon.Battle
{
    /// <summary>Player preferences, saved in PlayerPrefs. Anything that shows or plays them listens to <see cref="Changed"/>.</summary>
    public static class GameSettings
    {
        private const string MusicVolKey = "orasyon.musicVolume";
        private const string SfxVolKey   = "orasyon.sfxVolume";
        private const string MusicOnKey  = "orasyon.musicOn";
        private const string SfxOnKey    = "orasyon.sfxOn";

        public static event Action Changed;

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicVolKey, 0.6f);
            set { PlayerPrefs.SetFloat(MusicVolKey, Mathf.Clamp01(value)); Notify(); }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxVolKey, 0.9f);
            set { PlayerPrefs.SetFloat(SfxVolKey, Mathf.Clamp01(value)); Notify(); }
        }

        public static bool MusicOn
        {
            get => PlayerPrefs.GetInt(MusicOnKey, 1) == 1;
            set { PlayerPrefs.SetInt(MusicOnKey, value ? 1 : 0); Notify(); }
        }

        public static bool SfxOn
        {
            get => PlayerPrefs.GetInt(SfxOnKey, 1) == 1;
            set { PlayerPrefs.SetInt(SfxOnKey, value ? 1 : 0); Notify(); }
        }

        private static void Notify()
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
