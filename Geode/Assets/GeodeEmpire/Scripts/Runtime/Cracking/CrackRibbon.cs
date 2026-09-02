using UnityEngine;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Cracking
{
    /// <summary>
    /// Visible fracture lines: a thin strip hugging the seam, one segment per stress sector. Vertex alpha
    /// shows faint stress and solid cracks; also stamps small impact marks where the chisel hit.
    /// </summary>
    public sealed class CrackRibbon : MonoBehaviour
    {
        private Mesh _mesh;
        private Color[] _colors;
        private int _segsPerSector;
        private MeshRenderer _renderer;
        private GameObject _marksGo;
        private Mesh _marks;
        private int _markCount;
        private const int MaxMarks = 32;
        private Vector3[] _markVerts = new Vector3[MaxMarks * 4];
        private Color[] _markCols = new Color[MaxMarks * 4];
        private Vector2[] _markUvs = new Vector2[MaxMarks * 4];
        private int[] _markTris = new int[MaxMarks * 6];

        public static CrackRibbon Attach(SpecimenEntity e, Material mat)
        {
            var go = new GameObject("CrackRibbon");
            go.transform.SetParent(e.Visual.BottomHalf, false);
            var r = go.AddComponent<CrackRibbon>();
            r.Build(e.Visual.Geometry, mat);
            return r;
        }

        private void Build(GeodeGeometry geo, Material mat)
        {
            int N = geo.Longitudes;
            _segsPerSector = Mathf.Max(1, N / StressModel.Sectors);
            int total = StressModel.Sectors * _segsPerSector;
            var verts = new Vector3[(total + 1) * 2];
            var uvs = new Vector2[verts.Length];
            _colors = new Color[verts.Length];
            var tris = new int[total * 6];
            float width = geo.MeanEquatorRadius * 0.11f;
            for (int i = 0; i <= total; i++)
            {
                int li = i % N;
                float lon = i / (float)total * Mathf.PI * 2f;
                float r = geo.Bottom.EquatorOuterRadius[li] + 0.0015f;
                float y = geo.Bottom.EquatorY[li];
                var dir = new Vector3(Mathf.Cos(lon), 0f, Mathf.Sin(lon));
                float jag = Mathf.Sin(lon * 23f) * width * 0.35f + Mathf.Sin(lon * 41f + 1.3f) * width * 0.2f;
                verts[i * 2] = dir * r + Vector3.up * (y + width + jag);
                verts[i * 2 + 1] = dir * r + Vector3.up * (y - width + jag);
                uvs[i * 2] = new Vector2(i / (float)total * 8f, 0f);
                uvs[i * 2 + 1] = new Vector2(i / (float)total * 8f, 1f);
                _colors[i * 2] = _colors[i * 2 + 1] = new Color(1f, 1f, 1f, 0f);
            }
            for (int i = 0; i < total; i++)
            {
                int a = i * 2, b = i * 2 + 1, c = (i + 1) * 2, d = (i + 1) * 2 + 1;
                tris[i * 6] = a; tris[i * 6 + 1] = c; tris[i * 6 + 2] = b;
                tris[i * 6 + 3] = b; tris[i * 6 + 4] = c; tris[i * 6 + 5] = d;
            }
            _mesh = new Mesh { name = "CrackRibbon" };
            _mesh.vertices = verts; _mesh.uv = uvs; _mesh.colors = _colors; _mesh.triangles = tris;
            _mesh.RecalculateBounds();
            var mf = gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = _mesh;
            _renderer = gameObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = mat;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;

            _marksGo = new GameObject("ImpactMarks");
            _marksGo.transform.SetParent(transform, false);
            _marks = new Mesh { name = "ImpactMarks" };
            var mmf = _marksGo.AddComponent<MeshFilter>();
            mmf.sharedMesh = _marks;
            var mmr = _marksGo.AddComponent<MeshRenderer>();
            mmr.sharedMaterial = mat;
            mmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mmr.receiveShadows = false;
            for (int i = 0; i < MaxMarks; i++)
            {
                _markTris[i * 6] = i * 4; _markTris[i * 6 + 1] = i * 4 + 2; _markTris[i * 6 + 2] = i * 4 + 1;
                _markTris[i * 6 + 3] = i * 4 + 1; _markTris[i * 6 + 4] = i * 4 + 2; _markTris[i * 6 + 5] = i * 4 + 3;
            }
        }

        /// <summary>Update per-sector alpha from the stress model.</summary>
        public void Refresh(StressModel model, bool showStress)
        {
            int total = StressModel.Sectors * _segsPerSector;
            for (int i = 0; i <= total; i++)
            {
                int sector = Mathf.Min(StressModel.Sectors - 1, i / _segsPerSector);
                float s = model.Stress[sector];
                float a;
                if (s >= 1f) a = 1f;
                else a = 0.16f + (showStress ? Mathf.Clamp01(s) * 0.5f : Mathf.Clamp01(s - 0.55f) * 0.9f);   // faint seam guide, brighter with stress
                // taper inside a sector so cracks look like segments, not a ring
                float inSector = (i % _segsPerSector) / (float)_segsPerSector;
                float taper = s >= 1f ? 1f : Mathf.Sin(inSector * Mathf.PI) * 0.6f + 0.4f;
                var c = new Color(1f, 1f, 1f, a * taper);
                _colors[i * 2] = c;
                _colors[i * 2 + 1] = c;
            }
            _mesh.colors = _colors;
        }

        /// <summary>Stamp a small dark mark at a rock-local point/normal (bottom-half local space).</summary>
        public void AddImpactMark(Vector3 localPoint, Vector3 localNormal, float size, float strength)
        {
            int i = _markCount % MaxMarks;
            _markCount++;
            Vector3 n = localNormal.normalized;
            Vector3 t = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            Vector3 b = Vector3.Cross(n, t);
            Vector3 p = localPoint + n * 0.0015f;
            _markVerts[i * 4] = p + (-t - b) * size;
            _markVerts[i * 4 + 1] = p + (t - b) * size;
            _markVerts[i * 4 + 2] = p + (-t + b) * size;
            _markVerts[i * 4 + 3] = p + (t + b) * size;
            var c = new Color(1f, 1f, 1f, Mathf.Clamp01(strength));
            _markCols[i * 4] = _markCols[i * 4 + 1] = _markCols[i * 4 + 2] = _markCols[i * 4 + 3] = c;
            _markUvs[i * 4] = new Vector2(0f, 0f); _markUvs[i * 4 + 1] = new Vector2(1f, 0f);
            _markUvs[i * 4 + 2] = new Vector2(0f, 1f); _markUvs[i * 4 + 3] = new Vector2(1f, 1f);
            _marks.Clear();
            _marks.vertices = _markVerts; _marks.colors = _markCols; _marks.uv = _markUvs;
            _marks.triangles = _markTris;
            _marks.RecalculateBounds();
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
            if (_marks != null) Destroy(_marks);
        }
    }
}
