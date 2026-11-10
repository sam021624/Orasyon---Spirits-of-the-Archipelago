using System.Collections.Generic;
using System.IO;
using System.Linq;
using Orasyon.Battle;
using UnityEditor;
using UnityEngine;

namespace Orasyon.EditorTools
{
    /// <summary>
    /// Builds the CharacterDefinition assets from the art in Assets/Sprites/Characters-New.
    ///
    /// Folder layout drives the fight order: Chapter N / {slot}_{creature}. Slots 1 and 2 are regular enemies,
    /// slot 3 is the boss. Rename or move folders to reorder the fights, then rebuild.
    /// Stats and movesets live in the <see cref="Table"/> below (keyed by the creature id in the folder name)
    /// and are re-applied on every run, so edit them here, not only in the Inspector.
    /// </summary>
    public static class CharacterAssets
    {
        public const string DataFolder = "Assets/Data/Characters/";
        public const string ArtRoot    = "Assets/Sprites/Characters-New/";
        public const int    ChapterCount = 5;

        private sealed class Creature
        {
            public string displayName;
            public int    hp;
            public float  size;
            public float  hover;
            public string[] weak;
            public string description;
            public EnemyAttack[] attacks;
            public EnemyMechanic mechanic;
            public bool noSignatureCycle;   // the signature move is triggered by a rule instead of a timer
        }

        // ── Public API ───────────────────────────────────────────────────────
        /// <summary>Asset file names (no extension) of a chapter's enemies in fight order (the last one is the boss).</summary>
        public static string[] EnemyAssetNames(int chapter) =>
            EnemyFolders(chapter).Select(f => AssetName(chapter, f.slot, f.id)).ToArray();

        /// <summary>Creates or updates the hero and every enemy of every chapter.</summary>
        [MenuItem("Tools/Orasyon/Create Character Assets")]
        public static void EnsureAll()
        {
            VfxAssets.EnsureAll();
            table = null;   // rebuild so move VFX references are fresh
            Directory.CreateDirectory(DataFolder.TrimEnd('/'));
            BuildHero();
            for (int c = 1; c <= ChapterCount; c++) BuildChapter(c);
            AssetDatabase.SaveAssets();
            Debug.Log("[Orasyon] Character assets updated.");
        }

        // ── Folder scan ──────────────────────────────────────────────────────
        private struct EnemyFolder { public int slot; public string id; public string path; }

        private static List<EnemyFolder> EnemyFolders(int chapter)
        {
            var list = new List<EnemyFolder>();
            string chapterPath = $"{ArtRoot}Chapter {chapter}";
            if (!AssetDatabase.IsValidFolder(chapterPath)) return list;

            foreach (var sub in AssetDatabase.GetSubFolders(chapterPath))
            {
                string name = Path.GetFileName(sub);
                int us = name.IndexOf('_');
                if (us <= 0 || !int.TryParse(name.Substring(0, us), out int slot)) continue;
                list.Add(new EnemyFolder { slot = slot, id = name.Substring(us + 1), path = sub });
            }
            return list.OrderBy(f => f.slot).ToList();
        }

        private static string AssetName(int chapter, int slot, string id) => $"Ch{chapter}_{slot}_{Pascal(id)}";

        private static string Pascal(string id) =>
            string.Concat(id.Split('_').Select(p => p.Length == 0 ? "" : char.ToUpperInvariant(p[0]) + p.Substring(1)));

        // ── Builders ─────────────────────────────────────────────────────────
        private static void BuildChapter(int chapter)
        {
            var folders = EnemyFolders(chapter);
            for (int i = 0; i < folders.Count; i++)
            {
                var f = folders[i];
                bool boss = i == folders.Count - 1;
                string path = DataFolder + AssetName(chapter, f.slot, f.id) + ".asset";

                var def = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
                if (def == null)
                {
                    def = ScriptableObject.CreateInstance<CharacterDefinition>();
                    AssetDatabase.CreateAsset(def, path);
                }

                if (!Table.TryGetValue(f.id, out var data))
                {
                    Debug.LogWarning($"[Orasyon] No stats for '{f.id}' in CharacterAssets.Table; using defaults.");
                    data = new Creature { displayName = Pascal(f.id), hp = 35, size = 320f, attacks = new[] { Atk("Strike", 2, 3) } };
                }

                def.displayName = data.displayName;
                def.isBoss = boss;
                def.maxHP = data.hp;
                def.spriteSize = data.size;
                def.hoverHeight = data.hover;
                def.framesPerSecond = 12f;
                def.spriteFacesRight = false;   // all enemy art faces left or front
                def.themeWords = data.weak;
                def.description = data.description;
                def.attacks = data.attacks;
                def.signatureEvery = boss && !data.noSignatureCycle ? 3 : 0;   // bosses: every 3rd enemy turn is the signature move
                def.mechanic = data.mechanic;
                def.attackName = data.attacks[0].name;
                def.attackDamage = data.attacks[0].maxDamage;

                def.idle   = Clip(f.path, f.id, "idle");
                def.walk   = Clip(f.path, f.id, "walking");
                def.attack = Clip(f.path, f.id, "attack");
                def.cast   = Clip(f.path, f.id, "cast");
                def.hurt   = Clip(f.path, f.id, "hurt");
                def.death  = Clip(f.path, f.id, "death");
                def.kidlat = new FrameClip();
                def.flurry = new FrameClip();
                EditorUtility.SetDirty(def);
            }
        }

        private static void BuildHero()
        {
            string path = DataFolder + "Albularyo.asset";   // existing scenes reference this asset
            var def = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<CharacterDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }

            string dir = ArtRoot + "Hero/hero";
            def.displayName = "Albularyo";
            def.isBoss = false;
            def.maxHP = 30;
            def.spriteSize = 320f;
            def.hoverHeight = 0f;
            def.framesPerSecond = 10f;
            def.spriteFacesRight = true;
            def.idle   = Clip(dir, "hero", "idle");
            def.walk   = Clip(dir, "hero", "walking");
            def.attack = Clip(dir, "hero", "attack");
            def.cast   = Clip(dir, "hero", "cast");
            def.hurt   = Clip(dir, "hero", "hurt");
            def.death  = Clip(dir, "hero", "death");
            def.kidlat = Clip(dir, "hero", "kidlat");
            def.flurry = Clip(dir, "hero", "flurry", fps: 16f);
            EditorUtility.SetDirty(def);
        }

        /// <summary>One strip. Cells are square (height x height), so the frame count is width / height.</summary>
        private static FrameClip Clip(string folder, string id, string clip, float fps = 0f)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{id}_{clip}.png");
            if (tex == null)
            {
                // Folder names can be longer than the file prefix (duwende_minion / minion_idle): match by suffix.
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                {
                    string p = AssetDatabase.GUIDToAssetPath(guid);
                    string n = Path.GetFileNameWithoutExtension(p);
                    if (n == clip || n.EndsWith("_" + clip)) { tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p); break; }
                }
            }
            if (tex == null) return new FrameClip();
            return new FrameClip(tex, Mathf.RoundToInt(tex.width / (float)tex.height), fps);
        }

        // ── Moves ────────────────────────────────────────────────────────────
        private static EnemyAttack Atk(string name, int min, int max, float weight = 1f, float crit = 0f,
                                       AttackEffect effect = AttackEffect.None, float chance = 1f, int turns = 2, int power = 1,
                                       string vfx = null, bool travels = false, string hit = null, bool signature = false,
                                       GridEffectType grid = GridEffectType.None, int gridCount = 0, int gridTurns = 0, int hits = 1)
            => new EnemyAttack
            {
                isSignature = signature,
                gridEffect = grid, gridEffectCount = gridCount, gridEffectTurns = gridTurns, hits = hits,
                name = name, minDamage = min, maxDamage = max, weight = weight, critChance = crit,
                effect = effect, effectChance = chance, effectTurns = turns, effectPower = power,
                vfx = vfx != null ? VfxAssets.Load(vfx) : null,
                vfxTravels = travels,
                hitVfx = hit != null ? VfxAssets.Load(hit) : null,
            };

        // ── Creature table (id = folder name after the slot number) ──────────
        // Built on first use, after the VFX assets exist (Atk looks them up by name).
        private static Dictionary<string, Creature> table;
        private static Dictionary<string, Creature> Table => table ??= BuildTable();

        private static Dictionary<string, Creature> BuildTable() => new Dictionary<string, Creature>
        {
            // Chapter 1: The Backyard Mound
            { "duwende_minion", new Creature {
                displayName = "Duwende Minion", hp = 30, size = 300f,
                weak = new[] { "CLUB", "ELF", "IMP", "BEARD", "MOUND" },
                description = "A loyal little servant of the Duwende King, never far from the mound. " +
                              "He is mostly bark and very little bite, and he keeps dropping his club.",
                attacks = new[]
                {
                    Atk("Club Bonk", 1, 2, weight: 3, crit: 0.10f, vfx: "minion_bonk"),
                    Atk("Dirt Toss", 1, 1, weight: 1, effect: AttackEffect.Weaken, chance: 0.6f, turns: 2, power: 30, vfx: "minion_bonk"),
                    Atk("Pilfer",    1, 1, weight: 2, vfx: "minion_bonk", grid: GridEffectType.StealTiles, gridCount: 1, gridTurns: 2),
                } } },
            { "enchanted_mushroom", new Creature {
                displayName = "Enchanted Mushroom", hp = 30, size = 300f,
                weak = new[] { "SPORE", "MOSS", "DEW", "CAP", "GLOW" },
                description = "Glowing mushrooms sprout wherever the little folk dance. " +
                              "Breathe too deeply near one and you wake up hours later, wondering where the path went.",
                attacks = new[]
                {
                    Atk("Spore Burst",   1, 2, weight: 3, effect: AttackEffect.Poison, chance: 0.6f, turns: 3, power: 1, vfx: "mushroom_spores"),
                    Atk("Sleepy Spores", 1, 1, weight: 1, effect: AttackEffect.Stun, chance: 0.5f, vfx: "mushroom_spores"),
                } } },
            { "duwende_king", new Creature {
                displayName = "Duwende King", hp = 80, size = 400f,
                weak = new[] { "MOUND", "GOLD", "KING", "DIRT", "GNOME" },
                description = "Ruler of the backyard mounds. Walk past his home without a respectful \"tabi tabi po\" " +
                              "and his mischief will follow you for days. Today, he is not in a forgiving mood.",
                attacks = new[]
                {
                    Atk("Staff Smash",   3, 4, weight: 3, crit: 0.20f, vfx: "minion_bonk"),
                    Atk("Mischief Hex",  1, 2, weight: 2, effect: AttackEffect.Weaken, turns: 2, power: 40, vfx: "king_curse", travels: true),
                    Atk("Mound Shelter", 0, 0, weight: 1, effect: AttackEffect.Guard,  turns: 2, power: 50, vfx: "king_curse"),
                    Atk("Pilfer",        1, 2, weight: 2, vfx: "king_curse", grid: GridEffectType.StealTiles, gridCount: 2, gridTurns: 2),
                    Atk("Mound Collapse", 4, 5, weight: 0, effect: AttackEffect.Weaken, turns: 2, power: 50, vfx: "king_curse", signature: true),
                } } },

            // Chapter 2: The Old Balete
            { "bat", new Creature {
                displayName = "Forest Bat", hp = 30, size = 300f, hover = 70f,
                weak = new[] { "BAT", "WING", "NIGHT", "CAVE", "FANG" },
                description = "Little bats flit between the old trees after dusk, keeping watch for the bigger spirits of the forest. " +
                              "Alone they are only a nuisance. When they gather, something larger is never far behind.",
                attacks = new[]
                {
                    Atk("Wing Gust", 1, 2, weight: 3, crit: 0.10f, vfx: "bat_drain"),
                    Atk("Fang Bite", 1, 1, weight: 2, effect: AttackEffect.Poison, chance: 0.6f, turns: 2, power: 1, vfx: "bat_drain"),
                } } },
            { "balete_tree", new Creature {
                displayName = "Balete Tree Minion", hp = 30, size = 380f,
                weak = new[] { "ROOT", "TREE", "LEAF", "BARK", "VINE" },
                description = "Elders warn children never to point at a balete tree. " +
                              "Its tangled roots remember every insult, and sometimes they pull themselves out of the ground to settle the score.",
                attacks = new[]
                {
                    Atk("Root Lash", 2, 3, weight: 3, crit: 0.10f, vfx: "balete_roots"),
                    Atk("Entangle",  1, 1, weight: 1, effect: AttackEffect.Stun, chance: 0.5f, vfx: "balete_roots"),
                } } },
            { "kapre", new Creature {
                displayName = "Kapre", hp = 100, size = 440f,
                weak = new[] { "SMOKE", "CIGAR", "GIANT", "BALETE", "ASH" },
                description = "A towering spirit who sits in the oldest balete, smoking a cigar that never burns out. " +
                              "He is not cruel, only proud, and he has no patience for visitors who forget their manners.",
                attacks = new[]
                {
                    Atk("Smoke Slam",    3, 5, weight: 3, crit: 0.15f, vfx: "kapre_smoke"),
                    Atk("Smoke Veil",    1, 1, weight: 2, vfx: "kapre_smoke", grid: GridEffectType.SmokeTiles, gridCount: 4, gridTurns: 3),
                    Atk("Choking Smoke", 2, 2, weight: 1, effect: AttackEffect.Poison, chance: 0.8f, turns: 2, power: 2, vfx: "kapre_smoke"),
                    Atk("Ground Stomp",  2, 3, weight: 1, effect: AttackEffect.Stun, chance: 0.5f),
                    Atk("Long Drag",     0, 0, weight: 1, effect: AttackEffect.Leech, power: 6, vfx: "kapre_smoke"),
                    Atk("Great Cloud",   3, 4, weight: 0, effect: AttackEffect.Poison, turns: 3, power: 2, vfx: "kapre_smoke", signature: true,
                        grid: GridEffectType.SmokeTiles, gridCount: 6, gridTurns: 3),
                } } },

            // Chapter 3: The Deep Forest
            { "santelmo", new Creature {
                displayName = "Santelmo", hp = 35, size = 290f, hover = 60f,
                weak = new[] { "FIRE", "FLAME", "GLOW", "SPARK", "LIGHT" },
                description = "A drifting ball of cold blue fire that haunts the forest paths. " +
                              "Travelers who follow it in the dark are never quite sure where they end up.",
                attacks = new[]
                {
                    Atk("Fire Orb",   2, 3, weight: 3, crit: 0.10f, vfx: "santelmo_orb", travels: true, hit: "santelmo_burst"),
                    Atk("Cold Flame", 1, 2, weight: 2, effect: AttackEffect.Poison, chance: 0.6f, turns: 3, power: 1, vfx: "santelmo_flames"),
                    Atk("Will-o-wisp", 1, 2, weight: 2, vfx: "santelmo_flames", grid: GridEffectType.BurnTiles, gridCount: 2),
                } } },
            { "sigbin", new Creature {
                displayName = "Sigbin", hp = 35, size = 360f,
                weak = new[] { "SHADOW", "HEEL", "NIGHT", "TAIL", "HOUND" },
                description = "A hound-like creature that walks backwards with its head between its legs, " +
                              "drinking the shadows of the unwary. It is said to be at its strongest at midnight.",
                attacks = new[]
                {
                    Atk("Shadow Bite", 2, 3, weight: 3, crit: 0.15f, vfx: "sigbin_bite"),
                    Atk("Shadow Veil", 1, 1, weight: 1, effect: AttackEffect.Weaken, chance: 0.8f, turns: 2, power: 40, vfx: "sigbin_shadow"),
                    Atk("Gapos",       1, 1, weight: 2, vfx: "sigbin_shadow", grid: GridEffectType.LockTiles, gridCount: 3, gridTurns: 2),
                } } },
            { "tiyanak", new Creature {
                displayName = "Tiyanak", hp = 120, size = 340f,
                weak = new[] { "LULLABY", "CRADLE", "BABY", "FOREST", "CRY" },
                description = "It cries like a lost baby to lure travelers off the path. " +
                              "Take it in your arms and the sweet face twists into something sharp-toothed and furious.",
                attacks = new[]
                {
                    Atk("Scratch",     1, 2, weight: 3, crit: 0.20f, vfx: "tiyanak_slash", hits: 2),
                    Atk("Fake Smile",  1, 2, weight: 2, vfx: "tiyanak_slash", grid: GridEffectType.HexTiles, gridCount: 2, gridTurns: 3),
                    Atk("Wail",        1, 1, weight: 2, effect: AttackEffect.Stun, chance: 0.5f, vfx: "tiyanak_wail"),
                    Atk("Mimic Cry",   0, 0, weight: 1, effect: AttackEffect.Guard, turns: 2, power: 40, vfx: "tiyanak_wail"),
                    Atk("Inconsolable Wail", 2, 3, weight: 0, effect: AttackEffect.Stun, chance: 0.8f, vfx: "tiyanak_wail", signature: true,
                        grid: GridEffectType.ScrambleAll),
                } } },

            // Chapter 4: The Village Rooftops
            { "amalanhig", new Creature {
                displayName = "Amalanhig", hp = 40, size = 400f,
                weak = new[] { "GRAVE", "TOMB", "BONES", "DUST", "SPIRIT" },
                description = "A restless dead man who climbs out of his grave to bite the living. " +
                              "He moves slowly, but he never tires, and he never lets go.",
                attacks = new[]
                {
                    Atk("Grave Grip", 2, 4, weight: 3, crit: 0.15f, vfx: "amalanhig_grip"),
                    Atk("Cold Touch", 2, 2, weight: 1, effect: AttackEffect.Leech, power: 100, vfx: "amalanhig_grip"),
                    Atk("Grave Curse", 1, 2, weight: 2, vfx: "amalanhig_grip", grid: GridEffectType.HexTiles, gridCount: 2, gridTurns: 3),
                } } },
            { "mangkukulam", new Creature {
                displayName = "Mangkukulam", hp = 40, size = 380f,
                weak = new[] { "DOLL", "PIN", "WITCH", "CURSE", "HEX" },
                description = "A village witch who sticks pins into little dolls and makes her neighbors suffer for it. " +
                              "Nobody admits to knowing her, and everybody knows exactly where she lives.",
                attacks = new[]
                {
                    Atk("Pin Prick",  2, 3, weight: 3, crit: 0.15f, vfx: "king_curse"),
                    Atk("Voodoo Doll", 1, 2, weight: 2, effect: AttackEffect.Weaken, chance: 0.7f, turns: 2, power: 35, vfx: "manananggal_mist"),
                    Atk("Hex",        1, 1, weight: 2, vfx: "king_curse", grid: GridEffectType.HexTiles, gridCount: 2, gridTurns: 3),
                } } },
            { "wakwak", new Creature {
                displayName = "Wak-Wak", hp = 40, size = 390f, hover = 40f,
                weak = new[] { "WING", "FEATHER", "SKY", "ROOST", "BEAK" },
                description = "A great black bird whose cry, \"wak-wak\", echoes over the rooftops after midnight. " +
                              "Locals say the louder it calls, the nearer something worse is flying.",
                attacks = new[]
                {
                    Atk("Talons",       2, 4, weight: 3, crit: 0.20f, vfx: "wakwak_talons"),
                    Atk("Rake",         2, 3, weight: 2, vfx: "wakwak_rake"),
                    Atk("Screech",      1, 1, weight: 1, effect: AttackEffect.Stun, chance: 0.4f, vfx: "wakwak_screech"),
                    Atk("Feather Storm", 2, 3, weight: 2, vfx: "wakwak_feathers", hit: "wakwak_featherhit", grid: GridEffectType.SmokeTiles, gridCount: 3, gridTurns: 3),
                } } },
            { "manananggal", new Creature {
                displayName = "Manananggal", hp = 140, size = 440f, hover = 40f, mechanic = EnemyMechanic.Manananggal,
                weak = new[] { "SALT", "GARLIC", "DAWN", "SUN", "ROOF" },
                description = "By day she is a quiet neighbor. By night her body splits in two, and her wings carry her over the " +
                              "rooftops in search of a victim. Salt on the lower half keeps her from ever becoming whole again. While she flies, short words barely hurt her: spell long ones, or wait until she splits apart.",
                attacks = new[]
                {
                    Atk("Claw Swipe",  3, 4, weight: 3, crit: 0.20f, vfx: "manananggal_slash"),
                    Atk("Blood Drain", 2, 2, weight: 2, effect: AttackEffect.Leech, power: 100, vfx: "manananggal_mist"),
                    Atk("Night Wail",  2, 2, weight: 1, effect: AttackEffect.Weaken, chance: 0.4f, turns: 2, power: 30, vfx: "manananggal_mist"),

                    Atk("Midnight Feast", 3, 4, weight: 0, effect: AttackEffect.Leech, power: 100, vfx: "manananggal_mist", signature: true),
                } } },

            // Chapter 5: The Cosmic Ocean
            { "berberoka", new Creature {
                displayName = "Berberoka", hp = 45, size = 420f,
                weak = new[] { "FLOOD", "MUD", "SWAMP", "POND", "TOAD" },
                description = "A bloated giant who lives in the shallows. It drinks a whole pond, waits for fish to flop in the mud, " +
                              "then floods the land back over everyone who came to eat them.",
                attacks = new[]
                {
                    Atk("Flood Slam", 3, 4, weight: 3, crit: 0.10f, vfx: "berberoka_splash"),
                    Atk("Mud Glob",   2, 3, weight: 2, effect: AttackEffect.Poison, chance: 0.6f, turns: 2, power: 1, vfx: "berberoka_glob", travels: true, hit: "berberoka_splash"),
                    Atk("Swallow",    1, 2, weight: 2, vfx: "berberoka_splash", grid: GridEffectType.VoidTiles, gridCount: 1),
                } } },
            { "magindara", new Creature {
                displayName = "Magindara", hp = 45, size = 400f,
                weak = new[] { "SIREN", "SONG", "CORAL", "PEARL", "WAVE" },
                description = "A mermaid whose song carries across the water on quiet nights. " +
                              "Sailors who listen too long forget which way the shore is.",
                attacks = new[]
                {
                    Atk("Tail Whip",  2, 4, weight: 3, crit: 0.15f, vfx: "siyokoy_splash"),
                    Atk("Siren Song", 1, 2, weight: 2, effect: AttackEffect.Stun, chance: 0.4f, vfx: "siyokoy_geyser"),
                    Atk("Undertow",   1, 1, weight: 2, vfx: "siyokoy_wave", grid: GridEffectType.LockTiles, gridCount: 3, gridTurns: 2),
                } } },
            { "siyokoy", new Creature {
                displayName = "Siyokoy", hp = 45, size = 390f,
                weak = new[] { "TRIDENT", "SCALE", "FISH", "TIDE", "REEF" },
                description = "A merman with green scales and a hunting spear, said to drag sailors under and keep them as guests. " +
                              "He guards the road to the deep, where the moon is eaten.",
                attacks = new[]
                {
                    Atk("Trident Stab", 3, 5, weight: 3, crit: 0.20f, vfx: "siyokoy_stab"),
                    Atk("Tidal Wave",   3, 4, weight: 2, vfx: "siyokoy_wave"),
                    Atk("Geyser",       2, 3, weight: 1, effect: AttackEffect.Stun, chance: 0.4f, vfx: "siyokoy_geyser"),
                    Atk("Shell Guard",  0, 0, weight: 1, effect: AttackEffect.Guard, turns: 2, power: 50, vfx: "siyokoy_shell"),
                    Atk("Sea Mist",     1, 1, weight: 2, vfx: "siyokoy_splash", grid: GridEffectType.SmokeTiles, gridCount: 3, gridTurns: 3),
                } } },
            { "bakunawa", new Creature {
                displayName = "Bakunawa", hp = 220, size = 470f, mechanic = EnemyMechanic.Bakunawa, noSignatureCycle = true,
                weak = new[] { "MOON", "ECLIPSE", "GONG", "SERPENT", "DRAGON" },
                description = "The great sea serpent who swallows the moon. Long ago the people banged pots and gongs " +
                              "until it spat the moon back out. Tonight there are no pots, only you and the Book of Orasyon. Long words ring like gongs and push the eclipse back.",
                attacks = new[]
                {
                    Atk("Dragon Bite",  4, 6, weight: 3, crit: 0.20f, vfx: "bakunawa_wave"),
                    Atk("Moon Beam",    4, 5, weight: 2, vfx: "bakunawa_beam"),
                    Atk("Eclipse",      1, 1, weight: 1, effect: AttackEffect.Stun, chance: 0.5f, vfx: "bakunawa_moon"),
                    Atk("Moon Devour",  0, 0, weight: 1, effect: AttackEffect.Leech, power: 8, vfx: "bakunawa_moon"),
                    Atk("Total Eclipse", 6, 6, weight: 0, effect: AttackEffect.Stun, chance: 0.4f, vfx: "bakunawa_beam", signature: true),
                } } },
        };
    }
}
