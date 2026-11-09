using System.IO;
using System.Linq;
using Orasyon.Battle;
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
    /// Builds the Bookworm-style battle layout in Level1:
    /// top bar (names, hearts, chapter) · battle stage (parallax background, characters, word preview)
    /// · bottom: buffs (left), letter grid + Attack (center), enemy info (right).
    /// Re-running replaces the BattleCanvas object (manual edits inside it are lost).
    /// </summary>
    public static class Level1BattleBuilder
    {
        private const string TilePrefabPath = "Assets/Resources/TilePrefab.prefab";
        private const string DataFolder     = "Assets/Data/Characters/";
        private const string CharFolder     = "Assets/Sprites/Characters/";
        private const string StoryFolder    = "Assets/Data/Story/";
        private const string TreasureFolder = "Assets/Resources/Treasures/";
        private const string HeartSprite    = "Assets/Sprites/Icons/Hearts/PNG/basic/heart.png";

        /// <summary>Everything that differs between chapters.</summary>
        private sealed class ChapterSpec
        {
            public string   scenePath;
            public string   chapterTitle;
            public string   backgroundPack;
            public string[] enemyAssets;      // fought in order, last = boss
            public bool     tutorial;
            public string   firstBoard;       // fixed first board (tutorial) or "" for random
            public string   nextStoryAsset;   // dialogue after the boss, or null
            public string   victoryMessage;
            public string   rewardAsset;      // treasure the boss drops (Resources/Treasures)
        }

        private static readonly ChapterSpec Chapter1 = new ChapterSpec
        {
            scenePath      = "Assets/Levels/Level1.unity",
            chapterTitle   = "Chapter 1: The Backyard Mound",
            backgroundPack = "Assets/Sprites/Terrains/1",
            enemyAssets    = new[] { "01_ForestBat", "02_BaleteTreeMinion", "03_DuwendeKing" },
            tutorial       = true,
            firstBoard     = "WORDATESLINGBUKA",
            nextStoryAsset = "Interlude_Chapter1",
            rewardAsset    = "KalasagNgPunso",
            victoryMessage = "The Duwende King retreats into his mound.\nThe backyard is quiet again.",
        };

        private static readonly ChapterSpec Chapter2 = new ChapterSpec
        {
            scenePath      = "Assets/Levels/Level2.unity",
            chapterTitle   = "Chapter 2: The Old Balete",
            backgroundPack = "Assets/Sprites/Terrains/6",
            enemyAssets    = new[] { "04_EnchantedMushroom", "05_DuwendeMinion", "06_Kapre" },
            tutorial       = false,
            firstBoard     = "",
            nextStoryAsset = null,
            rewardAsset    = "PanaNgBalete",
            victoryMessage = "The Kapre's smoke drifts away from the old balete.\nHe nods, impressed, and lets you pass.",
        };

        private static readonly Color Wood      = new Color(0.16f, 0.10f, 0.06f, 1f);
        private static readonly Color WoodLight = new Color(0.25f, 0.15f, 0.09f, 1f);
        private static readonly Color WoodDark  = new Color(0.10f, 0.06f, 0.04f, 0.95f);
        private static readonly Color Gold      = new Color(0.88f, 0.68f, 0.30f, 1f);
        private static readonly Color Cream     = new Color(0.97f, 0.91f, 0.76f, 1f);
        private static readonly Color Parchment = new Color(0.96f, 0.89f, 0.70f, 1f);
        private static readonly Color Ink       = new Color(0.28f, 0.16f, 0.07f, 1f);

        private static Sprite roundedSprite, panelSprite, circleSprite;

        [MenuItem("Tools/Orasyon/Rebuild Level 1 Battle UI")]
        public static void Build() => BuildChapter(Chapter1);

        [MenuItem("Tools/Orasyon/Rebuild Level 2 Battle UI")]
        public static void BuildLevel2() => BuildChapter(Chapter2);

        private static void BuildChapter(ChapterSpec spec)
        {
            roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            panelSprite   = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            circleSprite  = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            UpdateCharacterData();
            TreasureAssets.Ensure();
            SetPointFilter(spec.backgroundPack);
            var tilePrefab = BuildTilePrefab();

            var scene = EditorSceneManager.OpenScene(spec.scenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "BattleCanvas") Object.DestroyImmediate(root);
            EnsureEventSystem(scene);

            // ── Canvas ───────────────────────────────────────────────────────
            var canvasGo = new GameObject("BattleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var canvas = canvasGo.transform;

            var albularyo = Load<CharacterDefinition>(DataFolder + "Albularyo.asset");
            var foes = spec.enemyAssets.Select(n => Load<CharacterDefinition>(DataFolder + n + ".asset")).ToArray();
            var bat = foes[0];   // first foe: shown in the scene before play starts

            // ── Stage (top 560px) ────────────────────────────────────────────
            var stage = Node("Stage", canvas);
            Anchor(stage, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 560));
            stage.gameObject.AddComponent<Image>().color = new Color(0.09f, 0.13f, 0.10f);
            stage.gameObject.AddComponent<RectMask2D>();

            var parallax = BuildBackground(stage, spec.backgroundPack);

            var characters = Node("Characters", stage);
            Stretch(characters);

            var playerAnim = Character("Player", characters, albularyo, -560f);
            var enemyAnim  = Character("Enemy",  characters, bat,       560f);

            var popup = Text("DamagePopupTemplate", characters, "-10", 46, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            Anchor(popup.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(320, 64));
            var dropIcon = Panel("TreasureDropTemplate", characters, Color.white);
            dropIcon.type = Image.Type.Simple; dropIcon.preserveAspect = true; dropIcon.raycastTarget = false;
            Anchor(dropIcon.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150));
            dropIcon.gameObject.SetActive(false);   // template: copies are made when a boss drops its treasure
            popup.outlineWidth = 0.2f;
            popup.outlineColor = new Color32(40, 20, 10, 255);
            popup.gameObject.SetActive(false);   // template: copies are made for each popup

            // Word preview (current word as tiles), hint, feedback
            var previewRow = Node("WordPreview", stage);
            Anchor(previewRow, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(1000, 80));
            var hlg = previewRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = hlg.childControlHeight = false;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;
            var letterTemplate = Panel("LetterTemplate", previewRow, Parchment, roundedSprite);
            letterTemplate.rectTransform.sizeDelta = new Vector2(72, 72);
            AddShadow(letterTemplate.gameObject);
            var templateLetter = Text("Letter", letterTemplate.transform, "A", 44, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(templateLetter.rectTransform);
            letterTemplate.gameObject.SetActive(false);   // template for the word preview tiles

            var hint = Text("WordHint", stage, "", 22, Cream, TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.Italic);
            Anchor(hint.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -184), new Vector2(700, 32));
            hint.outlineWidth = 0.25f;
            hint.outlineColor = new Color32(20, 10, 5, 255);
            var feedback = Text("Feedback", stage, "", 28, Cream, TextAlignmentOptions.Top, FontStyles.Bold);
            Anchor(feedback.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -218), new Vector2(900, 96));
            feedback.outlineWidth = 0.2f;
            feedback.outlineColor = new Color32(20, 10, 5, 255);

            var status = Text("PlayerStatus", stage, "", 20, Cream, TextAlignmentOptions.Left, FontStyles.Bold);
            Anchor(status.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -94), new Vector2(700, 30));
            var enemyStatus = Text("EnemyStatus", stage, "", 20, Cream, TextAlignmentOptions.Right, FontStyles.Bold);
            Anchor(enemyStatus.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -94), new Vector2(600, 30));
            enemyStatus.outlineWidth = 0.25f;
            enemyStatus.outlineColor = new Color32(20, 10, 5, 255);
            status.outlineWidth = 0.25f;
            status.outlineColor = new Color32(20, 10, 5, 255);

            // ── Top bar ──────────────────────────────────────────────────────
            var topBar = Panel("TopBar", canvas, new Color(0.06f, 0.04f, 0.03f, 0.80f));
            Anchor(topBar.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 88));

            var playerName = Text("PlayerName", topBar.transform, "Albularyo", 28, new Color(0.60f, 0.93f, 0.62f), TextAlignmentOptions.Left, FontStyles.Bold);
            Anchor(playerName.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -6), new Vector2(560, 40));
            var playerHearts = Hearts("PlayerHearts", topBar.transform, mirrored: false);
            Anchor((RectTransform)playerHearts.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -48), new Vector2(440, 32));

            var enemyName = Text("EnemyName", topBar.transform, bat.displayName, 28, Gold, TextAlignmentOptions.Right, FontStyles.Bold);
            Anchor(enemyName.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -6), new Vector2(560, 40));
            var enemyHearts = Hearts("EnemyHearts", topBar.transform, mirrored: true);
            Anchor((RectTransform)enemyHearts.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -48), new Vector2(440, 32));

            var plaque = Panel("ChapterPlaque", topBar.transform, Wood, panelSprite);
            Anchor(plaque.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(640, 76));
            AddOutline(plaque.gameObject, Gold, 2);
            var chapter = Text("ChapterTitle", plaque.transform, spec.chapterTitle, 28, Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            Anchor(chapter.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(620, 38));
            var encounter = Text("Encounter", plaque.transform, $"Encounter 1 of {foes.Length}", 18, Cream, TextAlignmentOptions.Center);
            Anchor(encounter.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -44), new Vector2(620, 26));

            // ── Bottom (520px) ───────────────────────────────────────────────
            var bottom = Panel("Bottom", canvas, Wood);
            Anchor(bottom.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(0, 520));
            var bottomGroup = bottom.gameObject.AddComponent<CanvasGroup>();
            var trim = Panel("TopTrim", bottom.transform, Gold);
            Anchor(trim.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 6));

            // Left: potion shelf (top) + item slots reserved for boss drops (bottom, empty for now)
            var left = Panel("InventoryPanel", bottom.transform, WoodLight, panelSprite);
            Anchor(left.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(16, 100), new Vector2(580, 404));
            AddOutline(left.gameObject, Gold, 2);
            var buffInventory = left.gameObject.AddComponent<BuffInventory>();
            var cards = new[]
            {
                PotionSlot(left.transform, 0, "Healing", "+1 heart",          "5-letter word", PotionSprite("potion_red")),
                PotionSlot(left.transform, 1, "Warding", "Cures + blocks a hit", "6-letter word", PotionSprite("potion_green")),
                PotionSlot(left.transform, 2, "Power",   "Next word x2",       "7+ letters",    PotionSprite("potion_blue")),
            };
            var shelf = Panel("Shelf", left.transform, new Color(0.36f, 0.22f, 0.12f, 1f));
            Anchor(shelf.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -196), new Vector2(-24, 12));
            AddOutline(shelf.gameObject, new Color(0.10f, 0.06f, 0.03f, 1f), 2);
            // Treasure shelf: 5 slots, one per final boss. Empty until a boss drops its treasure.
            var itemArea = Node("TreasureSlots", left.transform);
            Anchor(itemArea, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(-24, 180));
            var treasureShelf = itemArea.gameObject.AddComponent<TreasureShelf>();
            var tso = new SerializedObject(treasureShelf);
            var tslots = tso.FindProperty("slots");
            tslots.arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                var slotBg = Panel("Slot_" + (i + 1), itemArea, new Color(0.10f, 0.06f, 0.04f, 0.85f), panelSprite);
                Anchor(slotBg.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(4 + i * 111, 0), new Vector2(104, 170));
                AddOutline(slotBg.gameObject, new Color(Gold.r, Gold.g, Gold.b, 0.35f), 1);
                var slotBtn = slotBg.gameObject.AddComponent<Button>();
                slotBtn.targetGraphic = slotBg;
                var slotColors = slotBtn.colors;
                slotColors.disabledColor = Color.white;   // empty slots keep their look
                slotBtn.colors = slotColors;
                var slotIcon = Panel("Icon", slotBg.transform, Color.white);
                slotIcon.type = Image.Type.Simple; slotIcon.preserveAspect = true; slotIcon.raycastTarget = false;
                slotIcon.enabled = false;   // shown once a treasure is owned
                Anchor(slotIcon.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(84, 84));
                var slotLabel = Text("Name", slotBg.transform, "", 15, Gold, TextAlignmentOptions.Top, FontStyles.Bold);
                Anchor(slotLabel.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(98, 62));
                var e = tslots.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("icon").objectReferenceValue = slotIcon;
                e.FindPropertyRelative("label").objectReferenceValue = slotLabel;
                e.FindPropertyRelative("button").objectReferenceValue = slotBtn;
            }
            tso.ApplyModifiedPropertiesWithoutUndo();
            var scramble = MakeButton("ScrambleButton", bottom.transform, "SCRAMBLE", new Color(0.66f, 0.32f, 0.12f), Cream, 28);
            Anchor((RectTransform)scramble.transform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(16, 20), new Vector2(580, 64));

            // Center: grid + attack/clear
            var gridFrame = Panel("GridFrame", bottom.transform, WoodDark, panelSprite);
            Anchor(gridFrame.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 100), new Vector2(440, 404));
            AddOutline(gridFrame.gameObject, Gold, 2);
            var gridRt = Node("LetterGrid", gridFrame.transform);
            Anchor(gridRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 400));
            gridRt.gameObject.AddComponent<RectMask2D>();
            var gridGroup = gridRt.gameObject.AddComponent<CanvasGroup>();
            var grid = gridRt.gameObject.AddComponent<LetterGrid>();
            var gso = new SerializedObject(grid);
            gso.FindProperty("tilePrefab").objectReferenceValue = tilePrefab;
            gso.FindProperty("cellSize").floatValue = 92f;
            gso.FindProperty("spacing").floatValue = 8f;
            gso.ApplyModifiedPropertiesWithoutUndo();

            var attack = MakeButton("AttackButton", bottom.transform, "ATTACK", new Color(0.32f, 0.62f, 0.28f), Cream, 34);
            Anchor((RectTransform)attack.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-5, 20), new Vector2(290, 64));
            var clear = MakeButton("ClearButton", bottom.transform, "CLEAR", new Color(0.40f, 0.24f, 0.32f), Cream, 22);
            Anchor((RectTransform)clear.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(222, 20), new Vector2(130, 64));
            var hintBtn = MakeButton("HintButton", bottom.transform, "HINT (3)", new Color(0.20f, 0.42f, 0.55f), Cream, 22);
            Anchor((RectTransform)hintBtn.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-232, 20), new Vector2(150, 64));

            // Right: enemy info
            var right = Panel("EnemyInfoPanel", bottom.transform, WoodLight, panelSprite);
            Anchor(right.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-16, 100), new Vector2(580, 404));
            AddOutline(right.gameObject, Gold, 2);
            var portraitBg = Panel("PortraitFrame", right.transform, WoodDark, panelSprite);
            Anchor(portraitBg.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -14), new Vector2(132, 132));
            var portraitGo = Node("Portrait", portraitBg.transform);
            Stretch(portraitGo, 4);
            var portrait = portraitGo.gameObject.AddComponent<RawImage>();
            portrait.raycastTarget = false;
            var infoName = Text("Name", right.transform, bat.displayName, 28, Gold, TextAlignmentOptions.Left, FontStyles.Bold);
            Anchor(infoName.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(160, -16), new Vector2(404, 40));
            var infoWeak = Text("Weakness", right.transform, "", 18, new Color(0.62f, 0.95f, 0.58f), TextAlignmentOptions.TopLeft);
            Anchor(infoWeak.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(160, -62), new Vector2(404, 80));
            var attacksBox = Panel("AttacksBox", right.transform, WoodDark, panelSprite);
            Anchor(attacksBox.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -156), new Vector2(552, 132));
            var infoAttack = Text("Attacks", attacksBox.transform, "", 17, Cream, TextAlignmentOptions.TopLeft);
            Stretch(infoAttack.rectTransform, 12);
            infoAttack.lineSpacing = 6;
            var descBox = Panel("DescriptionBox", right.transform, WoodDark, panelSprite);
            Anchor(descBox.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -296), new Vector2(552, 96));
            var infoDesc = Text("Description", descBox.transform, "", 16, Cream, TextAlignmentOptions.TopLeft, FontStyles.Italic);
            Stretch(infoDesc.rectTransform, 12);
            var info = right.gameObject.AddComponent<EnemyInfoPanel>();
            var iso = new SerializedObject(info);
            iso.FindProperty("portrait").objectReferenceValue = portrait;
            iso.FindProperty("nameLabel").objectReferenceValue = infoName;
            iso.FindProperty("attackLabel").objectReferenceValue = infoAttack;
            iso.FindProperty("weaknessLabel").objectReferenceValue = infoWeak;
            iso.FindProperty("descriptionLabel").objectReferenceValue = infoDesc;
            iso.ApplyModifiedPropertiesWithoutUndo();
            info.Show(bat);

            var menu = MakeButton("MenuButton", bottom.transform, "MENU", new Color(0.62f, 0.46f, 0.12f), Cream, 28);
            Anchor((RectTransform)menu.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-16, 20), new Vector2(580, 64));

            // ── Tutorial popup ───────────────────────────────────────────────
            var tutPanel = Panel("TutorialPanel", canvas, new Color(0.07f, 0.05f, 0.12f, 0.96f), panelSprite);
            Anchor(tutPanel.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(780, 340));
            AddOutline(tutPanel.gameObject, Gold, 2);
            var tutTitle = Text("Title", tutPanel.transform, "Tutorial", 32, Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            Anchor(tutTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(720, 44));
            var tutBody = Text("Body", tutPanel.transform, "", 22, Cream, TextAlignmentOptions.TopLeft);
            Anchor(tutBody.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(720, 190));
            var tutNext = MakeButton("NextButton", tutPanel.transform, "Got it!", Gold, Ink, 24);
            Anchor((RectTransform)tutNext.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(220, 54));

            // ── Result popup ─────────────────────────────────────────────────
            var result = Panel("ResultPanel", canvas, new Color(0, 0, 0, 0.75f));
            Stretch(result.rectTransform);
            var resultBox = Panel("Box", result.transform, Wood, panelSprite);
            Anchor(resultBox.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680, 470));
            AddOutline(resultBox.gameObject, Gold, 3);
            var resultTitle = Text("Title", resultBox.transform, "Chapter Complete!", 48, Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            Anchor(resultTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(640, 64));
            var resultBody = Text("Body", resultBox.transform, "", 24, Cream, TextAlignmentOptions.Center);
            Anchor(resultBody.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(620, 110));
            // Reward row: the boss's treasure (hidden on defeat)
            var rewardRow = Panel("Reward", resultBox.transform, WoodDark, panelSprite);
            Anchor(rewardRow.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -222), new Vector2(600, 120));
            AddOutline(rewardRow.gameObject, new Color(Gold.r, Gold.g, Gold.b, 0.6f), 1);
            var rewardIcon = Panel("Icon", rewardRow.transform, Color.white);
            rewardIcon.type = Image.Type.Simple; rewardIcon.preserveAspect = true;
            Anchor(rewardIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(96, 96));
            var rewardName = Text("Name", rewardRow.transform, "", 26, Gold, TextAlignmentOptions.Left);
            Anchor(rewardName.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(132, 18), new Vector2(450, 40));
            var rewardEffect = Text("Effect", rewardRow.transform, "", 22, new Color(0.62f, 0.95f, 0.58f), TextAlignmentOptions.Left, FontStyles.Bold);
            Anchor(rewardEffect.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(132, -22), new Vector2(450, 34));

            var retry = MakeButton("RetryButton", resultBox.transform, "PLAY AGAIN", new Color(0.32f, 0.62f, 0.28f), Cream, 24);
            Anchor((RectTransform)retry.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-150, 28), new Vector2(260, 60));
            var resultMenu = MakeButton("MainMenuButton", resultBox.transform, "MAIN MENU", new Color(0.62f, 0.46f, 0.12f), Cream, 24);
            Anchor((RectTransform)resultMenu.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(150, 28), new Vector2(260, 60));

            // ── Word preview component ───────────────────────────────────────
            var preview = previewRow.gameObject.AddComponent<WordPreview>();
            var pso = new SerializedObject(preview);
            pso.FindProperty("grid").objectReferenceValue = grid;
            pso.FindProperty("row").objectReferenceValue = previewRow;
            pso.FindProperty("letterTemplate").objectReferenceValue = letterTemplate.gameObject;
            pso.FindProperty("hintLabel").objectReferenceValue = hint;
            pso.ApplyModifiedPropertiesWithoutUndo();

            // ── Buff slots ───────────────────────────────────────────────────
            var bso = new SerializedObject(buffInventory);
            var slots = bso.FindProperty("slots");
            slots.arraySize = 3;
            int[] minLengths = { 5, 6, 7 };
            string[] names = { "Healing Potion", "Warding Potion", "Power Potion" };
            for (int i = 0; i < 3; i++)
            {
                var s = slots.GetArrayElementAtIndex(i);
                s.FindPropertyRelative("type").enumValueIndex = i;
                s.FindPropertyRelative("displayName").stringValue = names[i];
                s.FindPropertyRelative("minWordLength").intValue = minLengths[i];
                s.FindPropertyRelative("button").objectReferenceValue = cards[i].button;
                s.FindPropertyRelative("countLabel").objectReferenceValue = cards[i].count;
                s.FindPropertyRelative("group").objectReferenceValue = cards[i].group;
            }
            bso.ApplyModifiedPropertiesWithoutUndo();

            // ── Battle manager ───────────────────────────────────────────────
            var managers = Node("Managers", canvas);
            var battle = managers.gameObject.AddComponent<BattleManager>();
            var m = new SerializedObject(battle);
            Set(m, "chapterLabel", chapter);
            Set(m, "encounterLabel", encounter);
            Set(m, "player", albularyo);
            Set(m, "playerAnimator", playerAnim);
            Set(m, "playerNameLabel", playerName);
            Set(m, "playerHearts", playerHearts);
            Set(m, "playerStatusLabel", status);
            Set(m, "enemyStatusLabel", enemyStatus);
            Set(m, "shakeTarget", stage);
            Set(m, "fxSprite", AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/soft_glow.png").OfType<Sprite>().FirstOrDefault() ?? circleSprite);
            var enemies = m.FindProperty("enemies");
            enemies.arraySize = foes.Length;
            for (int i = 0; i < foes.Length; i++) enemies.GetArrayElementAtIndex(i).objectReferenceValue = foes[i];
            m.FindProperty("chapterTitle").stringValue = spec.chapterTitle;
            m.FindProperty("firstBoard").stringValue = spec.firstBoard;
            m.FindProperty("victoryMessage").stringValue = spec.victoryMessage;
            if (!string.IsNullOrEmpty(spec.nextStoryAsset))
                Set(m, "nextStory", Load<Orasyon.Story.StoryScript>(StoryFolder + spec.nextStoryAsset + ".asset"));
            Set(m, "enemyAnimator", enemyAnim);
            Set(m, "enemyNameLabel", enemyName);
            Set(m, "enemyHearts", enemyHearts);
            Set(m, "enemyInfo", info);
            Set(m, "background", parallax);
            Set(m, "letterGrid", grid);
            Set(m, "gridGroup", gridGroup);
            Set(m, "attackButton", attack);
            Set(m, "clearButton", clear);
            Set(m, "hintButton", hintBtn);
            Set(m, "scrambleButton", scramble);
            Set(m, "menuButton", menu);
            Set(m, "buffs", buffInventory);
            Set(m, "feedbackLabel", feedback);
            Set(m, "damagePopupTemplate", popup);
            Set(m, "resultPanel", result.gameObject);
            Set(m, "resultTitle", resultTitle);
            Set(m, "resultBody", resultBody);
            Set(m, "retryButton", retry);
            Set(m, "resultMenuButton", resultMenu);
            Set(m, "bossReward", Load<TreasureDefinition>(TreasureFolder + spec.rewardAsset + ".asset"));
            Set(m, "treasureShelf", treasureShelf);
            Set(m, "dropIconTemplate", dropIcon);
            Set(m, "rewardGroup", rewardRow.gameObject);
            Set(m, "rewardIcon", rewardIcon);
            Set(m, "rewardName", rewardName);
            Set(m, "rewardEffect", rewardEffect);
            m.ApplyModifiedPropertiesWithoutUndo();

            // ── Tutorial manager (first chapter only) ────────────────────────
            if (spec.tutorial)
            {
                var tutorial = managers.gameObject.AddComponent<TutorialManager>();
                var t = new SerializedObject(tutorial);
                Set(t, "tutorialPanel", tutPanel.gameObject);
                Set(t, "titleText", tutTitle);
                Set(t, "bodyText", tutBody);
                Set(t, "nextButton", tutNext);
                Set(t, "nextButtonLabel", tutNext.GetComponentInChildren<TextMeshProUGUI>());
                Set(t, "battle", battle);
                Set(t, "letterGrid", grid);
                Set(t, "bottomGroup", bottomGroup);
                t.FindProperty("sampleWord").stringValue = "WORD";
                var positions = t.FindProperty("sampleWordPositions");
                positions.arraySize = 4;
                for (int i = 0; i < 4; i++) positions.GetArrayElementAtIndex(i).vector2IntValue = new Vector2Int(0, i);
                t.ApplyModifiedPropertiesWithoutUndo();
                tutPanel.transform.SetAsLastSibling();
            }
            else
            {
                Object.DestroyImmediate(tutPanel.gameObject);
            }

            result.gameObject.SetActive(false);
            result.transform.SetAsLastSibling();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Orasyon] {spec.chapterTitle} battle UI rebuilt in {spec.scenePath}.");
        }

        // ── Character data ───────────────────────────────────────────────────
        private static void UpdateCharacterData()
        {
            var albularyo = Load<CharacterDefinition>(DataFolder + "Albularyo.asset");
            albularyo.walk = Clip("Main Character", "Walking");
            albularyo.spriteSize = 320f;
            EditorUtility.SetDirty(albularyo);

            var bat = Load<CharacterDefinition>(DataFolder + "01_ForestBat.asset");
            bat.attackName = "Wing Gust";
            bat.themeWords = new[] { "BAT", "WING", "NIGHT", "CAVE", "FANG" };
            bat.hoverHeight = 70f;
            bat.spriteSize = 280f;
            bat.description = "Little bats flit between the old trees after dusk, keeping watch for the bigger spirits of the forest. " +
                              "Alone they are only a nuisance. When they gather, something larger is never far behind.";
            EditorUtility.SetDirty(bat);

            var tree = Load<CharacterDefinition>(DataFolder + "02_BaleteTreeMinion.asset");
            tree.attackName = "Root Lash";
            tree.themeWords = new[] { "ROOT", "TREE", "LEAF", "BARK", "VINE" };
            tree.spriteSize = 320f;
            tree.description = "Elders warn children never to point at a balete tree. " +
                               "Its tangled roots remember every insult, and sometimes they pull themselves out of the ground to settle the score.";
            EditorUtility.SetDirty(tree);

            var king = Load<CharacterDefinition>(DataFolder + "03_DuwendeKing.asset");
            king.attackName = "Staff Smash";
            king.themeWords = new[] { "MOUND", "GOLD", "KING", "DIRT", "GNOME" };
            king.spriteSize = 380f;
            king.description = "Ruler of the backyard mounds. Walk past his home without a respectful \"tabi tabi po\" " +
                               "and his mischief will follow you for days. Today, he is not in a forgiving mood.";
            EditorUtility.SetDirty(king);

            // ── Chapter 2 (created from the sprite folders the first time) ──
            var mushroom = Creature("04_EnchantedMushroom", "Enchanted Mushroom 1st Enemy", "Enchanted Mushroom",
                idle: null, attack: "Attack", hurt: null, death: null);
            mushroom.attackName = "Spore Burst";
            mushroom.themeWords = new[] { "SPORE", "MOSS", "DEW", "CAP", "GLOW" };
            mushroom.spriteSize = 260f;
            mushroom.description = "Glowing mushrooms sprout wherever the Kapre's smoke settles. " +
                                   "Breathe too deeply near one and you wake up hours later, wondering where the path went.";
            EditorUtility.SetDirty(mushroom);

            var minion = Creature("05_DuwendeMinion", "Duwende_Minion 2nd Enemy", "Duwende Minion",
                idle: "Idle", attack: "Attack", hurt: "Hurt", death: "The_Duwende_minion_becomes_dizzy");
            minion.attackName = "Club Bonk";
            minion.themeWords = new[] { "CLUB", "ELF", "IMP", "BEARD", "MOUND" };
            minion.spriteSize = 280f;
            minion.description = "A loyal servant of the Duwende King, still sore about his master's defeat. " +
                                 "He followed you into the forest looking for revenge, and for the club he keeps dropping.";
            EditorUtility.SetDirty(minion);

            var kapre = Creature("06_Kapre", "Kapre_2nd_Stage_Boss", "Kapre",
                idle: "Idle", attack: "The_Kapre_draws", hurt: "Hurt", death: "Death");
            kapre.isBoss = true;
            kapre.attackName = "Smoke Slam";
            kapre.themeWords = new[] { "SMOKE", "CIGAR", "GIANT", "BALETE", "ASH" };
            kapre.spriteSize = 430f;

            // ── Movesets (overwritten on every rebuild; tune here or in the assets) ──
            bat.attacks = new[]
            {
                Atk("Wing Gust", 1, 2, weight: 3, crit: 0.10f),
                Atk("Fang Bite", 1, 1, weight: 2, effect: AttackEffect.Poison, chance: 0.6f, turns: 2, power: 1),
            };
            tree.attacks = new[]
            {
                Atk("Root Lash", 2, 3, weight: 3, crit: 0.10f),
                Atk("Entangle",  1, 1, weight: 1, effect: AttackEffect.Stun, chance: 0.5f),
            };
            king.attacks = new[]
            {
                Atk("Staff Smash",   3, 4, weight: 3, crit: 0.20f),
                Atk("Mischief Hex",  1, 2, weight: 2, effect: AttackEffect.Weaken, turns: 2, power: 40),
                Atk("Mound Shelter", 0, 0, weight: 1, effect: AttackEffect.Guard,  turns: 2, power: 50),
            };
            mushroom.attacks = new[]
            {
                Atk("Spore Burst",  1, 2, weight: 3, effect: AttackEffect.Poison, chance: 0.6f, turns: 3, power: 1),
                Atk("Sleepy Spores", 1, 1, weight: 1, effect: AttackEffect.Stun, chance: 0.5f),
            };
            minion.attacks = new[]
            {
                Atk("Club Bonk", 2, 3, weight: 3, crit: 0.25f),
                Atk("Dirt Toss", 1, 1, weight: 1, effect: AttackEffect.Weaken, chance: 0.8f, turns: 2, power: 30),
            };
            kapre.attacks = new[]
            {
                Atk("Smoke Slam",    3, 5, weight: 3, crit: 0.15f),
                Atk("Choking Smoke", 2, 2, weight: 2, effect: AttackEffect.Poison, chance: 0.8f, turns: 2, power: 2),
                Atk("Ground Stomp",  2, 3, weight: 1, effect: AttackEffect.Stun, chance: 0.5f),
                Atk("Long Drag",     0, 0, weight: 1, effect: AttackEffect.Leech, power: 6),
            };
            foreach (var d in new[] { bat, tree, king, mushroom, minion, kapre })
            {
                d.attackName = d.attacks[0].name;   // keep the legacy single-attack fields in sync
                d.attackDamage = d.attacks[0].maxDamage;
                EditorUtility.SetDirty(d);
            }
            kapre.description = "A towering spirit who sits in the oldest balete, smoking a cigar that never burns out. " +
                                "He is not cruel, only proud, and he has no patience for visitors who forget their manners.";
            EditorUtility.SetDirty(kapre);

            AssetDatabase.SaveAssets();
        }

        private static EnemyAttack Atk(string name, int min, int max, float weight = 1f, float crit = 0f,
                                       AttackEffect effect = AttackEffect.None, float chance = 1f, int turns = 2, int power = 1)
            => new EnemyAttack
            {
                name = name, minDamage = min, maxDamage = max, weight = weight, critChance = crit,
                effect = effect, effectChance = chance, effectTurns = turns, effectPower = power,
            };

        /// <summary>Loads or creates a creature's data. Clip names are folder-name prefixes; null = not available.</summary>
        private static CharacterDefinition Creature(string file, string folder, string displayName,
                                                    string idle, string attack, string hurt, string death)
        {
            string path = DataFolder + file + ".asset";
            var def = Load<CharacterDefinition>(path);
            bool isNew = def == null;
            if (isNew)
            {
                def = ScriptableObject.CreateInstance<CharacterDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }
            def.displayName = displayName;
            def.idle   = idle != null ? Clip(folder, idle) : new Texture2D[0];
            def.attack = Clip(folder, attack);
            def.hurt   = hurt != null ? Clip(folder, hurt) : new Texture2D[0];
            def.death  = death != null ? Clip(folder, death) : new Texture2D[0];
            def.cast   = new Texture2D[0];
            def.walk   = new Texture2D[0];
            if (def.idle.Length == 0)
            {
                // No idle animation: use the front-facing still from the rotations folder
                var still = Load<Texture2D>(CharFolder + folder + "/Idle/rotations/south.png");
                def.idle = still != null ? new[] { still } : def.attack.Take(1).ToArray();
            }
            if (isNew)
            {
                // Starting stats only; tune them in the Inspector afterwards
                def.maxHP = 30; def.attackDamage = 3; def.framesPerSecond = 10f;
                if (file.Contains("Kapre")) { def.maxHP = 50; def.attackDamage = 5; }
                if (file.Contains("Mushroom")) def.attackDamage = 2;
            }
            return def;
        }

        private static Texture2D[] Clip(string character, string prefix)
        {
            string anims = CharFolder + character + "/Idle/animations";
            var clipFolder = AssetDatabase.GetSubFolders(anims)
                .FirstOrDefault(f => Path.GetFileName(f) == prefix || Path.GetFileName(f).StartsWith(prefix + "_")
                                     || Path.GetFileName(f).StartsWith(prefix));
            if (clipFolder == null) return new Texture2D[0];
            var dir = AssetDatabase.GetSubFolders(clipFolder).FirstOrDefault() ?? clipFolder;
            return AssetDatabase.FindAssets("t:Texture2D", new[] { dir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetDirectoryName(p).Replace('\\', '/') == dir)
                .OrderBy(p => p)
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>)
                .ToArray();
        }

        // ── Pieces ───────────────────────────────────────────────────────────
        private static GameObject BuildTilePrefab()
        {
            var root = new GameObject("LetterTile", typeof(RectTransform), typeof(Image), typeof(Button));
            ((RectTransform)root.transform).sizeDelta = new Vector2(92, 92);
            var bg = root.GetComponent<Image>();
            bg.sprite = roundedSprite; bg.type = Image.Type.Sliced; bg.color = Parchment;
            AddShadow(root);
            var btn = root.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = bg;

            var ring = Panel("HighlightRing", root.transform, new Color(0.4f, 1f, 0.4f, 0.35f), roundedSprite);
            Stretch(ring.rectTransform, -6);
            ring.raycastTarget = false;
            ring.enabled = false;

            var letter = Text("Letter", root.transform, "A", 52, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(letter.rectTransform);

            var tile = root.AddComponent<LetterTile>();
            var so = new SerializedObject(tile);
            so.FindProperty("background").objectReferenceValue = bg;
            so.FindProperty("highlightRing").objectReferenceValue = ring;
            so.FindProperty("letterLabel").objectReferenceValue = letter;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, TilePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
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

        private static ParallaxBackground BuildBackground(RectTransform stage, string backgroundPack)
        {
            var bgRoot = Node("Background", stage);
            Anchor(bgRoot, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(1920, 1080));
            var parallax = bgRoot.gameObject.AddComponent<ParallaxBackground>();

            var layerPaths = Directory.GetFiles(backgroundPack, "*.png")
                .Select(p => p.Replace('\\', '/'))
                .Where(p => int.TryParse(Path.GetFileNameWithoutExtension(p), out _))
                .OrderBy(p => int.Parse(Path.GetFileNameWithoutExtension(p)))
                .ToArray();

            var so = new SerializedObject(parallax);
            var layers = so.FindProperty("layers");
            layers.arraySize = layerPaths.Length;
            for (int i = 0; i < layerPaths.Length; i++)
            {
                var tex = Load<Texture2D>(layerPaths[i]);
                var layer = Node("Layer_" + (i + 1), bgRoot);
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
                e.FindPropertyRelative("speed").floatValue = layerPaths.Length == 1 ? 1f : Mathf.Lerp(0.1f, 1f, i / (float)(layerPaths.Length - 1));
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return parallax;
        }

        private static UIFrameAnimator Character(string name, RectTransform parent, CharacterDefinition def, float x)
        {
            var rt = Node(name, parent);
            Anchor(rt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(x, 30f + def.hoverHeight), Vector2.one * def.spriteSize);
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            raw.texture = def.idle.Length > 0 ? def.idle[0] : null;
            return rt.gameObject.AddComponent<UIFrameAnimator>();
        }

        private static HeartBar Hearts(string name, Transform parent, bool mirrored)
        {
            var rt = Node(name, parent);
            var layout = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 3;
            layout.childAlignment = mirrored ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            layout.reverseArrangement = mirrored;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var bar = rt.gameObject.AddComponent<HeartBar>();
            var so = new SerializedObject(bar);
            so.FindProperty("heartSprite").objectReferenceValue = AssetDatabase.LoadAllAssetsAtPath(HeartSprite).OfType<Sprite>().FirstOrDefault();
            so.FindProperty("mirrored").boolValue = mirrored;
            so.ApplyModifiedPropertiesWithoutUndo();
            return bar;
        }

        private struct Card { public Button button; public TextMeshProUGUI count; public CanvasGroup group; }

        /// <summary>One potion on the shelf: flask icon with a count badge, name, effect and how to earn it.</summary>
        private static Card PotionSlot(Transform parent, int index, string title, string effect, string requirement, Sprite potion)
        {
            var slot = Panel("Potion_" + title, parent, new Color(0, 0, 0, 0));   // invisible hit area
            Anchor(slot.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(14 + index * 188, -10), new Vector2(176, 180));
            var group = slot.gameObject.AddComponent<CanvasGroup>();

            var icon = Panel("Icon", slot.transform, Color.white);
            icon.sprite = potion;
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            Anchor(icon.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(104, 104));

            var button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = icon;
            var colors = button.colors;
            colors.normalColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 1f);
            button.colors = colors;

            var count = Text("Count", slot.transform, "x0", 24, Cream, TextAlignmentOptions.Left, FontStyles.Bold);
            Anchor(count.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, 1), new Vector2(38, -72), new Vector2(60, 32));
            count.outlineWidth = 0.25f;
            count.outlineColor = new Color32(20, 10, 5, 255);

            var n = Text("Name", slot.transform, title, 20, Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            Anchor(n.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -108), new Vector2(172, 26));
            var eff = Text("Effect", slot.transform, effect, 15, Cream, TextAlignmentOptions.Center);
            Anchor(eff.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -134), new Vector2(172, 22));
            var req = Text("Requirement", slot.transform, requirement, 13, new Color(0.80f, 0.74f, 0.62f), TextAlignmentOptions.Center, FontStyles.Italic);
            Anchor(req.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -156), new Vector2(172, 20));

            return new Card { button = button, count = count, group = group };
        }

        private static Sprite PotionSprite(string name)
        {
            return AssetDatabase.LoadAllAssetsAtPath($"Assets/Sprites/Icons/Potions/{name}.png").OfType<Sprite>().FirstOrDefault();
        }

        // ── Generic helpers ──────────────────────────────────────────────────
        private static void EnsureEventSystem(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<EventSystem>(true) != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        private static void Set(SerializedObject so, string prop, Object value)
        {
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogError($"[Orasyon] Missing property '{prop}' on {so.targetObject.GetType().Name}"); return; }
            p.objectReferenceValue = value;
        }

        private static string Glyph(string preferred, string fallback)
        {
            var font = TMP_Settings.defaultFontAsset;
            return font != null && font.HasCharacters(preferred) ? preferred : fallback;
        }

        private static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static Image Panel(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var rt = Node(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }
            return img;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color,
                                            TextAlignmentOptions align, FontStyles style = FontStyles.Normal)
        {
            var rt = Node(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align; t.fontStyle = style;
            t.raycastTarget = false;
            return t;
        }

        private static Button MakeButton(string name, Transform parent, string label, Color color, Color labelColor, float fontSize)
        {
            var img = Panel(name, parent, color, roundedSprite);
            AddOutline(img.gameObject, new Color(0.10f, 0.06f, 0.03f, 0.9f), 2);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor      = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.highlightedColor = Color.white;
            colors.pressedColor     = new Color(0.70f, 0.70f, 0.70f, 1f);
            colors.selectedColor    = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.disabledColor    = new Color(0.40f, 0.40f, 0.40f, 0.75f);
            colors.fadeDuration     = 0.08f;
            btn.colors = colors;
            var t = Text("Label", img.transform, label, fontSize, labelColor, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(t.rectTransform);
            return btn;
        }

        private static void AddOutline(GameObject go, Color color, float width)
        {
            var o = go.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(width, -width);
        }

        private static void AddShadow(GameObject go)
        {
            var s = go.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.45f);
            s.effectDistance = new Vector2(0f, -4f);
        }
    }
}
