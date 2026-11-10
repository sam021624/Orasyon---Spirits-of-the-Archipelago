using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Orasyon.EditorTools
{
    /// <summary>
    /// Draws the small pixel icons used for enemy moves (damage, crit and the five status effects) into
    /// Assets/Resources/MoveIcons. Existing icons are left alone, so you can replace them with your own art
    /// (keep the file names) and they will not be overwritten.
    /// </summary>
    public static class MoveIconAssets
    {
        private const string Folder = "Assets/Resources/MoveIcons";
        private const int S = 32;
        private static readonly Color Line = new Color(0.10f, 0.06f, 0.05f, 1f);

        [MenuItem("Tools/Orasyon/Create Move Icons")]
        public static void Ensure()
        {
            Directory.CreateDirectory(Folder);
            Make("damage", Claws);
            Make("crit",   Burst);
            Make("poison", Droplet);
            Make("stun",   Sparkle);
            Make("weaken", ArrowDown);
            Make("guard",  Shield);
            Make("leech",  Heart);
            Make("steal",  Sack);
            Make("smoke",  Cloud);
            Make("lock",   Padlock);
            Make("burn",   Flame);
            Make("hex",    Rune);
            Make("scramble", Swirl);
            Make("void",   BlackHole);
            AssetDatabase.SaveAssets();
        }

        private static void Make(string name, Func<int, int, Color> draw)
        {
            string path = $"{Folder}/{name}.png";
            if (File.Exists(path)) return;

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
        }

        private static bool HasNeighbour(Color[,] px, int x, int y)
        {
            for (int oy = -1; oy <= 1; oy++)
                for (int ox = -1; ox <= 1; ox++)
                {
                    if ((ox == 0) == (oy == 0)) continue;
                    int nx = x + ox, ny = y + oy;
                    if (nx >= 0 && ny >= 0 && nx < S && ny < S && px[nx, ny].a > 0f) return true;
                }
            return false;
        }

        // ── Shapes (y = 0 is the bottom) ─────────────────────────────────────
        private static Color Claws(int x, int y)
        {
            float t = x - y;
            for (int k = -1; k <= 1; k++)
            {
                float d = (x + y) - 31f - k * 9f;
                if (Mathf.Abs(d) <= 2.4f * (1f - Mathf.Abs(t) / 26f))
                    return d > 0 ? new Color(0.82f, 0.18f, 0.16f) : new Color(1f, 0.50f, 0.38f);
            }
            return Color.clear;
        }

        private static Color Burst(int x, int y)
        {
            float dx = x - 15.5f, dy = y - 15.5f, r = Mathf.Sqrt(dx * dx + dy * dy);
            float theta = Mathf.Atan2(dy, dx);
            float edge = 6.5f + 8.5f * Mathf.Pow(Mathf.Abs(Mathf.Cos(4f * theta)), 3f);
            if (r <= 4.5f) return new Color(1f, 0.92f, 0.45f);
            if (r <= edge) return new Color(1f, 0.52f, 0.22f);
            return Color.clear;
        }

        private static Color Droplet(int x, int y)
        {
            float dx = x - 15.5f;
            bool inside;
            if (y >= 11) inside = Mathf.Abs(dx) <= Mathf.Max(0f, (29f - y) / 18f * 10f);   // narrows to the tip at the top
            else { float dy = y - 11f; inside = dx * dx + dy * dy <= 100f; }                 // round belly
            if (!inside) return Color.clear;
            return dx < -2f && dx > -6f && y > 5 && y < 15 ? new Color(0.78f, 1f, 0.68f) : new Color(0.36f, 0.78f, 0.30f);
        }

        private static Color Sparkle(int x, int y)
        {
            float dx = Mathf.Abs(x - 15.5f), dy = Mathf.Abs(y - 15.5f);
            const float R = 14f;
            if (Mathf.Pow(dx, 0.6667f) + Mathf.Pow(dy, 0.6667f) > Mathf.Pow(R, 0.6667f)) return Color.clear;
            return dx + dy < 4f ? new Color(1f, 1f, 0.9f) : new Color(1f, 0.85f, 0.25f);
        }

        private static Color ArrowDown(int x, int y)
        {
            float dx = Mathf.Abs(x - 15.5f);
            bool shaft = dx <= 3.5f && y >= 14 && y <= 29;
            bool head = y >= 2 && y < 14 && dx <= (y - 2) / 12f * 11f;
            if (!shaft && !head) return Color.clear;
            return x < 15 ? new Color(0.80f, 0.64f, 1f) : new Color(0.58f, 0.40f, 0.86f);
        }

        private static Color Shield(int x, int y)
        {
            float dx = Mathf.Abs(x - 15.5f);
            float half = y >= 15 ? 11.5f : Mathf.Max(0f, 11.5f * (y - 2f) / 13f);
            if (y < 2 || y > 29 || dx > half) return Color.clear;
            if (dx < 2f && y > 8 && y < 24) return new Color(1f, 0.90f, 0.50f);              // gold stripe
            return x < 15 ? new Color(0.50f, 0.74f, 1f) : new Color(0.28f, 0.50f, 0.90f);
        }

        private static Color Sack(int x, int y)
        {
            float dx = x - 15.5f;
            // body: a round bag, neck pinched at y ~ 21, ruffle on top
            float cy = 12f, rr = 10f;
            if (dx * dx + (y - cy) * (y - cy) <= rr * rr) return dx < -3f && y > 8 && y < 18 ? new Color(0.78f, 0.58f, 0.34f) : new Color(0.58f, 0.40f, 0.22f);
            if (y >= 21 && y <= 24 && Mathf.Abs(dx) <= 3.2f) return new Color(0.58f, 0.40f, 0.22f);              // neck
            if (y >= 25 && y <= 28 && Mathf.Abs(dx) <= 6f - (y - 25) * 0.4f) return new Color(0.70f, 0.50f, 0.28f); // ruffle
            if (y >= 20 && y <= 21 && Mathf.Abs(dx) <= 4.5f) return new Color(0.95f, 0.78f, 0.30f);              // gold tie
            return Color.clear;
        }

        private static Color Cloud(int x, int y)
        {
            bool Disc(float cx, float cy, float r) => (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;
            bool inside = Disc(10f, 14f, 7f) || Disc(18f, 17f, 9f) || Disc(24f, 13f, 6f) || (y >= 7 && y <= 14 && x >= 8 && x <= 26);
            if (!inside) return Color.clear;
            return y > 17 ? new Color(0.86f, 0.86f, 0.90f) : y > 11 ? new Color(0.68f, 0.68f, 0.74f) : new Color(0.52f, 0.52f, 0.60f);
        }

        private static Color Padlock(int x, int y)
        {
            float dx = x - 15.5f;
            if (x >= 6 && x <= 25 && y >= 3 && y <= 17) return (Mathf.Abs(dx) < 1.6f && y >= 8 && y <= 13) ? new Color(0.20f, 0.12f, 0.06f) : (x < 15 ? new Color(0.95f, 0.76f, 0.30f) : new Color(0.78f, 0.58f, 0.18f));
            // shackle: a ring above the body
            float cy = 17f, r = Mathf.Sqrt(dx * dx + (y - cy) * (y - cy));
            if (y >= 17 && r >= 5.2f && r <= 8f) return new Color(0.80f, 0.82f, 0.88f);
            return Color.clear;
        }

        private static Color Flame(int x, int y)
        {
            float dx = x - 15.5f;
            float t = (y - 3f) / 26f;                        // 0 bottom, 1 tip
            if (t < 0f || t > 1f) return Color.clear;
            float half = 9.5f * Mathf.Sin(Mathf.Clamp01(t * 1.15f) * Mathf.PI * 0.62f) * (1f - t * 0.55f);
            half += 1.2f * Mathf.Sin(t * 18f) * t;           // flickering edge
            if (Mathf.Abs(dx) > half) return Color.clear;
            if (Mathf.Abs(dx) < half * 0.45f && t < 0.62f) return new Color(1f, 0.92f, 0.45f);
            return t > 0.55f ? new Color(1f, 0.55f, 0.18f) : new Color(0.95f, 0.32f, 0.12f);
        }

        private static Color Rune(int x, int y)
        {
            float dx = Mathf.Abs(x - 15.5f), dy = Mathf.Abs(y - 15.5f);
            bool hexagon = dx * 0.866f + dy * 0.5f <= 12f && dy <= 12f;
            if (!hexagon) return Color.clear;
            bool mark = (Mathf.Abs((x - 15.5f) - (y - 15.5f)) <= 1.6f && dx < 8f) || (Mathf.Abs((x - 15.5f) + (y - 15.5f)) <= 1.6f && dx < 8f);
            if (mark) return new Color(1f, 0.90f, 1f);
            return x < 15 ? new Color(0.62f, 0.38f, 0.88f) : new Color(0.44f, 0.24f, 0.70f);
        }

        private static Color Swirl(int x, int y)
        {
            float dx = x - 15.5f, dy = y - 15.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float theta = Mathf.Atan2(dy, dx);
            for (int k = 0; k < 3; k++)
            {
                float a = theta + k * Mathf.PI * 2f;
                if (a < 0f || a > 12.5f) continue;
                float target = 2.5f + 1.05f * a;
                if (Mathf.Abs(r - target) <= 1.5f) return a > 8f ? new Color(0.62f, 0.95f, 0.92f) : new Color(0.30f, 0.76f, 0.80f);
            }
            return Color.clear;
        }

        private static Color BlackHole(int x, int y)
        {
            float dx = x - 15.5f, dy = y - 15.5f, r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r <= 6.5f) return new Color(0.03f, 0.01f, 0.06f);
            if (r <= 10f)  return new Color(0.08f, 0.04f, 0.14f);
            if (r <= 13f)  return (Mathf.Atan2(dy, dx) > 0f) ? new Color(0.62f, 0.38f, 0.92f) : new Color(0.42f, 0.22f, 0.72f);
            return Color.clear;
        }

        private static Color Heart(int x, int y)
        {
            float u = (x - 15.5f) / 12f, v = (y - 15f) / 12f;
            float a = u * u + v * v - 1f;
            if (a * a * a - u * u * v * v * v > 0f) return Color.clear;
            return u < -0.25f && v > 0.1f && v < 0.6f ? new Color(1f, 0.78f, 0.84f) : new Color(0.92f, 0.28f, 0.42f);
        }
    }
}
