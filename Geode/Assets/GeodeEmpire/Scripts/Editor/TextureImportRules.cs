using UnityEditor;
using UnityEngine;

namespace GeodeEmpire.EditorTools
{
    /// <summary>
    /// Import rules for the generated PBR texture sets (Tools/Blender/gen_textures.py -> Assets/GeodeEmpire/Textures/Generated):
    /// *_normal is a tangent-space normal map, *_mask / *_height are linear data, everything stays at or under 1024 px
    /// with mipmaps and default compression (the M2 / 8 GB budget). Albedo maps stay sRGB.
    /// </summary>
    public sealed class TextureImportRules : AssetPostprocessor
    {
        private const string Folder = "Assets/GeodeEmpire/Textures/Generated/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var importer = (TextureImporter)assetImporter;
            string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            importer.maxTextureSize = 1024;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.textureCompression = TextureImporterCompression.Compressed;
            if (name.EndsWith("_normal"))
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.sRGBTexture = false;
            }
            else if (name.EndsWith("_mask") || name.EndsWith("_height"))
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = false;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = false;
            }
            else
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.alphaSource = TextureImporterAlphaSource.None;
            }
        }
    }
}
