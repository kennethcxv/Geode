using UnityEditor;

namespace GeodeEmpire.EditorTools
{
    /// <summary>Import settings for Blender-generated FBX under Assets/GeodeEmpire/Models.</summary>
    public sealed class GeodeModelPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.Contains("/GeodeEmpire/Models/")) return;
            var importer = (ModelImporter)assetImporter;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.importNormals = ModelImporterNormals.Import;
            importer.useFileScale = true;
            importer.globalScale = 1f;
            importer.bakeAxisConversion = true;
            bool crystals = assetPath.Contains("/Models/Crystals/");
            // the Golf checkout kit carries its anchors and sockets as empties; they must survive the import, and the
            // meshes stay readable so the kit builder can measure them when it authors colliders and layouts
            bool checkoutKit = assetPath.Contains("/Models/Checkout/");
            if (checkoutKit)
            {
                importer.preserveHierarchy = true;
                // the kit round-trips glTF -> Blender -> FBX, which already lands in Unity's axis convention; baking a
                // second conversion mirrors it about Z (the POS screen ends up facing the customer)
                importer.bakeAxisConversion = false;
            }
            importer.isReadable = crystals || checkoutKit || assetPath.Contains("/Models/Props/");
            importer.importTangents = crystals ? ModelImporterTangents.None : ModelImporterTangents.CalculateMikk;
            importer.generateSecondaryUV = false;
            importer.addCollider = false;
        }
    }
}
