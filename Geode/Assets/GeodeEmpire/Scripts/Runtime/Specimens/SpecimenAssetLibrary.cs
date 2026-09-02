using System;
using UnityEngine;

namespace GeodeEmpire.Specimens
{
    /// <summary>
    /// Asset references the runtime specimen assembler needs (Blender crystal meshes, shaders/materials,
    /// procedural textures). One instance lives in Resources/ and is built by the editor tool
    /// GeodeEmpire > Build Specimen Assets.
    /// </summary>
    [CreateAssetMenu(menuName = "Geode Empire/Specimen Asset Library")]
    public sealed class SpecimenAssetLibrary : ScriptableObject
    {
        public const string ResourcePath = "SpecimenAssetLibrary";

        public Mesh[] CrystalMeshes = new Mesh[14];
        public Material CrystalMaterial;
        public Material ShellMaterial;
        public Material CrackMaterial;
        public Material HighlightMaterial;
        public Texture2D NoiseTexture;
        public Texture2D RockTexture;

        public sealed class MeshData
        {
            public Vector3[] Vertices;
            public Vector3[] Normals;
            public int[] Triangles;
            public float Height;
        }

        [NonSerialized] private MeshData[] _cache;

        private static SpecimenAssetLibrary _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _instance = null; }

        public static SpecimenAssetLibrary Load()
        {
            if (_instance == null) _instance = Resources.Load<SpecimenAssetLibrary>(ResourcePath);
            return _instance;
        }

        public Mesh GetMesh(CrystalArchetype a)
        {
            int i = (int)a;
            return i >= 0 && i < CrystalMeshes.Length ? CrystalMeshes[i] : null;
        }

        public MeshData GetMeshData(CrystalArchetype a)
        {
            int i = (int)a;
            if (_cache == null || _cache.Length != CrystalMeshes.Length) _cache = new MeshData[CrystalMeshes.Length];
            if (i < 0 || i >= _cache.Length) return null;
            if (_cache[i] == null)
            {
                var m = CrystalMeshes[i];
                if (m == null) return null;
                var verts = m.vertices;
                float h = 0f;
                foreach (var v in verts) h = Mathf.Max(h, v.y);
                _cache[i] = new MeshData { Vertices = verts, Normals = m.normals, Triangles = m.triangles, Height = Mathf.Max(0.01f, h) };
            }
            return _cache[i];
        }
    }
}
