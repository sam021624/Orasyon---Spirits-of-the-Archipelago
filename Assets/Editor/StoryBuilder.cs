using System.IO;
using System.Linq;
using Orasyon.Battle;
using Orasyon.Story;
using Orasyon.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Orasyon.EditorTools
{
    /// <summary>
    /// Creates the Story scene (dialogue player) and the story assets:
    /// Prologue (MainMenu → Story → Level1) and the Chapter 1 interlude (Level1 → Story → Level2).
    /// Re-running rebuilds the scene and overwrites the dialogue lines in the assets.
    /// </summary>
    public static class StoryBuilder
    {
        private const string ScenePath   = "Assets/Levels/Story.unity";
        private const string StoryFolder = "Assets/Data/Story";
        private const string Terrains    = "Assets/Sprites/Terrains/";

        private static readonly Color Wood  = new Color(0.14f, 0.09f, 0.05f, 0.96f);
        private static readonly Color Gold  = new Color(0.90f, 0.70f, 0.32f, 1f);
        private static readonly Color Cream = new Color(0.97f, 0.91f, 0.76f, 1f);

        [MenuItem("Tools/Orasyon/Rebuild Story Scene + Dialogue")]
        public static void Build()
        {
            foreach (var pack in new[] { "1", "2", "6" }) PointFilter(Terrains + pack + "/2304x1296.png");
            var prologue  = BuildPrologue();
            var interlude = BuildInterlude();
            BuildScene(prologue);
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[Orasyon] Story scene and dialogue rebuilt.");
        }

        /// <summary>Rebuilds only the Story scene (Lola, dialogue box...). Dialogue assets are left exactly as they are.</summary>
        [MenuItem("Tools/Orasyon/Rebuild Story Scene Only (keep dialogue)")]
        public static void BuildSceneOnly()
        {
            var prologue = AssetDatabase.LoadAssetAtPath<StoryScript>(StoryFolder + "/Prologue.asset");
            if (prologue == null) { Build(); return; }
            BuildScene(prologue);
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[Orasyon] Story scene rebuilt (dialogue untouched).");
        }

        // ── Dialogue ─────────────────────────────────────────────────────────
        private static StoryScript BuildPrologue()
        {
            var s = Asset("Prologue");
            s.title = "Prologue";
            s.subtitle = "The Book of Orasyon";
            s.background = Bg("2");   // the village at dusk
            s.nextScene = "Level1";
            s.lines = new[]
            {
                Line(Speaker.Narrator,  "At the edge of a quiet village, the old albularya Lola Isidra keeps a house full of herbs, candles and very old books."),
                Line(Speaker.Lola,      "There you are, apo. Come, sit with me. The sun is setting, and the spirits are restless tonight."),
                Line(Speaker.Albularyo, "Lola, the neighbors keep saying strange things. Lights in the balete tree, voices from the mounds in the backyard..."),
                Line(Speaker.Lola,      "They are not imagining it. Something has upset the balance between our world and theirs."),
                Line(Speaker.Lola,      "This is the Book of Orasyon. It has passed from albularyo to albularyo for generations. Tonight, it passes to you.", pose: LolaPose.Give),
                Line(Speaker.Albularyo, "To me? But Lola, I barely remember the old prayers."),
                Line(Speaker.Lola,      "You know words, apo. That is all the book needs. Speak them with intent, and they become power."),
                Line(Speaker.Albularyo, "The pages... they're glowing! Lola, what's happening?"),
                Line(Speaker.Lola,      "The book has chosen you. Go where it takes you, and remember your manners. Tabi tabi po."),
                Line(Speaker.Narrator,  "A rush of light and wind. When it fades, the house is gone.", flash: true, newBg: Bg("1"), lolaOnStage: false),
                Line(Speaker.Albularyo, "Where am I? It looks like our backyard... but bigger. Brighter. Older.", lolaOnStage: false),
                Line(Speaker.Lola,      "You are inside the stories now, apo. I will speak to you through the pages.", lolaOnStage: false),
                Line(Speaker.Lola,      "Start with the backyard mound. The Duwende King has grown bold, and his servants are everywhere. Be brave.", lolaOnStage: false),
            };
            EditorUtility.SetDirty(s);
            return s;
        }

        private static StoryScript BuildInterlude()
        {
            var s = Asset("Interlude_Chapter1");
            s.title = "Chapter 1 Complete";
            s.subtitle = "Voices in the Pages";
            s.background = Bg("1");   // the backyard grove, now quiet
            s.nextScene = "Level2";
            s.lines = new[]
            {
                Line(Speaker.Narrator,  "The mound is quiet. Somewhere far away, the pages of the Book of Orasyon begin to rustle.", lolaOnStage: false),
                Line(Speaker.Albularyo, "Lola? Lola, can you hear me? The Duwende King is gone. He went back into his mound!", lolaOnStage: false),
                Line(Speaker.Lola,      "I heard every word, apo. You were strong, and you were respectful. That is how an albularyo wins.", lolaOnStage: false),
                Line(Speaker.Albularyo, "So it's over? I can come home now?", lolaOnStage: false),
                Line(Speaker.Lola,      "Not yet. The duwende were only the first to stir. Listen... do you hear the leaves?", lolaOnStage: false),
                Line(Speaker.Albularyo, "Something big is moving in the trees. And I smell... smoke? Like a cigar?", lolaOnStage: false),
                Line(Speaker.Lola,      "The Kapre. He sits in the oldest balete, and his smoke can hide even the clearest path.", lolaOnStage: false),
                Line(Speaker.Lola,      "Speak clearly, apo. Your words must cut through the smoke.", lolaOnStage: false),
                Line(Speaker.Albularyo, "Then I'll find him. Book of Orasyon, take me to the old balete!", lolaOnStage: false),
                Line(Speaker.Narrator,  "The pages turn on their own, and the world turns with them.", flash: true, newBg: Bg("6"), lolaOnStage: false),
            };
            EditorUtility.SetDirty(s);
            return s;
        }

        private static StoryLine Line(Speaker who, string text, bool flash = false, Texture2D newBg = null, bool lolaOnStage = true, LolaPose pose = LolaPose.Auto)
            => new StoryLine { speaker = who, text = text, flash = flash, changeBackground = newBg, lolaOnStage = lolaOnStage, lolaPose = pose };

        private static StoryScript Asset(string name)
        {
            Directory.CreateDirectory(StoryFolder);
            string path = $"{StoryFolder}/{name}.asset";
            var s = AssetDatabase.LoadAssetAtPath<StoryScript>(path);
            if (s == null)
            {
                s = ScriptableObject.CreateInstance<StoryScript>();
                AssetDatabase.CreateAsset(s, path);
            }
            return s;
        }

        private static Texture2D Bg(string pack) => AssetDatabase.LoadAssetAtPath<Texture2D>(Terrains + pack + "/2304x1296.png");

        // ── Scene ────────────────────────────────────────────────────────────
        private static void BuildScene(StoryScript defaultScript)
        {
            var scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);

            var cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("StoryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var canvas = canvasGo.transform;

            var softGlow = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/soft_glow.png").OfType<Sprite>().FirstOrDefault();
            var vignette = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/vignette.png").OfType<Sprite>().FirstOrDefault();
            var rounded  = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var panel    = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

            // Background
            var bgRt = Node("Background", canvas); Stretch(bgRt);
            var bg = bgRt.gameObject.AddComponent<RawImage>();
            bg.texture = defaultScript.background;
            bg.raycastTarget = false;
            var vig = Img("Vignette", canvas, new Color(0, 0, 0, 0.8f), vignette); Stretch(vig.rectTransform);

            // Albularyo (left)
            var albuDef = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Data/Characters/Albularyo.asset");
            var albuRt = Node("Albularyo", canvas);
            Anchor(albuRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0), new Vector2(-560, -300), new Vector2(460, 460));
            var albuImg = albuRt.gameObject.AddComponent<RawImage>();
            albuDef.idle.Apply(albuImg, 0);
            albuImg.raycastTarget = false;
            var albuAnim = albuRt.gameObject.AddComponent<UIFrameAnimator>();

            // Lola (right): animated from Characters-New/Prologue/lola. The spirit-light below is only a fallback.
            var lolaRt = Node("Lola", canvas);
            Anchor(lolaRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0), new Vector2(560, -300), new Vector2(460, 460));
            var lolaGroup = lolaRt.gameObject.AddComponent<CanvasGroup>();
            var lolaImage = lolaRt.gameObject.AddComponent<RawImage>();
            lolaImage.raycastTarget = false;
            var lolaIdleTex = AssetDatabase.LoadAssetAtPath<Texture2D>(LolaFolder + "lola_idle.png");
            if (lolaIdleTex != null) new FrameClip(lolaIdleTex, Mathf.RoundToInt(lolaIdleTex.width / (float)lolaIdleTex.height)).Apply(lolaImage, 0);
            else lolaImage.enabled = false;
            var lolaAnim = lolaRt.gameObject.AddComponent<UIFrameAnimator>();
            var placeholder = Node("Placeholder_SpiritLight", lolaRt);
            Anchor(placeholder, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(420, 420));
            var outer = Img("Glow", placeholder, new Color(1f, 0.78f, 0.40f, 0.65f), softGlow); Stretch(outer.rectTransform);
            outer.gameObject.AddComponent<GlowPulse>();
            var core = Img("Core", placeholder, new Color(1f, 0.95f, 0.80f, 0.95f), softGlow);
            Anchor(core.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150));
            for (int i = 0; i < 6; i++)
            {
                var mote = Img("Mote_" + i, placeholder, new Color(1f, 0.9f, 0.6f, 0.6f), softGlow);
                float a = i * Mathf.PI / 3f;
                Anchor(mote.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(Mathf.Cos(a) * 150, Mathf.Sin(a) * 150), new Vector2(26, 26));
                mote.gameObject.AddComponent<FireflyDrift>();
            }

            // Dialogue box (bottom)
            var dialogueRt = Node("Dialogue", canvas);
            Anchor(dialogueRt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(1560, 260));
            var dialogueGroup = dialogueRt.gameObject.AddComponent<CanvasGroup>();
            dialogueGroup.blocksRaycasts = false;
            var box = Img("Box", dialogueRt, Wood, panel); Stretch(box.rectTransform);
            Outline(box.gameObject, Gold, 3);
            var body = Text("Body", dialogueRt, "", 32, Cream, TextAlignmentOptions.TopLeft);
            Stretch(body.rectTransform);
            body.rectTransform.offsetMin = new Vector2(60, 50);
            body.rectTransform.offsetMax = new Vector2(-60, -54);

            var plate = Img("NamePlate", dialogueRt, new Color(0.22f, 0.13f, 0.07f, 1f), rounded);
            Anchor(plate.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(50, 0), new Vector2(420, 60));
            Outline(plate.gameObject, Gold, 2);
            var name = Text("Name", plate.transform, "Albularyo", 30, Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(name.rectTransform);

            var hint = Text("ContinueHint", dialogueRt, "click to continue", 20, Cream, TextAlignmentOptions.BottomRight, FontStyles.Italic);
            Anchor(hint.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-36, 18), new Vector2(400, 30));

            // Click-anywhere area (under the Skip button)
            var advanceImg = Img("AdvanceArea", canvas, new Color(0, 0, 0, 0)); Stretch(advanceImg.rectTransform);
            advanceImg.raycastTarget = true;
            var advance = advanceImg.gameObject.AddComponent<Button>();
            advance.transition = Selectable.Transition.None;

            // Skip
            var skipImg = Img("SkipButton", canvas, new Color(0.22f, 0.13f, 0.07f, 0.9f), rounded);
            Anchor(skipImg.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -30), new Vector2(170, 56));
            skipImg.raycastTarget = true;
            Outline(skipImg.gameObject, Gold, 2);
            var skip = skipImg.gameObject.AddComponent<Button>();
            var skipLabel = Text("Label", skipImg.transform, "SKIP  >>", 24, Cream, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(skipLabel.rectTransform);

            // Title card
            var titleRt = Node("TitleCard", canvas); Stretch(titleRt);
            var titleGroup = titleRt.gameObject.AddComponent<CanvasGroup>();
            titleGroup.blocksRaycasts = false;
            var titleDim = Img("Dim", titleRt, new Color(0, 0, 0, 0.6f)); Stretch(titleDim.rectTransform);
            var title = Text("Title", titleRt, "Prologue", 84, Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(1400, 110));
            title.characterSpacing = 10;
            title.outlineWidth = 0.2f; title.outlineColor = new Color32(20, 10, 5, 255);
            var subtitle = Text("Subtitle", titleRt, "", 38, Cream, TextAlignmentOptions.Center, FontStyles.Italic);
            Anchor(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(1400, 60));

            // Flash + fade
            var flash = Img("Flash", canvas, new Color(1f, 0.97f, 0.88f, 1f)); Stretch(flash.rectTransform);
            var flashGroup = flash.gameObject.AddComponent<CanvasGroup>();
            flashGroup.alpha = 0f; flashGroup.blocksRaycasts = false;
            var fade = Img("Fade", canvas, Color.black); Stretch(fade.rectTransform);
            var fadeGroup = fade.gameObject.AddComponent<CanvasGroup>();
            fadeGroup.blocksRaycasts = false;

            // Player component
            var player = Node("StoryPlayer", canvas).gameObject.AddComponent<StoryPlayer>();
            var so = new SerializedObject(player);
            void Set(string p, Object v) => so.FindProperty(p).objectReferenceValue = v;
            Set("defaultScript", defaultScript);
            Set("background", bg);
            Set("albularyo", albuAnim);
            Set("albularyoDefinition", albuDef);
            Set("lolaGroup", lolaGroup);
            Set("lolaAnimator", lolaAnim);
            Set("lolaPlaceholder", placeholder.gameObject);
            SetClip(so, "lolaIdle",  "lola_idle");
            SetClip(so, "lolaTalk",  "lola_talk");
            SetClip(so, "lolaLaugh", "lola_laugh");
            SetClip(so, "lolaGive",  "lola_give");
            SetClip(so, "lolaHeal",  "lola_heal");
            Set("dialogueGroup", dialogueGroup);
            Set("namePlate", plate.gameObject);
            Set("nameLabel", name);
            Set("bodyText", body);
            Set("continueHint", hint);
            Set("advanceButton", advance);
            Set("skipButton", skip);
            Set("titleGroup", titleGroup);
            Set("titleText", title);
            Set("subtitleText", subtitle);
            Set("flashGroup", flashGroup);
            Set("fadeGroup", fadeGroup);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private const string LolaFolder = "Assets/Sprites/Characters-New/Prologue/lola/";

        /// <summary>Fills a FrameClip field of StoryPlayer from one of Lola's strips (square cells).</summary>
        private static void SetClip(SerializedObject so, string field, string clipName)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(LolaFolder + clipName + ".png");
            var p = so.FindProperty(field);
            if (p == null || tex == null) { Debug.LogWarning($"[Orasyon] Lola clip '{clipName}' not set."); return; }
            p.FindPropertyRelative("sheet").objectReferenceValue = tex;
            p.FindPropertyRelative("frames").intValue = Mathf.RoundToInt(tex.width / (float)tex.height);
        }

        private static void UpdateBuildSettings()
        {
            string[] order = { "Assets/Levels/MainMenu.unity", ScenePath, "Assets/Levels/Level1.unity", "Assets/Levels/Level2.unity" };
            var list = order.Select(p => new EditorBuildSettingsScene(p, true)).ToList();
            foreach (var s in EditorBuildSettings.scenes)
                if (!order.Contains(s.path)) list.Add(s);
            EditorBuildSettings.scenes = list.ToArray();
        }

        private static void PointFilter(string path)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null || ti.filterMode == FilterMode.Point) return;
            ti.filterMode = FilterMode.Point;
            ti.SaveAndReimport();
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
            => Anchor(rt, anchor, anchor, pivot, pos, size);

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

        private static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color,
                                            TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
        {
            var t = Node(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align; t.fontStyle = style;
            t.raycastTarget = false;
            return t;
        }

        private static void Outline(GameObject go, Color color, float width)
        {
            var o = go.AddComponent<UnityEngine.UI.Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(width, -width);
        }
    }
}
