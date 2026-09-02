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
            importer.isReadable = crystals || assetPath.Contains("/Models/Props/");
            importer.importTangents = crystals ? ModelImporterTangents.None : ModelImporterTangents.CalculateMikk;
            importer.generateSecondaryUV = false;
            importer.addCollider = false;
        }
    }
}
