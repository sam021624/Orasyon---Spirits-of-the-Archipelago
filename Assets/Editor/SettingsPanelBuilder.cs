using Orasyon.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Orasyon.EditorTools
{
    /// <summary>Builds the settings panel (music and sound sliders, mute buttons) used by the main menu and the pause menu.</summary>
    public static class SettingsPanelBuilder
    {
        private static readonly Color Wood  = new Color(0.16f, 0.10f, 0.06f, 1f);
        private static readonly Color Gold  = new Color(0.88f, 0.68f, 0.30f, 1f);
        private static readonly Color Cream = new Color(0.97f, 0.91f, 0.76f, 1f);

        public static SettingsPanel Build(Transform parent)
        {
            var rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var panel   = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

            var root = Node("SettingsRoot", parent);
            Stretch(root);
            var dim = Img("Dimmer", root, new Color(0, 0, 0, 0.78f), null);
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;   // blocks clicks on whatever is behind

            var box = Img("Panel", root, Wood, panel);
            Anchor(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 470));
            var outline = box.gameObject.AddComponent<Outline>();
            outline.effectColor = Gold; outline.effectDistance = new Vector2(3, -3);

            var title = Text("Title", box.transform, "SETTINGS", 44, Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            Anchor(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(700, 56));
            title.characterSpacing = 8;

            var musicSlider = Row(box.transform, "Music", -120, rounded, out var musicToggle);
            var sfxSlider   = Row(box.transform, "Sound effects", -220, rounded, out var sfxToggle);

            var close = Button(box.transform, "CloseButton", "BACK", rounded, new Color(0.78f, 0.56f, 0.20f), new Color(0.28f, 0.16f, 0.07f), 30);
            Anchor((RectTransform)close.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(300, 70));

            var comp = root.gameObject.AddComponent<SettingsPanel>();
            var so = new SerializedObject(comp);
            so.FindProperty("root").objectReferenceValue = root.gameObject;
            so.FindProperty("musicSlider").objectReferenceValue = musicSlider;
            so.FindProperty("sfxSlider").objectReferenceValue = sfxSlider;
            so.FindProperty("musicToggle").objectReferenceValue = musicToggle;
            so.FindProperty("sfxToggle").objectReferenceValue = sfxToggle;
            so.FindProperty("closeButton").objectReferenceValue = close;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.gameObject.SetActive(false);
            return comp;
        }

        private static Slider Row(Transform box, string label, float y, Sprite rounded, out Button toggle)
        {
            var text = Text(label + "Label", box, label, 28, Cream, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            Anchor(text.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(50, y), new Vector2(260, 44));

            var slider = MakeSlider(box, label + "Slider", rounded);
            Anchor((RectTransform)slider.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(320, y), new Vector2(330, 36));

            toggle = Button(box, label + "Toggle", "ON", rounded, new Color(0.25f, 0.15f, 0.09f), Cream, 24);
            Anchor((RectTransform)toggle.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-50, y), new Vector2(110, 52));
            return slider;
        }

        private static Slider MakeSlider(Transform parent, string name, Sprite rounded)
        {
            var root = Node(name, parent);
            var slider = root.gameObject.AddComponent<Slider>();

            var bg = Img("Background", root, new Color(0.05f, 0.03f, 0.02f, 1f), rounded);
            Anchor(bg.rectTransform, new Vector2(0, 0.25f), new Vector2(1, 0.75f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var fillArea = Node("Fill Area", root);
            Anchor(fillArea, new Vector2(0, 0.25f), new Vector2(1, 0.75f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            fillArea.offsetMin = new Vector2(6, 0); fillArea.offsetMax = new Vector2(-14, 0);
            var fill = Img("Fill", fillArea, Gold, rounded);
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(0, 1);
            fill.rectTransform.sizeDelta = new Vector2(10, 0);

            var handleArea = Node("Handle Slide Area", root);
            Anchor(handleArea, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            handleArea.offsetMin = new Vector2(10, 0); handleArea.offsetMax = new Vector2(-10, 0);
            var handle = Img("Handle", handleArea, Cream, rounded);
            handle.rectTransform.sizeDelta = new Vector2(24, 0);
            handle.raycastTarget = true;

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = 0.6f;
            return slider;
        }

        private static Button Button(Transform parent, string name, string label, Sprite rounded, Color face, Color labelColor, float fontSize)
        {
            var img = Img(name, parent, face, rounded);
            var o = img.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.10f, 0.06f, 0.03f, 0.9f); o.effectDistance = new Vector2(2, -2);
            var btn = img.gameObject.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            var t = Text("Label", img.transform, label, fontSize, labelColor, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(t.rectTransform);
            return btn;
        }

        // ── tiny UI helpers ──────────────────────────────────────────────────
        private static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image Img(string name, Transform parent, Color color, Sprite sprite)
        {
            var img = Node(name, parent).gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }
            return img;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align, FontStyles style)
        {
            var t = Node(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align; t.fontStyle = style;
            t.raycastTarget = false;
            return t;
        }

        private static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
