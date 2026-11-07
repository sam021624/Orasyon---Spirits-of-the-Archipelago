using System.IO;
using System.Linq;
using Orasyon.Battle;
using Orasyon.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Orasyon.EditorTools
{
    /// <summary>
    /// Builds the title screen: drifting moonlit forest, animated Albularyo and Duwende King,
    /// the title spelled in letter tiles, wood-and-gold menu buttons, and a How to Play panel.
    /// Re-running rebuilds the whole MainMenu scene content.
    /// </summary>
    public static class MainMenuBuilder
    {
        private const string ScenePath      = "Assets/Levels/MainMenu.unity";
        private const string BackgroundPack = "Assets/Sprites/Terrains/5";
        private const string UiFolder       = "Assets/Sprites/UI";
        private const string DataFolder     = "Assets/Data/Characters/";

        private static readonly Color Wood      = new Color(0.16f, 0.10f, 0.06f, 0.96f);
        private static readonly Color WoodLight = new Color(0.26f, 0.16f, 0.09f, 0.96f);
        private static readonly Color Gold      = new Color(0.90f, 0.70f, 0.32f, 1f);
        private static readonly Color Cream     = new Color(0.97f, 0.91f, 0.76f, 1f);
        private static readonly Color Parchment = new Color(0.96f, 0.89f, 0.70f, 1f);
        private static readonly Color Ink       = new Color(0.28f, 0.16f, 0.07f, 1f);

        private static Sprite rounded, panel, softGlow, vignette;

        [MenuItem("Tools/Orasyon/Rebuild Main Menu")]
        public static void Build()
        {
            rounded  = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            panel    = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            softGlow = MakeRadialSprite("soft_glow", 128, (d) => Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f));
            vignette = MakeRadialSprite("vignette", 256, (d) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1.15f, d)));
            SetPointFilter(BackgroundPack);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);

            var cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.04f, 0.10f);
            cam.orthographic = true;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var canvas = canvasGo.transform;

            // ── Background: drifting moonlit forest ──────────────────────────
            var bg = Node("Background", canvas);
            Stretch(bg);
            bg.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.14f);
            BuildParallax(bg);
            var tint = Img("NightTint", bg, new Color(0.03f, 0.03f, 0.12f, 0.45f));   // calms the bright forest
            Stretch(tint.rectTransform);

            // Moon with a pulsing halo (top right)
            var halo = Img("MoonHalo", canvas, new Color(0.70f, 0.85f, 1f, 0.55f), softGlow);
            Anchor(halo.rectTransform, Center, new Vector2(620, 330), new Vector2(460, 460));
            halo.gameObject.AddComponent<GlowPulse>();
            var moon = Img("Moon", canvas, new Color(0.93f, 0.95f, 1f, 1f), AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"));
            Anchor(moon.rectTransform, Center, new Vector2(620, 330), new Vector2(120, 120));

            // Fireflies
            var flies = Node("Fireflies", canvas);
            Stretch(flies);
            var rng = new System.Random(4);
            for (int i = 0; i < 16; i++)
            {
                var f = Img("Firefly_" + i, flies, new Color(0.85f, 1f, 0.55f, 0.5f), softGlow);
                float size = 16 + rng.Next(18);
                Anchor(f.rectTransform, Center, new Vector2(rng.Next(-900, 900), rng.Next(-420, 380)), new Vector2(size, size));
                f.gameObject.AddComponent<FireflyDrift>();
            }

            // Characters: hero on the left, the boss lurking on the right
            Character("Albularyo",   canvas, Load<CharacterDefinition>(DataFolder + "Albularyo.asset"),      new Vector2(-640, -560), 470, Color.white);
            Character("DuwendeKing", canvas, Load<CharacterDefinition>(DataFolder + "03_DuwendeKing.asset"), new Vector2( 650, -560), 520, new Color(0.78f, 0.74f, 0.92f));

            // Darken the edges so the title and buttons stand out
            var vig = Img("Vignette", canvas, new Color(0.01f, 0.01f, 0.04f, 0.92f), vignette);
            Stretch(vig.rectTransform);

            // Decorative tiles drifting in the corners (spells BAKUNAWA)
            var deco = Node("FloatingLetterTiles", canvas);
            Stretch(deco);
            string deco1 = "BAKUNAWA";
            Vector2[] decoPos =
            {
                new Vector2(-860, 380), new Vector2(-720, 250), new Vector2(-880, 120), new Vector2(-700, 20),
                new Vector2( 860, 120), new Vector2( 760, -10), new Vector2( 880, -140), new Vector2(-860, -150),
            };
            for (int i = 0; i < deco1.Length; i++)
            {
                var t = LetterTile(deco, deco1[i], decoPos[i], 70, (i % 2 == 0 ? -1 : 1) * (6 + i * 2), 0.55f);
                var flt = t.gameObject.AddComponent<FloatingLetterTile>();
                var so = new SerializedObject(flt);
                so.FindProperty("phaseOffset").floatValue = i * 0.37f;
                so.FindProperty("bobHeight").floatValue = 9f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // ── Title: ORASYON in letter tiles ───────────────────────────────
            var titleGroup = Node("TitleGroup", canvas);
            Anchor(titleGroup, Center, new Vector2(0, 250), new Vector2(1100, 300));
            var titleGlow = Img("TitleGlow", titleGroup, new Color(1f, 0.78f, 0.35f, 0.35f), softGlow);
            Anchor(titleGlow.rectTransform, Center, new Vector2(0, 20), new Vector2(1300, 380));
            titleGlow.gameObject.AddComponent<GlowPulse>();

            string title = "ORASYON";
            const float tileSize = 128f, gap = 14f;
            float startX = -(title.Length - 1) * (tileSize + gap) * 0.5f;
            for (int i = 0; i < title.Length; i++)
            {
                var t = LetterTile(titleGroup, title[i], new Vector2(startX + i * (tileSize + gap), 40), tileSize, (i % 2 == 0 ? -3f : 3f), 1f);
                var flt = t.gameObject.AddComponent<FloatingLetterTile>();
                var so = new SerializedObject(flt);
                so.FindProperty("phaseOffset").floatValue = i * 0.12f;
                so.FindProperty("bobHeight").floatValue = 6f;
                so.FindProperty("swayDegrees").floatValue = 2f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var lineL = Img("LineLeft", titleGroup, Gold);
            Anchor(lineL.rectTransform, Center, new Vector2(-470, -78), new Vector2(120, 3));
            var lineR = Img("LineRight", titleGroup, Gold);
            Anchor(lineR.rectTransform, Center, new Vector2(470, -78), new Vector2(120, 3));
            var subtitle = Text("Subtitle", titleGroup, "SPIRITS OF THE ARCHIPELAGO", 34, Cream, FontStyles.Bold);
            Anchor(subtitle.rectTransform, Center, new Vector2(0, -78), new Vector2(800, 50));
            subtitle.textWrappingMode = TextWrappingModes.NoWrap;
            subtitle.characterSpacing = 12;
            Outline(subtitle);

            // ── Menu buttons ─────────────────────────────────────────────────
            var menu = Node("MenuGroup", canvas);
            Anchor(menu, Center, new Vector2(0, -150), new Vector2(460, 320));
            var menuGroup = menu.gameObject.AddComponent<CanvasGroup>();
            var start = MenuButton(menu, "StartGameBtn", "START GAME", new Vector2(0, 100), true);
            var howTo = MenuButton(menu, "HowtoPlayBtn", "HOW TO PLAY", new Vector2(0, 0), false);
            var quit  = MenuButton(menu, "QuitBtn", "QUIT", new Vector2(0, -100), false);

            var footer = Text("Footer", canvas, "A word battle through Philippine folklore", 20, new Color(0.80f, 0.82f, 0.95f, 0.7f), FontStyles.Italic);
            Anchor(footer.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(900, 30));

            // ── How to Play panel ────────────────────────────────────────────
            var howRoot = Node("HowToRoot", canvas);
            Stretch(howRoot);
            var howGroup = howRoot.gameObject.AddComponent<CanvasGroup>();
            var dim = Img("Dimmer", howRoot, new Color(0, 0, 0, 0.75f));
            Stretch(dim.rectTransform);
            var box = Img("Panel", howRoot, Wood, panel);
            Anchor(box.rectTransform, Center, Vector2.zero, new Vector2(900, 600));
            AddOutline(box.gameObject, Gold, 3);
            var howTitle = Text("HowToTitle", box.transform, "HOW TO PLAY", 44, Gold, FontStyles.Bold);
            Anchor(howTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(800, 56));
            howTitle.characterSpacing = 8;
            string[] steps =
            {
                "Click <b>any letters</b>, in any order, to spell a word of 3+ letters.",
                "Only real words light up <b>Attack</b>. Longer words and rare letters hit harder.",
                "Used tiles are removed. <b>Scramble</b> shuffles the tiles you have left.",
                "Words of 5+ letters earn <b>potions</b>: red heals, green wards, blue doubles damage.",
                "Defeat each creature, then face the <b>boss</b> at the end of the chapter.",
            };
            for (int i = 0; i < steps.Length; i++)
            {
                var badge = Img("Step" + (i + 1), box.transform, Parchment, rounded);
                Anchor(badge.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(60, -110 - i * 76), new Vector2(54, 54));
                var num = Text("Num", badge.transform, (i + 1).ToString(), 30, Ink, FontStyles.Bold);
                Stretch(num.rectTransform);
                var line = Text("Text", box.transform, steps[i], 23, Cream, FontStyles.Normal);
                line.alignment = TextAlignmentOptions.MidlineLeft;
                Anchor(line.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(136, -110 - i * 76), new Vector2(720, 56));
            }
            var close = MenuButton(box.transform, "CloseButton", "BACK", Vector2.zero, true);
            var closeRt = (RectTransform)close.transform;
            Anchor(closeRt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 28), new Vector2(300, 70));

            // ── Fade overlay + controller ────────────────────────────────────
            var fade = Img("FadeOverlay", canvas, Color.black);
            Stretch(fade.rectTransform);
            var fadeGroup = fade.gameObject.AddComponent<CanvasGroup>();

            var ctrlGo = Node("MainMenuController", canvas);
            var ctrl = ctrlGo.gameObject.AddComponent<MainMenuController>();
            var c = new SerializedObject(ctrl);
            c.FindProperty("firstSceneName").stringValue = "Story";   // the prologue plays first, then Level1
            c.FindProperty("titleGroup").objectReferenceValue = titleGroup;
            c.FindProperty("menuGroup").objectReferenceValue = menuGroup;
            c.FindProperty("fadeGroup").objectReferenceValue = fadeGroup;
            c.FindProperty("howToRoot").objectReferenceValue = howRoot.gameObject;
            c.FindProperty("howToGroup").objectReferenceValue = howGroup;
            c.FindProperty("firstSelected").objectReferenceValue = start.gameObject;
            c.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(start.onClick, ctrl.StartGame);
            UnityEventTools.AddPersistentListener(howTo.onClick, ctrl.ShowHowTo);
            UnityEventTools.AddPersistentListener(quit.onClick, ctrl.QuitGame);
            UnityEventTools.AddPersistentListener(close.onClick, ctrl.HideHowTo);

            // Draw order: menu above decorations, how-to and fade on top
            howRoot.SetAsLastSibling();
            fade.transform.SetAsLastSibling();
            ctrlGo.SetAsLastSibling();
            howRoot.gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Orasyon] Main menu rebuilt.");
        }

        // ── Pieces ───────────────────────────────────────────────────────────
        private static void BuildParallax(RectTransform parent)
        {
            var root = Node("Parallax", parent);
            Anchor(root, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(1920, 1080));
            var parallax = root.gameObject.AddComponent<ParallaxBackground>();

            var paths = Directory.GetFiles(BackgroundPack, "*.png")
                .Select(p => p.Replace('\\', '/'))
                .Where(p => int.TryParse(Path.GetFileNameWithoutExtension(p), out _))
                .OrderBy(p => int.Parse(Path.GetFileNameWithoutExtension(p)))
                .ToArray();

            var so = new SerializedObject(parallax);
            so.FindProperty("autoScrollSpeed").floatValue = 22f;
            var layers = so.FindProperty("layers");
            layers.arraySize = paths.Length;
            for (int i = 0; i < paths.Length; i++)
            {
                var tex = Load<Texture2D>(paths[i]);
                var layer = Node("Layer_" + (i + 1), root);
                Stretch(layer);
                RectTransform Copy(string n, float x)
                {
                    var rt = Node(n, layer);
                    Anchor(rt, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(x, 0), new Vector2(1920, 1080));
                    var raw = rt.gameObject.AddComponent<RawImage>();
                    raw.texture = tex;
                    raw.raycastTarget = false;
                    return rt;
                }
                var e = layers.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("first").objectReferenceValue = Copy("A", 0);
                e.FindPropertyRelative("second").objectReferenceValue = Copy("B", 1920);
                e.FindPropertyRelative("speed").floatValue = Mathf.Lerp(0.1f, 1f, i / (float)Mathf.Max(1, paths.Length - 1));
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Character(string name, Transform parent, CharacterDefinition def, Vector2 pos, float size, Color tint)
        {
            var rt = Node(name, parent);
            Anchor(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0), pos, new Vector2(size, size));
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = def.idle.Length > 0 ? def.idle[0] : null;
            raw.color = tint;
            raw.raycastTarget = false;
            var anim = rt.gameObject.AddComponent<UIFrameAnimator>();
            var so = new SerializedObject(anim);
            so.FindProperty("playIdleOnStart").objectReferenceValue = def;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform LetterTile(Transform parent, char letter, Vector2 pos, float size, float angle, float alpha)
        {
            var tile = Img("Tile_" + letter, parent, new Color(Parchment.r, Parchment.g, Parchment.b, alpha), rounded);
            Anchor(tile.rectTransform, Center, pos, new Vector2(size, size));
            tile.rectTransform.localEulerAngles = new Vector3(0, 0, angle);
            tile.raycastTarget = false;
            var shadow = tile.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f * alpha);
            shadow.effectDistance = new Vector2(0, -size * 0.05f);
            AddOutline(tile.gameObject, new Color(Gold.r * 0.7f, Gold.g * 0.6f, Gold.b * 0.4f, alpha), 2);
            var l = Text("Letter", tile.transform, letter.ToString(), size * 0.62f, new Color(Ink.r, Ink.g, Ink.b, alpha), FontStyles.Bold);
            Stretch(l.rectTransform);
            return tile.rectTransform;
        }

        private static Button MenuButton(Transform parent, string name, string label, Vector2 pos, bool primary)
        {
            var root = Node(name, parent);
            Anchor(root, Center, pos, new Vector2(440, 80));

            var glow = Img("Glow", root, new Color(1f, 0.80f, 0.40f, 0f), softGlow);
            Anchor(glow.rectTransform, Center, Vector2.zero, new Vector2(560, 170));

            var face = Img("Face", root, primary ? new Color(0.78f, 0.56f, 0.20f, 1f) : WoodLight, rounded);
            Stretch(face.rectTransform);
            face.raycastTarget = true;   // the clickable area
            AddOutline(face.gameObject, primary ? new Color(1f, 0.88f, 0.55f, 1f) : Gold, 2);
            var shadow = face.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.55f);
            shadow.effectDistance = new Vector2(0, -5);

            var text = Text("Label", root, label, 32, primary ? Ink : Cream, FontStyles.Bold);
            Stretch(text.rectTransform);
            text.characterSpacing = 6;

            var btn = root.gameObject.AddComponent<Button>();
            btn.targetGraphic = face;
            btn.transition = Selectable.Transition.None;   // MenuButtonFX handles hover / press

            var fx = root.gameObject.AddComponent<MenuButtonFX>();
            var so = new SerializedObject(fx);
            so.FindProperty("background").objectReferenceValue = face;
            so.FindProperty("label").objectReferenceValue = text;
            so.FindProperty("glow").objectReferenceValue = glow;
            so.FindProperty("hoverTint").colorValue = primary ? new Color(0.12f, 0.06f, 0.02f, 1f) : new Color(1f, 0.92f, 0.65f, 1f);
            so.FindProperty("hoverGlowAlpha").floatValue = 0.6f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return btn;
        }

        // ── Generated sprites ────────────────────────────────────────────────
        private static Sprite MakeRadialSprite(string name, int size, System.Func<float, float> alphaOfDistance)
        {
            Directory.CreateDirectory(UiFolder);
            string path = $"{UiFolder}/{name}.png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alphaOfDistance(Mathf.Sqrt(dx * dx + dy * dy))));
                    }
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.alphaIsTransparency = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
        }

        private static void SetPointFilter(string folder)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                if (ti.filterMode == FilterMode.Point) continue;
                ti.filterMode = FilterMode.Point;
                ti.SaveAndReimport();
            }
        }

        // ── Generic helpers ──────────────────────────────────────────────────
        private static readonly Vector2[] Center = { new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f) };

        private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        private static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Anchor(RectTransform rt, Vector2[] anchors, Vector2 pos, Vector2 size)
            => Anchor(rt, anchors[0], anchors[1], anchors[2], pos, size);

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

        private static Image Img(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var img = Node(name, parent).gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            }
            return img;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color, FontStyles style)
        {
            var t = Node(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return t;
        }

        private static void Outline(TextMeshProUGUI t)
        {
            t.outlineWidth = 0.22f;
            t.outlineColor = new Color32(15, 10, 25, 255);
        }

        private static void AddOutline(GameObject go, Color color, float width)
        {
            var o = go.AddComponent<UnityEngine.UI.Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(width, -width);
        }
    }
}
