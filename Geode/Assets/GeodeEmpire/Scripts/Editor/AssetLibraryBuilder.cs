using System.IO;
using UnityEditor;
using UnityEngine;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.EditorTools
{
    /// <summary>Creates/updates materials and the SpecimenAssetLibrary from generated content.</summary>
    public static class AssetLibraryBuilder
    {
        public const string MaterialFolder = "Assets/GeodeEmpire/Materials";
        public const string LibraryPath = "Assets/GeodeEmpire/Resources/SpecimenAssetLibrary.asset";
        public const string CrystalModelFolder = "Assets/GeodeEmpire/Models/Crystals";

        public static readonly string[] ArchetypeFiles =
        {
            "crystal_quartz_point", "crystal_quartz_stubby", "crystal_quartz_cluster", "crystal_cube", "crystal_octahedron",
            "crystal_rhomb", "crystal_dogtooth", "crystal_nailhead", "crystal_blade", "crystal_needle", "crystal_pyritohedron",
            "crystal_druzy_tile", "crystal_botryoidal", "crystal_aragonite_spray",
        };

        [MenuItem("GeodeEmpire/Assets/Build Specimen Assets")]
        public static SpecimenAssetLibrary Build()
        {
            ProceduralTextureFactory.EnsureCoreTextures();
            Directory.CreateDirectory(MaterialFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(LibraryPath));

            var noise = AssetDatabase.LoadAssetAtPath<Texture2D>(ProceduralTextureFactory.TextureFolder + "/T_Noise.png");
            var rock = AssetDatabase.LoadAssetAtPath<Texture2D>(ProceduralTextureFactory.TextureFolder + "/T_Rock.png");

            var crystalMat = LoadOrCreateMaterial("M_Crystal", "GeodeEmpire/Crystal");
            crystalMat.SetTexture("_NoiseTex", noise);
            var shellMat = LoadOrCreateMaterial("M_GeodeShell", "GeodeEmpire/GeodeShell");
            shellMat.SetTexture("_NoiseTex", noise);
            shellMat.SetTexture("_RockTex", rock);
            var crackMat = LoadOrCreateMaterial("M_CrackLine", "Universal Render Pipeline/Unlit");
            crackMat.SetColor("_BaseColor", new Color(0.05f, 0.04f, 0.03f, 1f));
            EditorUtility.SetDirty(crystalMat);
            EditorUtility.SetDirty(shellMat);
            EditorUtility.SetDirty(crackMat);

            var lib = AssetDatabase.LoadAssetAtPath<SpecimenAssetLibrary>(LibraryPath);
            if (lib == null)
            {
                lib = ScriptableObject.CreateInstance<SpecimenAssetLibrary>();
                AssetDatabase.CreateAsset(lib, LibraryPath);
            }
            lib.CrystalMaterial = crystalMat;
            lib.ShellMaterial = shellMat;
            lib.CrackMaterial = crackMat;
            lib.NoiseTexture = noise;
            lib.RockTexture = rock;
            if (lib.CrystalMeshes == null || lib.CrystalMeshes.Length != ArchetypeFiles.Length) lib.CrystalMeshes = new Mesh[ArchetypeFiles.Length];
            int found = 0;
            for (int i = 0; i < ArchetypeFiles.Length; i++)
            {
                string path = CrystalModelFolder + "/" + ArchetypeFiles[i] + ".fbx";
                Mesh mesh = null;
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (o is Mesh m) { mesh = m; break; }
                lib.CrystalMeshes[i] = mesh;
                if (mesh != null) found++;
                else Debug.LogWarning($"[AssetLibraryBuilder] Missing crystal mesh: {path}");
            }
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log($"[AssetLibraryBuilder] Library built: {found}/{ArchetypeFiles.Length} crystal meshes, materials at {MaterialFolder}");
            return lib;
        }

        public static Material LoadOrCreateMaterial(string name, string shaderName)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new System.Exception("Shader not found: " + shaderName);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }
            return mat;
        }
    }
}
