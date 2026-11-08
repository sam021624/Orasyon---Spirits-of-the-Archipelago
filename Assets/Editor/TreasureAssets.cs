using System;
using System.IO;
using System.Linq;
using Orasyon.Battle;
using UnityEditor;
using UnityEngine;

namespace Orasyon.EditorTools
{
    /// <summary>
    /// Creates the boss treasures (and their pixel-art icons) if they don't exist yet.
    /// Existing assets are left alone, so edits made in the Inspector are kept.
    /// </summary>
    public static class TreasureAssets
    {
        private const string Folder     = "Assets/Resources/Treasures";
        private const string IconFolder = "Assets/Sprites/Icons/Treasures";
        private const int    S = 32;

        private static readonly Color Line = new Color(0.12f, 0.07f, 0.05f, 1f);

        public static void Ensure()
        {
            Make("KalasagNgPunso", "Kalasag ng Punso", 0, bonusHearts: 1, damagePercent: 0,
                 "A round shield carved from the Duwende King's own mound. The old wood still hums with earth magic, and blows land softer against it.",
                 IconFolder + "/shield.png", Shield);

            Make("PanaNgBalete", "Pana ng Balete", 1, bonusHearts: 0, damagePercent: 20,
                 "A bow strung from a root of the oldest balete. The Kapre let you take it, which is the closest he comes to a compliment. Your words strike harder.",
                 IconFolder + "/bow.png", Bow);

            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Orasyon/Reset Collected Treasures")]
        public static void ResetCollected()
        {
            PlayerProgress.ResetAll();
            Debug.Log("[Orasyon] Collected treasures cleared.");
        }

        private static void Make(string file, string displayName, int slot, int bonusHearts, int damagePercent,
                                 string description, string iconPath, Func<int, int, Color> draw)
        {
            Directory.CreateDirectory(Folder);
            string path = $"{Folder}/{file}.asset";
            if (AssetDatabase.LoadAssetAtPath<TreasureDefinition>(path) != null) return;

            var t = ScriptableObject.CreateInstance<TreasureDefinition>();
            t.id = file;
            t.displayName = displayName;
            t.description = description;
            t.slotIndex = slot;
            t.bonusHearts = bonusHearts;
            t.damageBonusPercent = damagePercent;
            t.icon = Icon(iconPath, draw);
            AssetDatabase.CreateAsset(t, path);
        }

        // ── Pixel art ────────────────────────────────────────────────────────
        private static Color Shield(int x, int y)
        {
            float dx = x - 15.5f, dy = y - 15.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d > 13.6f) return Color.clear;
            if (d > 11.6f) return y > 18 ? new Color(1f, 0.86f, 0.42f) : new Color(0.86f, 0.64f, 0.22f);   // gold rim
            if (Mathf.Abs(dx) + Mathf.Abs(dy) < 4.6f) return new Color(0.80f, 0.18f, 0.16f);              // red boss
            if (Mathf.Abs(dx) + Mathf.Abs(dy) < 6.2f) return new Color(0.92f, 0.74f, 0.30f);              // gold ring
            if ((x + 2) % 6 == 0) return new Color(0.36f, 0.22f, 0.11f);                                  // plank seams
            return dx + dy > 4 ? new Color(0.48f, 0.30f, 0.15f) : new Color(0.60f, 0.40f, 0.21f);          // wood
        }

        private static Color Bow(int x, int y)
        {
            // Limb: an arc bulging to the left
            float dx = x - 30f, dy = y - 15.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d >= 15.5f && d <= 17.8f && x <= 21 && y >= 1 && y <= 30)
                return d < 16.6f ? new Color(0.62f, 0.38f, 0.17f) : new Color(0.45f, 0.26f, 0.11f);
            // Grip wrap
            if (x >= 12 && x <= 15 && y >= 13 && y <= 18) return new Color(0.30f, 0.62f, 0.32f);
            // String
            if (x == 21 && y >= 2 && y <= 29) return new Color(0.93f, 0.90f, 0.82f);
            // Arrow
            if (y == 15 && x >= 9 && x <= 27) return new Color(0.80f, 0.68f, 0.48f);
            if (x >= 5 && x <= 8 && Mathf.Abs(y - 15) <= 8 - x) return new Color(0.75f, 0.78f, 0.84f);    // head
            if (x >= 25 && x <= 28 && (y == 16 + (x - 24) / 2 || y == 14 - (x - 24) / 2)) return new Color(0.85f, 0.25f, 0.20f); // fletching
            return Color.clear;
        }

        private static Sprite Icon(string path, Func<int, int, Color> draw)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var px = new Color[S, S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                    px[x, y] = draw(x, y);

            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    Color c = px[x, y];
                    if (c.a == 0f && HasNeighbour(px, x, y)) c = Line;   // dark outline like the other icons
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.filterMode = FilterMode.Point;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
        }

        private static bool HasNeighbour(Color[,] px, int x, int y)
        {
            for (int oy = -1; oy <= 1; oy++)
                for (int ox = -1; ox <= 1; ox++)
                {
                    if ((ox == 0) == (oy == 0)) continue;   // 4-neighbours only
                    int nx = x + ox, ny = y + oy;
                    if (nx >= 0 && ny >= 0 && nx < S && ny < S && px[nx, ny].a > 0f) return true;
                }
            return false;
        }
    }
}
