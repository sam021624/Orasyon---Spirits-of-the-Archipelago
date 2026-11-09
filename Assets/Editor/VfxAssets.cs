using System.Collections.Generic;
using System.IO;
using Orasyon.Battle;
using UnityEditor;
using UnityEngine;

namespace Orasyon.EditorTools
{
    /// <summary>
    /// Creates one VfxDefinition per sheet in Assets/Sprites/VFX (into Assets/Resources/Vfx).
    /// Frame cells are square (height x height) unless listed in <see cref="CellWidth"/>.
    /// Re-running updates the sheet reference and frame count but keeps size / fps you tuned in the Inspector.
    /// </summary>
    public static class VfxAssets
    {
        public const string OutputFolder = "Assets/Resources/Vfx/";

        // Strips whose cells are not square.
        private static readonly Dictionary<string, int> CellWidth = new Dictionary<string, int>
        {
            { "hero_kidlat_bolt", 128 },   // 1024x256: 8 tall frames
            { "bakunawa_beam",     96 },   // 1536x96: 16 frames
        };

        // name -> (on-screen height in px, fps, anchored to the ground instead of centered)
        private static readonly Dictionary<string, (float size, float fps, bool bottom)> Tuning = new Dictionary<string, (float, float, bool)>
        {
            { "hero_bolt",        (150f, 16f, false) },
            { "hero_impact",      (240f, 16f, false) },
            { "hero_flurry_hit",  (260f, 18f, false) },
            { "hero_kidlat_bolt", (620f, 14f, true)  },
            { "lola_heal",        (320f, 12f, false) },
            { "tiyanak_wail",     (520f, 12f, false) },
            { "wakwak_screech",   (360f, 12f, false) },
            { "bakunawa_beam",    (420f, 16f, false) },
            { "bakunawa_moon",    (380f, 10f, false) },
            { "bakunawa_wave",    (420f, 12f, false) },
            { "siyokoy_geyser",   (420f, 12f, true)  },
            { "siyokoy_wave",     (360f, 12f, false) },
            { "santelmo_orb",     (140f, 14f, false) },
            { "berberoka_glob",   (150f, 14f, false) },
        };

        [MenuItem("Tools/Orasyon/Create VFX Assets")]
        public static void EnsureAll()
        {
            Directory.CreateDirectory(OutputFolder.TrimEnd('/'));
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites/VFX" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png")) continue;
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null) continue;

                string name = Path.GetFileNameWithoutExtension(path);
                int cell = CellWidth.TryGetValue(name, out int w) ? w : tex.height;
                int frames = Mathf.Max(1, Mathf.RoundToInt(tex.width / (float)cell));

                string assetPath = OutputFolder + name + ".asset";
                var def = AssetDatabase.LoadAssetAtPath<VfxDefinition>(assetPath);
                bool isNew = def == null;
                if (isNew)
                {
                    def = ScriptableObject.CreateInstance<VfxDefinition>();
                    AssetDatabase.CreateAsset(def, assetPath);
                }

                def.clip = new FrameClip(tex, frames);
                if (isNew)
                {
                    def.size = 300f;
                    def.fps = 14f;
                    if (Tuning.TryGetValue(name, out var t))
                    {
                        def.size = t.size; def.fps = t.fps; def.anchorBottom = t.bottom;
                    }
                }
                EditorUtility.SetDirty(def);
                n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Orasyon] {n} VFX assets ready in {OutputFolder}");
        }

        public static VfxDefinition Load(string name) =>
            AssetDatabase.LoadAssetAtPath<VfxDefinition>(OutputFolder + name + ".asset");
    }
}
