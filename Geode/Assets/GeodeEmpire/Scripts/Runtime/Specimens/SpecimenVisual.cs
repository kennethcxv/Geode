using System.Collections.Generic;
using UnityEngine;

namespace GeodeEmpire.Specimens
{
    /// <summary>
    /// Assembles the visible specimen: two shell halves and combined crystal meshes, from geology + condition.
    /// Rebuilding crystals after damage is cheap (single combined mesh per half/material).
    /// </summary>
    public sealed class SpecimenVisual : MonoBehaviour
    {
        public SpecimenGeology Geology { get; private set; }
        public GeodeGeometry Geometry { get; private set; }
        public SpecimenCondition Condition { get; private set; }
        public Transform TopHalf { get; private set; }
        public Transform BottomHalf { get; private set; }
        public MeshRenderer TopShellRenderer { get; private set; }
        public MeshRenderer BottomShellRenderer { get; private set; }

        private SpecimenAssetLibrary _lib;
        private readonly List<MeshRenderer> _crystalRenderers = new List<MeshRenderer>();
        private readonly List<Mesh> _ownedMeshes = new List<Mesh>();
        private MaterialPropertyBlock _mpb;
        private float _highlight;

        private static readonly int RockColorId = Shader.PropertyToID("_RockColor");
        private static readonly int RockColor2Id = Shader.PropertyToID("_RockColor2");
        private static readonly int CavityColorId = Shader.PropertyToID("_CavityColor");
        private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
        private static readonly int BandAId = Shader.PropertyToID("_BandA");
        private static readonly int BandBId = Shader.PropertyToID("_BandB");
        private static readonly int BandStrengthId = Shader.PropertyToID("_BandStrength");
        private static readonly int BandFrequencyId = Shader.PropertyToID("_BandFrequency");
        private static readonly int BandOffsetId = Shader.PropertyToID("_BandOffset");
        private static readonly int HintColorId = Shader.PropertyToID("_HintColor");
        private static readonly int HintAmountId = Shader.PropertyToID("_HintAmount");
        private static readonly int WeatheringId = Shader.PropertyToID("_Weathering");
        private static readonly int CavitySmoothId = Shader.PropertyToID("_CavitySmoothness");
        private static readonly int CavityDruzyId = Shader.PropertyToID("_CavityDruzy");
        private static readonly int CavityCrystalColorId = Shader.PropertyToID("_CavityCrystalColor");
        private static readonly int HighlightId = Shader.PropertyToID("_Highlight");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int DeepColorId = Shader.PropertyToID("_DeepColor");
        private static readonly int ZoneColorId = Shader.PropertyToID("_ZoneColor");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        private static readonly int TranslucencyId = Shader.PropertyToID("_Translucency");
        private static readonly int RimStrengthId = Shader.PropertyToID("_RimStrength");
        private static readonly int SparkleId = Shader.PropertyToID("_Sparkle");
        private static readonly int ZoningId = Shader.PropertyToID("_ZoningStrength");
        private static readonly int InclusionsId = Shader.PropertyToID("_Inclusions");

        public static readonly Color[] MatrixTones =
        {
            new Color(0.5f, 0.45f, 0.4f), // warm brown-grey
            new Color(0.46f, 0.46f, 0.45f), // neutral grey
            new Color(0.56f, 0.48f, 0.38f), // tan
            new Color(0.34f, 0.33f, 0.32f), // dark basalt
        };

        public void Build(SpecimenGeology geology, SpecimenCondition condition, SpecimenAssetLibrary lib)
        {
            Clear();
            Geology = geology;
            Condition = condition ?? new SpecimenCondition();
            _lib = lib;
            Geometry = GeodeMeshBuilder.Build(geology);
            Condition.EnsureSize(Geometry.Crystals.Count);
            _mpb ??= new MaterialPropertyBlock();

            BottomHalf = CreateHalf("BottomHalf", Geometry.Bottom, out var bottomRenderer);
            TopHalf = CreateHalf("TopHalf", Geometry.Top, out var topRenderer);
            BottomShellRenderer = bottomRenderer;
            TopShellRenderer = topRenderer;
            RebuildCrystals();
            ApplyShellProperties();
            SetCrystalsVisible(Condition.Opened);
        }

        /// <summary>Crystals are hidden while the rock is closed (nothing inside is visible anyway).</summary>
        public void SetCrystalsVisible(bool visible)
        {
            foreach (var r in _crystalRenderers) if (r != null) r.enabled = visible;
        }

        public void Clear()
        {
            foreach (var m in _ownedMeshes) if (m != null) DestroyObj(m);
            _ownedMeshes.Clear();
            _crystalRenderers.Clear();
            for (int i = transform.childCount - 1; i >= 0; i--) DestroyObj(transform.GetChild(i).gameObject);
            TopHalf = BottomHalf = null;
        }

        private static void DestroyObj(Object o)
        {
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        private Transform CreateHalf(string name, GeodeHalfGeometry half, out MeshRenderer renderer)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var mesh = half.ToMesh(name + "_Shell");
            _ownedMeshes.Add(mesh);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _lib.ShellMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go.transform;
        }

        /// <summary>Rebuild the combined crystal meshes (call after damage changes).</summary>
        public void RebuildCrystals()
        {
            foreach (var r in _crystalRenderers) if (r != null) DestroyObj(r.gameObject);
            _crystalRenderers.Clear();
            for (int i = _ownedMeshes.Count - 1; i >= 0; i--)
                if (_ownedMeshes[i] != null && _ownedMeshes[i].name.Contains("Crystals")) { DestroyObj(_ownedMeshes[i]); _ownedMeshes.RemoveAt(i); }

            CreateCrystalObject(BottomHalf, false, false);
            CreateCrystalObject(BottomHalf, false, true);
            CreateCrystalObject(TopHalf, true, false);
            CreateCrystalObject(TopHalf, true, true);
            ApplyCrystalProperties();
        }

        private void CreateCrystalObject(Transform parent, bool top, bool secondary)
        {
            var mesh = CombineCrystals(top, secondary);
            if (mesh == null) return;
            _ownedMeshes.Add(mesh);
            var go = new GameObject((top ? "Top" : "Bottom") + (secondary ? "SecondaryCrystals" : "Crystals"));
            go.transform.SetParent(parent, false);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _lib.CrystalMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            _crystalRenderers.Add(mr);
        }

        private Mesh CombineCrystals(bool top, bool secondary)
        {
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var cols = new List<Color>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            foreach (var c in Geometry.Crystals)
            {
                if (c.TopHalf != top || c.Secondary != secondary) continue;
                byte dmg = Condition.DamageAt(c.Index);
                if (dmg >= CrystalDamage.Missing) continue;
                var data = _lib.GetMeshData(c.Archetype);
                if (data == null) continue;
                float cutY = dmg == CrystalDamage.Chipped ? data.Height * 0.78f : dmg == CrystalDamage.Broken ? data.Height * 0.42f : float.MaxValue;
                var m = Matrix4x4.TRS(c.Position, c.Rotation, c.Scale);
                var nm = m.inverse.transpose;
                int baseIndex = verts.Count;
                var tint = c.Tint;
                for (int i = 0; i < data.Vertices.Length; i++)
                {
                    var v = data.Vertices[i];
                    var n = data.Normals[i];
                    bool cut = v.y > cutY;
                    if (cut) { v.y = cutY; n = Vector3.up; }
                    verts.Add(m.MultiplyPoint3x4(v));
                    norms.Add(nm.MultiplyVector(n).normalized);
                    float zone = Mathf.Clamp01(v.y / data.Height);
                    var col = cut ? new Color(tint.r * 0.8f, tint.g * 0.8f, tint.b * 0.8f, 0f) : new Color(tint.r, tint.g, tint.b, zone);
                    cols.Add(col);
                    uvs.Add(new Vector2(v.x, v.z));
                }
                for (int t = 0; t < data.Triangles.Length; t++) tris.Add(baseIndex + data.Triangles[t]);
            }
            if (verts.Count == 0) return null;
            var mesh = new Mesh { name = (top ? "Top" : "Bottom") + (secondary ? "SecondaryCrystals" : "Crystals") };
            mesh.indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetColors(cols);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Color ApplySaturation(Color c, float saturation)
        {
            float luma = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
            var grey = new Color(luma, luma, luma, 1f);
            float k = Mathf.Lerp(0.35f, 1.2f, saturation);
            var r = Color.LerpUnclamped(grey, c, k);
            return new Color(Mathf.Clamp01(r.r), Mathf.Clamp01(r.g), Mathf.Clamp01(r.b), 1f);
        }

        private void ApplyShellProperties()
        {
            var g = Geology;
            var fam = g.Family;
            var pal = g.Palette;
            var tone = MatrixTones[Mathf.Clamp(g.ExteriorTone, 0, MatrixTones.Length - 1)];
            var tone2 = Color.Lerp(tone, Color.black, 0.35f);
            var weathered = Color.Lerp(tone, new Color(0.5f, 0.44f, 0.36f), g.Weathering * 0.35f);
            _mpb.Clear();
            _mpb.SetColor(RockColorId, weathered);
            _mpb.SetColor(RockColor2Id, tone2);
            _mpb.SetColor(CavityColorId, fam.CavityWall);
            _mpb.SetColor(RimColorId, Color.Lerp(tone, Color.white, 0.1f));
            _mpb.SetColor(BandAId, pal.BandA);
            _mpb.SetColor(BandBId, pal.BandB);
            _mpb.SetFloat(BandStrengthId, fam.BandStrength);
            _mpb.SetFloat(BandFrequencyId, fam.BandFrequency);
            _mpb.SetFloat(BandOffsetId, g.BandOffset);
            var hint = ApplySaturation(Color.Lerp(pal.SurfaceA, pal.SurfaceB, 0.5f), g.Saturation);
            _mpb.SetColor(HintColorId, hint);
            _mpb.SetFloat(HintAmountId, g.ExteriorHint);
            _mpb.SetFloat(WeatheringId, g.Weathering);
            _mpb.SetFloat(CavitySmoothId, fam.Id == MineralId.Agate ? 0.65f : 0.35f);
            _mpb.SetFloat(CavityDruzyId, CavityDruzyAmount(g));
            _mpb.SetColor(CavityCrystalColorId, ApplySaturation(Color.Lerp(pal.SurfaceA, pal.SurfaceB, 0.5f), g.Saturation));
            _mpb.SetFloat(HighlightId, _highlight);
            if (TopShellRenderer != null) TopShellRenderer.SetPropertyBlock(_mpb);
            if (BottomShellRenderer != null) BottomShellRenderer.SetPropertyBlock(_mpb);
        }

        private static float CavityDruzyAmount(SpecimenGeology g)
        {
            if (g.IsDruzy) return 1f;
            var fam = g.Family;
            float density = Mathf.Lerp(fam.DensityMin, fam.DensityMax, g.CrystalDensity);
            switch (fam.Placement)
            {
                case PlacementStyle.Carpet: return Mathf.Clamp01(density * 1.1f - 0.15f);
                case PlacementStyle.Banded: return 0.85f;
                case PlacementStyle.Clustered: return Mathf.Clamp01(density * 0.4f);
                case PlacementStyle.Scattered: return 0.12f;
                case PlacementStyle.Sprays: return 0.1f;
                default: return 0f;
            }
        }

        private void ApplyCrystalProperties()
        {
            foreach (var r in _crystalRenderers)
            {
                bool secondary = r.name.Contains("Secondary");
                var fam = secondary ? Geology.SecondaryFamily : Geology.Family;
                if (fam == null) continue;
                var pal = secondary ? fam.Palettes[0] : Geology.Palette;
                float sat = secondary ? 0.7f : Geology.Saturation;
                float clarity = Geology.Clarity;
                _mpb.Clear();
                _mpb.SetColor(BaseColorId, ApplySaturation(pal.SurfaceA, sat));
                _mpb.SetColor(DeepColorId, ApplySaturation(Color.Lerp(pal.DeepA, pal.DeepB, 0.5f + Geology.HueShift * 0.5f), sat));
                _mpb.SetColor(ZoneColorId, ApplySaturation(pal.Zone, sat));
                _mpb.SetFloat(SmoothnessId, fam.Smoothness);
                _mpb.SetFloat(MetallicId, fam.Metallic);
                _mpb.SetFloat(TranslucencyId, fam.Translucency * Mathf.Lerp(0.55f, 1.1f, clarity));
                _mpb.SetFloat(RimStrengthId, fam.Rim);
                _mpb.SetFloat(SparkleId, fam.Sparkle * Mathf.Lerp(0.6f, 1.3f, clarity));
                _mpb.SetFloat(ZoningId, secondary ? fam.ZoningBase : Geology.Zoning);
                _mpb.SetFloat(InclusionsId, fam.Inclusions * (1f - clarity * 0.8f));
                _mpb.SetFloat(HighlightId, _highlight);
                r.SetPropertyBlock(_mpb);
            }
        }

        /// <summary>Interaction highlight 0..1 on every renderer.</summary>
        public void SetHighlight(float amount)
        {
            if (Mathf.Approximately(_highlight, amount)) return;
            _highlight = amount;
            if (Geology == null) return;
            ApplyShellProperties();
            ApplyCrystalProperties();
        }

        /// <summary>Damage fraction weighted by crystal size: what the appraisal sees.</summary>
        public float CrystalDamageFraction()
        {
            if (Geometry == null || Geometry.Crystals.Count == 0) return 0f;
            float total = 0f, lost = 0f;
            foreach (var c in Geometry.Crystals)
            {
                float w = c.Height * c.Height * (c.Centerpiece ? 4f : 1f);
                total += w;
                byte d = Condition.DamageAt(c.Index);
                lost += w * (d == CrystalDamage.Chipped ? 0.3f : d == CrystalDamage.Broken ? 0.7f : d >= CrystalDamage.Missing ? 1f : 0f);
            }
            return total > 0f ? lost / total : 0f;
        }

        private void OnDestroy()
        {
            foreach (var m in _ownedMeshes) if (m != null) DestroyObj(m);
            _ownedMeshes.Clear();
        }
    }
}
