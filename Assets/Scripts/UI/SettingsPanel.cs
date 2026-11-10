using Orasyon.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Orasyon.UI
{
    /// <summary>Music and sound volume sliders with mute buttons. Used by the main menu and the pause menu.</summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject      root;
        [SerializeField] private Slider          musicSlider;
        [SerializeField] private Slider          sfxSlider;
        [SerializeField] private Button          musicToggle;
        [SerializeField] private Button          sfxToggle;
        [SerializeField] private Button          closeButton;

        public bool IsOpen => root != null && root.activeSelf;

        private bool wired;

        private void Wire()
        {
            if (wired) return;
            wired = true;
            musicSlider.onValueChanged.AddListener(v => GameSettings.MusicVolume = v);
            sfxSlider.onValueChanged.AddListener(v => GameSettings.SfxVolume = v);
            musicToggle.onClick.AddListener(() => { GameSettings.MusicOn = !GameSettings.MusicOn; Refresh(); });
            sfxToggle.onClick.AddListener(() => { GameSettings.SfxOn = !GameSettings.SfxOn; Refresh(); });
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void Awake() => Wire();

        public void Open()
        {
            Wire();
            root.SetActive(true);
            Refresh();
            root.transform.SetAsLastSibling();
        }

        public void Close() { if (root != null) root.SetActive(false); }

        private void Refresh()
        {
            musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
            sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
            SetLabel(musicToggle, GameSettings.MusicOn ? "ON" : "OFF");
            SetLabel(sfxToggle, GameSettings.SfxOn ? "ON" : "OFF");
        }

        private static void SetLabel(Button b, string text)
        {
            var label = b.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = text;
        }
    }
}
