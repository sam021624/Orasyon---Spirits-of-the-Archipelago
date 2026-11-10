using Orasyon.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Orasyon.UI
{
    /// <summary>
    /// Esc pauses the battle: Resume, Restart Chapter, sound switches and Main Menu.
    /// Built in code when the battle starts, so scenes don't need extra objects. Time stands still while it is open.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        private BattleManager battle;
        private GameObject    root;
        private Button        musicButton, sfxButton;

        public bool IsOpen => root != null && root.activeSelf;

        public void Init(BattleManager battleManager)
        {
            battle = battleManager;
            Build();
            root.SetActive(false);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || battle == null || root == null) return;
            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (IsOpen) Resume();
                else if (!battle.IsBattleOver) Open();
            }
        }

        private void OnDestroy()
        {
            if (Time.timeScale == 0f) Time.timeScale = 1f;
        }

        public void Open()
        {
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            Time.timeScale = 0f;
            RefreshToggles();
            AudioManager.Play(Sfx.Click);
        }

        public void Resume()
        {
            Time.timeScale = 1f;
            root.SetActive(false);
        }

        // ── UI ───────────────────────────────────────────────────────────────
        private void Build()
        {
            root = new GameObject("PauseMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var dim = Panel("Dim", root.transform, new Color(0f, 0f, 0f, 0.65f));
            Stretch(dim);

            var box = Panel("Box", root.transform, new Color(0.13f, 0.10f, 0.07f, 0.97f));
            Center(box, new Vector2(620, 820));
            var outline = box.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.85f, 0.68f, 0.30f);
            outline.effectDistance = new Vector2(3, -3);

            Label("Title", box, "PAUSED", 64, new Vector2(0, 340), new Vector2(560, 90), new Color(1f, 0.85f, 0.4f));
            MakeButton("Resume", box, "RESUME", 215, Resume);
            MakeButton("Restart", box, "RESTART CHAPTER", 100, () => battle.RestartChapter());
            musicButton = MakeButton("Music", box, "MUSIC: ON", -15, () => { GameSettings.MusicOn = !GameSettings.MusicOn; RefreshToggles(); });
            sfxButton   = MakeButton("Sfx", box, "SOUND: ON", -130, () => { GameSettings.SfxOn = !GameSettings.SfxOn; RefreshToggles(); });
            MakeButton("Menu", box, "MAIN MENU", -245, () => battle.QuitToMenu());

            Label("Keys", box,
                  "Keys:  letters pick tiles   Backspace undo   Enter attack\nTab scramble   Space hint   1 2 3 potions   / Orasyon   Esc pause",
                  20, new Vector2(0, -350), new Vector2(580, 90), new Color(0.8f, 0.75f, 0.6f));
        }

        private void RefreshToggles()
        {
            SetText(musicButton, GameSettings.MusicOn ? "MUSIC: ON" : "MUSIC: OFF");
            SetText(sfxButton,   GameSettings.SfxOn   ? "SOUND: ON" : "SOUND: OFF");
        }

        private static void SetText(Button b, string text)
        {
            var t = b != null ? b.GetComponentInChildren<TextMeshProUGUI>() : null;
            if (t != null) t.text = text;
        }

        private static RectTransform Panel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void Center(RectTransform rt, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
        }

        private static TextMeshProUGUI Label(string name, RectTransform parent, string text, float size, Vector2 pos, Vector2 box, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            Center(rt, box);
            rt.anchoredPosition = pos;
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return t;
        }

        private static Button MakeButton(string name, RectTransform parent, string text, float y, UnityEngine.Events.UnityAction onClick)
        {
            var rt = Panel(name, parent, new Color(0.30f, 0.20f, 0.10f, 1f));
            Center(rt, new Vector2(480, 90));
            rt.anchoredPosition = new Vector2(0, y);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = rt.GetComponent<Image>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1.2f, 1.1f, 0.9f);
            colors.pressedColor = new Color(0.8f, 0.7f, 0.6f);
            button.colors = colors;
            button.onClick.AddListener(onClick);
            Label("Text", rt, text, 36, Vector2.zero, new Vector2(460, 80), new Color(1f, 0.93f, 0.75f));
            rt.gameObject.AddComponent<UiSfx>();
            return button;
        }
    }
}
