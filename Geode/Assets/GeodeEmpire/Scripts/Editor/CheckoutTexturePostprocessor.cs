using UnityEditor;

namespace GeodeEmpire.EditorTools
{
    /// <summary>
    /// Import settings for the checkout kit textures. The albedo/artwork sheets are sRGB, anything ending _N is a
    /// normal map, and everything is capped at 1024 with compression on: the kit ships some 2048 sheets and this
    /// machine's budget does not want them (CLAUDE.md hardware budget).
    /// </summary>
    public sealed class CheckoutTexturePostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/GeodeEmpire/Textures/Checkout/")) return;
            var importer = (TextureImporter)assetImporter;
            bool normal = assetPath.EndsWith("_N.png");
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.mipmapEnabled = true;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.anisoLevel = 4;
        }
    }
}
