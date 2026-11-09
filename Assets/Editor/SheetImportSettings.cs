using UnityEditor;
using UnityEngine;

namespace Orasyon.EditorTools
{
    /// <summary>
    /// Import settings for the animation strips in Assets/Sprites/Characters-New and Assets/Sprites/VFX.
    /// They are plain textures (one strip per clip); the game shows one frame at a time by moving a UV rectangle,
    /// so no sprite slicing is needed. Wide strips must not be downscaled, hence the large max size.
    /// </summary>
    public sealed class SheetImportSettings : AssetPostprocessor
    {
        public const string CharactersRoot = "Assets/Sprites/Characters-New/";
        public const string VfxRoot        = "Assets/Sprites/VFX/";

        public static bool IsSheet(string path) =>
            path.StartsWith(CharactersRoot, System.StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(VfxRoot, System.StringComparison.OrdinalIgnoreCase);

        private void OnPreprocessTexture()
        {
            if (!IsSheet(assetPath)) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType         = TextureImporterType.Default;
            ti.textureShape        = TextureImporterShape.Texture2D;
            ti.mipmapEnabled       = false;
            ti.filterMode          = FilterMode.Bilinear;
            ti.wrapMode            = TextureWrapMode.Clamp;
            ti.alphaSource         = TextureImporterAlphaSource.FromInput;
            ti.alphaIsTransparency = true;
            ti.npotScale           = TextureImporterNPOTScale.None;
            ti.maxTextureSize      = 8192;
            ti.textureCompression  = TextureImporterCompression.CompressedHQ;
            ti.isReadable          = false;
        }

        [MenuItem("Tools/Orasyon/Reimport New Art Sheets")]
        public static void ReimportAll()
        {
            int n = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites/Characters-New", "Assets/Sprites/VFX" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".png")) continue;
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    n++;
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.Refresh();
            Debug.Log($"[Orasyon] Reimported {n} animation sheets.");
        }
    }
}
