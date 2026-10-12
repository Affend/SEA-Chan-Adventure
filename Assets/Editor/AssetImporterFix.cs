#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Menyetel kualitas import gambar di Assets/Resources:
//   - Filter Point & Mip Maps MATI  -> gambar tetap TAJAM saat bergerak (tidak blur)
//   - Tanpa kompresi               -> warna tidak pecah
//   - Khusus folder UI (artwork menu) tetap halus (bilinear + mip maps)
[InitializeOnLoad]
public static class AssetImporterFix
{
    const string DoneKey = "SEAChan.ImportFixed.v1";

    static AssetImporterFix()
    {
        EditorApplication.delayCall += RunOnce;
    }

    static void RunOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorPrefs.GetBool(DoneKey, false)) return;

        FixAll();
        EditorPrefs.SetBool(DoneKey, true);
    }

    [MenuItem("SEA Chan/5. Perbaiki Kualitas Gambar (tajam, tanpa blur)", false, 5)]
    public static void FixMenu()
    {
        FixAll();
    }

    static void FixAll()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources" });
        int n = 0;

        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;

            bool isUI = path.Replace('\\', '/').Contains("/UI/");
            bool changed = false;

            if (isUI)
            {
                // artwork menu: halus (bukan pixel art)
                if (ti.filterMode != FilterMode.Bilinear) { ti.filterMode = FilterMode.Bilinear; changed = true; }
                if (!ti.mipmapEnabled) { ti.mipmapEnabled = true; changed = true; }
            }
            else
            {
                if (ti.filterMode != FilterMode.Point) { ti.filterMode = FilterMode.Point; changed = true; }
                if (ti.mipmapEnabled) { ti.mipmapEnabled = false; changed = true; }
            }

            if (ti.wrapMode != TextureWrapMode.Clamp) { ti.wrapMode = TextureWrapMode.Clamp; changed = true; }
            if (ti.textureCompression != TextureImporterCompression.Uncompressed) { ti.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (!ti.alphaIsTransparency) { ti.alphaIsTransparency = true; changed = true; }
            if (ti.maxTextureSize < 2048) { ti.maxTextureSize = 2048; changed = true; }

            if (changed)
            {
                ti.SaveAndReimport();
                n++;
            }
        }

        Debug.Log("[SEA Chan] Kualitas gambar diperbaiki: " + n + " file disetel ulang.");
    }
}
#endif
