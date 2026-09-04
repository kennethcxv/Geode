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
        public Mesh TopColliderMesh { get; private set; }
        public Mesh BottomColliderMesh { get; private set; }

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
        private static readonly int SectorCrackId = Shader.PropertyToID("_SectorCrack");
        private static readonly int ImpactsId = Shader.PropertyToID("_Impacts");
        private static readonly int ImpactCountId = Shader.PropertyToID("_ImpactCount");
        private static readonly int SeamVisibleId = Shader.PropertyToID("_SeamVisible");
        private static readonly int SurfRId = Shader.PropertyToID("_SurfR");
        private static readonly int CrackFadeId = Shader.PropertyToID("_CrackFade");
        private static readonly int TexFamilyId = Shader.PropertyToID("_TexFamily");
        private static readonly int DirtId = Shader.PropertyToID("_Dirt");
        private static readonly int StainId = Shader.PropertyToID("_Stain");
        private static readonly int ChipId = Shader.PropertyToID("_Chip");
        private static readonly int PolishId = Shader.PropertyToID("_Polish");
        private static readonly int WetId = Shader.PropertyToID("_Wet");
        private static readonly int DustId = Shader.PropertyToID("_Dust");
        private static readonly int F0Id = Shader.PropertyToID("_F0");
        private static readonly int ClarityId = Shader.PropertyToID("_Clarity");
        private static readonly int ZoneBandsId = Shader.PropertyToID("_ZoneBands");
        private static readonly int StriationId = Shader.PropertyToID("_Striation");
        private static readonly int InclusionColorId = Shader.PropertyToID("_InclusionColor");
        private static readonly int CrystalWetId = Shader.PropertyToID("_Wet");

        /// <summary>Transient: 1 straight out of the wash tub or off the saw, drying back to 0 over ~45 s. Never saved, never valued.</summary>
        public float Wetness { get; private set; }
        private float _wetRefresh;
        public const float DrySeconds = 45f;

        public void SetWet(float amount)
        {
            Wetness = Mathf.Clamp01(Mathf.Max(Wetness, amount));
            if (Geology != null) ApplyShellProperties();
        }

        private void Update()
        {
            if (Wetness <= 0f) return;
            Wetness = Mathf.Max(0f, Wetness - Time.deltaTime / DrySeconds);
            _wetRefresh -= Time.deltaTime;
            if (_wetRefresh <= 0f || Wetness <= 0f) { _wetRefresh = 0.4f; if (Geology != null) ApplyShellProperties(); }
        }

        /// <summary>Clay still on the shell, 0..1: the geology's coating less whatever has been scrubbed off.</summary>
        public float DirtRemaining => Geology != null && Condition != null ? Mathf.Clamp01(Geology.Dirt * (1f - Condition.Cleaned)) : 0f;

        /// <summary>Re-apply condition-driven shell properties (after scrubbing).</summary>
        public void RefreshCondition() { if (Geology != null) { ApplyShellProperties(); ApplyCrystalProperties(); } }

        /// <summary>Rock dust on a freshly opened interior: 1 until the rock is rinsed. Pieces come off the saw rinsed.</summary>
        public float Dust => Condition != null && Condition.Opened && !Condition.Rinsed && !IsPiece ? 1f : 0f;

        private static readonly int CutPlaneId = Shader.PropertyToID("_CutPlane");
        private static readonly int CutFeedId = Shader.PropertyToID("_CutFeed");
        private static readonly int CutShowId = Shader.PropertyToID("_CutShow");
        private static readonly int CutDepthId = Shader.PropertyToID("_CutDepth");
        private static readonly int CutDoneId = Shader.PropertyToID("_CutDone");
        private Vector4 _cutDepth = new Vector4(0f, 1f, 0f, 100f), _cutDone = new Vector4(0f, 1f, 0f, -100f);

        /// <summary>Saw: how far up the rock the kerf reaches (up axis xyz, limit w) and a region already cut (w below -9: none).</summary>
        public void SetCutMasks(Vector4 depth, Vector4 done) { _cutDepth = depth; _cutDone = done; if (Geology != null) ApplyShellProperties(); }
        private Vector4 _cutPlane = new Vector4(0f, 1f, 0f, 0f), _cutFeed = new Vector4(1f, 0f, 0f, -10f);
        private float _cutShow;

        /// <summary>Saw preview: the planned plane (object space) and the kerf reach along the feed axis; show=0 clears it.</summary>
        public void SetCutPreview(Vector3 normal, float height, Vector3 feedAxis, float reach, float show)
        {
            _cutPlane = new Vector4(normal.x, normal.y, normal.z, height);
            _cutFeed = new Vector4(feedAxis.x, feedAxis.y, feedAxis.z, reach);
            _cutShow = show;
            if (Geology != null) ApplyShellProperties();
        }

        public const int CrackSectors = 16;
        public const int MaxImpacts = 32;
        private readonly float[] _sectorCrack = new float[CrackSectors];
        private readonly Vector4[] _impacts = new Vector4[MaxImpacts];
        private int _impactCount;
        private float _seamVisible = 0.4f, _crackFade = 1f;

        /// <summary>
        /// Fracture overlay state: per-sector seam stress (>= 1 is an open crack), chisel marks in shell surface
        /// coordinates, how visible the faint seam guide is, and a fade for opened specimens.
        /// </summary>
        public void SetCrackState(float[] sectorStress, IList<Vector4> impacts, float seamVisible, float fade)
        {
            for (int i = 0; i < CrackSectors; i++) _sectorCrack[i] = sectorStress != null && i < sectorStress.Length ? sectorStress[i] : 0f;
            _impactCount = 0;
            if (impacts != null)
                for (int i = Mathf.Max(0, impacts.Count - MaxImpacts); i < impacts.Count; i++) _impacts[_impactCount++] = impacts[i];
            for (int i = _impactCount; i < MaxImpacts; i++) _impacts[i] = Vector4.zero;
            _seamVisible = seamVisible;
            _crackFade = fade;
            if (Geology != null) ApplyShellProperties();
        }

        // real rock albedo is low (0.2-0.4): brighter tones blew out to cream under the bench lamp
        public static readonly Color[] MatrixTones =
        {
            new Color(0.35f, 0.3f, 0.26f), // warm brown-grey
            new Color(0.31f, 0.31f, 0.3f), // neutral grey
            new Color(0.39f, 0.33f, 0.25f), // tan
            new Color(0.22f, 0.21f, 0.2f), // dark basalt
        };

        public bool IsPiece => Geometry != null && Geometry.IsPiece;
        /// <summary>Finish of the cut faces, 0..1 (pieces only); pushed to the shell shader.</summary>
        public float Polish { get; private set; }

        /// <summary>
        /// Scenery budget: keep only the N largest crystals in the cavity (0 = every one). A dense druse is
        /// around six hundred crystals combined into one mesh, near a hundred thousand triangles a rock; forty of
        /// those on shop shelves is four million triangles of set dressing on an eight-gigabyte machine. Set this
        /// before Build. Anything the player can pick up, appraise or sell leaves it at 0.
        /// </summary>
        public int CrystalBudget;
        private float _crystalCut;   // crystals shorter than this are dropped when a budget is set

        public void Build(SpecimenGeology geology, SpecimenCondition condition, SpecimenAssetLibrary lib) => Build(geology, condition, lib, null, 0f);

        /// <summary>Build a whole specimen (piece == null) or one sawn piece of it.</summary>
        public void Build(SpecimenGeology geology, SpecimenCondition condition, SpecimenAssetLibrary lib, PieceShape? piece, float polish)
        {
            Clear();
            Geology = geology;
            Condition = condition ?? new SpecimenCondition();
            _lib = lib;
            Polish = polish;
            _mpb ??= new MaterialPropertyBlock();
            if (piece.HasValue)
            {
                Geometry = GeodeMeshBuilder.BuildPiece(geology, piece.Value);
                Condition.EnsureSize(GeodeMeshBuilder.Build(geology).Crystals.Count);   // damage is indexed by the whole rock's crystals
                BottomHalf = CreateHalf("Piece", Geometry.Bottom, out var pieceRenderer);
                TopHalf = null;
                BottomColliderMesh = HullMesh("Piece_Collider", Geometry.HullPoints);
                TopColliderMesh = null;
                _ownedMeshes.Add(BottomColliderMesh);
                BottomShellRenderer = pieceRenderer;
                TopShellRenderer = null;
                RebuildCrystals();
                ApplyShellProperties();
                SetCrystalsVisible(true);
                return;
            }
            Geometry = GeodeMeshBuilder.Build(geology);
            Condition.EnsureSize(Geometry.Crystals.Count);

            BottomHalf = CreateHalf("BottomHalf", Geometry.Bottom, out var bottomRenderer);
            TopHalf = CreateHalf("TopHalf", Geometry.Top, out var topRenderer);
            BottomColliderMesh = Geometry.Bottom.ToColliderMesh("BottomHalf_Collider", GeodeMeshBuilder.Longitudes, GeodeMeshBuilder.Latitudes);
            TopColliderMesh = Geometry.Top.ToColliderMesh("TopHalf_Collider", GeodeMeshBuilder.Longitudes, GeodeMeshBuilder.Latitudes);
            _ownedMeshes.Add(BottomColliderMesh);
            _ownedMeshes.Add(TopColliderMesh);
            BottomShellRenderer = bottomRenderer;
            TopShellRenderer = topRenderer;
            // crystal meshes are built lazily: a closed rock shows none, and the reveal rebuilds them with damage anyway
            if (Condition.Opened) RebuildCrystals();
            ApplyShellProperties();
            SetCrystalsVisible(Condition.Opened);
        }

        /// <summary>Update the cut-face finish (polishing) without rebuilding.</summary>
        public void SetPolish(float polish)
        {
            Polish = Mathf.Clamp01(polish);
            if (Geology != null) ApplyShellProperties();
        }

        /// <summary>A convex-hull source mesh from a point cloud (PhysX takes the hull of the vertices; the triangles only need to exist).</summary>
        private static Mesh HullMesh(string name, Vector3[] points)
        {
            var m = new Mesh { name = name };
            if (points == null || points.Length < 4) points = new[] { new Vector3(-0.01f, 0f, -0.01f), new Vector3(0.01f, 0f, -0.01f), new Vector3(0f, 0f, 0.01f), new Vector3(0f, 0.02f, 0f) };
            m.vertices = points;
            var tris = new List<int>();
            for (int i = 1; i + 1 < points.Length; i++) { tris.Add(0); tris.Add(i); tris.Add(i + 1); }
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        private bool _crystalsBuilt;

        /// <summary>Crystals are hidden while the rock is closed (nothing inside is visible anyway).</summary>
        public void SetCrystalsVisible(bool visible)
        {
            if (visible && !_crystalsBuilt) RebuildCrystals();
            foreach (var r in _crystalRenderers) if (r != null) r.enabled = visible;
        }

        public void Clear()
        {
            foreach (var m in _ownedMeshes) if (m != null) DestroyObj(m);
            _ownedMeshes.Clear();
            _crystalRenderers.Clear();
            _crystalsBuilt = false;
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
            ComputeCrystalCut();
            foreach (var r in _crystalRenderers) if (r != null) DestroyObj(r.gameObject);
            _crystalRenderers.Clear();
            for (int i = _ownedMeshes.Count - 1; i >= 0; i--)
                if (_ownedMeshes[i] != null && _ownedMeshes[i].name.Contains("Crystals")) { DestroyObj(_ownedMeshes[i]); _ownedMeshes.RemoveAt(i); }

            if (IsPiece)
            {
                // a piece keeps every crystal it contains on its one body, whichever half of the rock they grew in
                CreateCrystalObject(BottomHalf, false, false, true);
                CreateCrystalObject(BottomHalf, false, true, true);
            }
            else
            {
                CreateCrystalObject(BottomHalf, false, false);
                CreateCrystalObject(BottomHalf, false, true);
                CreateCrystalObject(TopHalf, true, false);
                CreateCrystalObject(TopHalf, true, true);
            }
            ApplyCrystalProperties();
            _crystalsBuilt = true;
        }

        /// <summary>The height of the smallest crystal the budget keeps; 0 keeps them all.</summary>
        private void ComputeCrystalCut()
        {
            _crystalCut = 0f;
            if (CrystalBudget <= 0 || Geometry == null || Geometry.Crystals == null || Geometry.Crystals.Count <= CrystalBudget) return;
            var sizes = new List<float>(Geometry.Crystals.Count);
            foreach (var c in Geometry.Crystals) sizes.Add(c.Scale.y);
            sizes.Sort();
            _crystalCut = sizes[sizes.Count - CrystalBudget];
        }

        private void CreateCrystalObject(Transform parent, bool top, bool secondary, bool anyHalf = false)
        {
            var mesh = CombineCrystals(top, secondary, anyHalf);
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

        private Mesh CombineCrystals(bool top, bool secondary, bool anyHalf = false)
        {
            // size the buffers first so the combine never grows (a dense druzy carpet is ~600 crystals)
            int vCount = 0, iCount = 0;
            foreach (var c in Geometry.Crystals)
            {
                if ((!anyHalf && c.TopHalf != top) || c.Secondary != secondary) continue;
                if (c.Scale.y < _crystalCut) continue;
                var d = _lib.GetMeshData(c.Archetype);
                if (d == null) continue;
                vCount += d.Vertices.Length; iCount += d.Triangles.Length;
                if (Condition.DamageAt(c.Index) >= CrystalDamage.Broken) { vCount += d.Vertices.Length; iCount += d.Triangles.Length; }   // its fallen tip
            }
            if (vCount == 0) return null;
            var verts = new List<Vector3>(vCount);
            var norms = new List<Vector3>(vCount);
            var cols = new List<Color>(vCount);
            var uvs = new List<Vector2>(vCount);
            var tris = new List<int>(iCount);
            bool clipTop = !float.IsNaN(Geometry.ClipTopY), clipBottom = !float.IsNaN(Geometry.ClipBottomY);
            foreach (var c in Geometry.Crystals)
            {
                if ((!anyHalf && c.TopHalf != top) || c.Secondary != secondary) continue;
                if (c.Scale.y < _crystalCut) continue;
                byte dmg = Condition.DamageAt(c.Index);
                var data = _lib.GetMeshData(c.Archetype);
                if (data == null) continue;
                // chipped: tip gone; broken: half gone; missing: only a pale stub of the base remains
                float cutY = dmg == CrystalDamage.Chipped ? data.Height * 0.78f : dmg == CrystalDamage.Broken ? data.Height * 0.42f : dmg >= CrystalDamage.Missing ? data.Height * 0.14f : float.MaxValue;
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
                    var wp = m.MultiplyPoint3x4(v);
                    var wn = nm.MultiplyVector(n).normalized;
                    // the saw's plane: anything past it is pressed flat onto the cut face, a frosted sawn facet
                    bool sawn = false;
                    if (clipTop && wp.y > Geometry.ClipTopY) { wp.y = Geometry.ClipTopY; wn = Vector3.up; sawn = true; }
                    if (clipBottom && wp.y < Geometry.ClipBottomY) { wp.y = Geometry.ClipBottomY; wn = Vector3.down; sawn = true; }
                    verts.Add(wp);
                    norms.Add(wn);
                    float zone = Mathf.Clamp01(v.y / data.Height);
                    // fresh fracture faces read frosted/pale, and everything on a stub is pale
                    bool pale = cut || sawn || dmg >= CrystalDamage.Missing;
                    var col = pale ? new Color(Mathf.Min(1.5f, tint.r * 1.55f), Mathf.Min(1.5f, tint.g * 1.5f), Mathf.Min(1.5f, tint.b * 1.45f), 0f) : new Color(tint.r, tint.g, tint.b, zone);
                    cols.Add(col);
                    uvs.Add(new Vector2(v.x, v.z));
                }
                for (int t = 0; t < data.Triangles.Length; t++) tris.Add(baseIndex + data.Triangles[t]);

                // the broken-off tip lies in the cavity beside its stub: the piece above the cut, toppled and shrunk
                if (dmg >= CrystalDamage.Broken)
                {
                    float tipLen = data.Height - cutY;
                    if (tipLen > data.Height * 0.15f)
                    {
                        uint h = (uint)(c.Index * 2654435761u);
                        float yaw = (h % 360u), roll = 70f + (h >> 9) % 25u;
                        var lie = c.Rotation * Quaternion.Euler(roll, yaw, 0f);
                        // it falls inward, toward the cavity floor, so it can never poke through the shell
                        Vector3 lobe = Geology.LobeCenters != null && Geology.LobeCenters.Length > 0 ? Geology.LobeCenters[0] : Vector3.zero;
                        Vector3 inward = lobe - c.Position; if (inward.sqrMagnitude < 1e-6f) inward = c.Rotation * Vector3.up; inward.Normalize();
                        // tipLen is in the archetype's unit space; the offset has to be in metres or the tip flies out of the rock
                        float tipLenM = tipLen / data.Height * c.Height;
                        Vector3 at = c.Position + inward * (c.Footprint * 1.5f + tipLenM * 0.35f);
                        var fm = Matrix4x4.TRS(at, lie, c.Scale * 0.8f);
                        var fnm = fm.inverse.transpose;
                        int fBase = verts.Count;
                        for (int i = 0; i < data.Vertices.Length; i++)
                        {
                            var v = data.Vertices[i];
                            var n = data.Normals[i];
                            bool below = v.y < cutY;                   // the part that stayed on the stub
                            if (below) { v.y = cutY; n = Vector3.down; }
                            v.y -= cutY;                              // pivot at the break
                            verts.Add(fm.MultiplyPoint3x4(v));
                            norms.Add(fnm.MultiplyVector(n).normalized);
                            var fcol = below ? new Color(Mathf.Min(1.5f, tint.r * 1.55f), Mathf.Min(1.5f, tint.g * 1.5f), Mathf.Min(1.5f, tint.b * 1.45f), 0f) : new Color(tint.r, tint.g, tint.b, 1f);
                            cols.Add(fcol);
                            uvs.Add(new Vector2(v.x, v.z));
                        }
                        for (int t = 0; t < data.Triangles.Length; t++) tris.Add(fBase + data.Triangles[t]);
                    }
                }
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
            var tone2 = Color.Lerp(tone, Color.black, 0.5f);
            var weathered = Color.Lerp(tone, new Color(0.38f, 0.33f, 0.27f), g.Weathering * 0.35f);
            _mpb.Clear();
            _mpb.SetColor(RockColorId, weathered);
            _mpb.SetColor(RockColor2Id, tone2);
            _mpb.SetColor(CavityColorId, fam.CavityWall);
            _mpb.SetColor(RimColorId, Color.Lerp(tone, Color.black, 0.3f));
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
            var druzyCol = Color.Lerp(pal.SurfaceA, pal.SurfaceB, 0.5f);
            // metallic ores: the druzy floor is dark steel, not chrome; a cloudy specimen's floor is milky like its points;
            // under scattered / clustered / spray habits the lining is a pale quartz druse, only tinted by the family
            if (fam.Metallic > 0.5f) druzyCol = Color.Lerp(druzyCol, pal.DeepA, 0.65f);
            else druzyCol = Color.Lerp(druzyCol, Color.Lerp(druzyCol, Color.white, 0.5f), (1f - g.Clarity) * 0.45f);
            if (!g.IsDruzy && fam.Placement != PlacementStyle.Carpet && fam.Placement != PlacementStyle.Banded)
                druzyCol = Color.Lerp(new Color(0.84f, 0.83f, 0.8f), druzyCol, fam.Metallic > 0.5f ? 0.55f : 0.3f);
            _mpb.SetColor(CavityCrystalColorId, ApplySaturation(druzyCol, g.Saturation));
            _mpb.SetFloat(HighlightId, _highlight);
            _mpb.SetFloatArray(SectorCrackId, _sectorCrack);
            _mpb.SetVectorArray(ImpactsId, _impacts);
            _mpb.SetFloat(ImpactCountId, _impactCount);
            _mpb.SetFloat(SeamVisibleId, _seamVisible);
            _mpb.SetFloat(SurfRId, Geometry != null ? Geometry.MeanEquatorRadius : 0.06f);
            _mpb.SetFloat(CrackFadeId, _crackFade);
            // exterior character: texture family, clay coating less what has been scrubbed off, staining, natural chip
            _mpb.SetFloat(TexFamilyId, (int)g.Texture);
            _mpb.SetFloat(DirtId, DirtRemaining);
            _mpb.SetFloat(StainId, g.Stain);
            float chipR = (Geometry != null ? Geometry.MeanEquatorRadius : 0.06f) * 0.2f;
            _mpb.SetVector(ChipId, new Vector4(g.ChipLongitude, g.ChipLatitude, chipR, g.HasNaturalChip ? 1f : 0f));
            _mpb.SetFloat(PolishId, Polish);
            _mpb.SetFloat(WetId, Wetness);
            _mpb.SetFloat(DustId, Dust);
            _mpb.SetVector(CutPlaneId, _cutPlane);
            _mpb.SetVector(CutFeedId, _cutFeed);
            _mpb.SetFloat(CutShowId, _cutShow);
            _mpb.SetVector(CutDepthId, _cutDepth);
            _mpb.SetVector(CutDoneId, _cutDone);
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
                // V6: no bare bowls. A vug with scattered or clustered crystals is lined with a fine druse (quartz or
                // calcite) that the accent crystals sit on, the way real vugs are
                case PlacementStyle.Carpet: return Mathf.Clamp01(density * 1.1f - 0.15f);
                case PlacementStyle.Banded: return 0.85f;
                case PlacementStyle.Clustered: return Mathf.Clamp01(0.35f + density * 0.4f);
                case PlacementStyle.Scattered: return 0.55f;
                case PlacementStyle.Sprays: return 0.5f;
                default: return 0.3f;
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
                // the stats have to be visible at arm's length: a pale specimen reads milky and light, a saturated one
                // carries its colour deep into the body; a cloudy one is matte and full of inclusions, a clear one
                // is glassy. Metallic families keep their lustre and only shift brightness.
                var baseCol = ApplySaturation(pal.SurfaceA, sat);
                float luma = 0.299f * baseCol.r + 0.587f * baseCol.g + 0.114f * baseCol.b;
                var milk = new Color(Mathf.Lerp(luma, 1f, 0.55f), Mathf.Lerp(luma, 1f, 0.55f), Mathf.Lerp(luma, 1f, 0.55f), 1f);
                if (fam.Metallic < 0.5f) baseCol = Color.Lerp(baseCol, milk, (1f - sat) * 0.4f);
                var deepCol = ApplySaturation(Color.Lerp(pal.DeepA, pal.DeepB, 0.5f + Geology.HueShift * 0.5f), sat);
                deepCol = Color.Lerp(deepCol, baseCol, (1f - sat) * 0.45f) * Mathf.Lerp(1.12f, 0.82f, sat);
                deepCol.a = 1f;
                _mpb.Clear();
                _mpb.SetColor(BaseColorId, baseCol);
                _mpb.SetColor(DeepColorId, deepCol);
                _mpb.SetColor(ZoneColorId, ApplySaturation(pal.Zone, sat));
                _mpb.SetFloat(SmoothnessId, fam.Smoothness * Mathf.Lerp(0.86f, 1f, clarity));
                _mpb.SetFloat(MetallicId, fam.Metallic);
                _mpb.SetFloat(TranslucencyId, fam.Translucency * Mathf.Lerp(0.35f, 1.15f, clarity));
                _mpb.SetFloat(RimStrengthId, fam.Rim * Mathf.Lerp(0.7f, 1.1f, clarity));
                _mpb.SetFloat(SparkleId, fam.Sparkle * Mathf.Lerp(0.45f, 1.4f, clarity));
                _mpb.SetFloat(ZoningId, secondary ? fam.ZoningBase : Geology.Zoning * Mathf.Lerp(0.6f, 1.1f, sat));
                _mpb.SetFloat(InclusionsId, Mathf.Clamp01(fam.Inclusions * (1f - clarity * 0.9f) + (1f - clarity) * 0.25f));
                _mpb.SetFloat(HighlightId, _highlight);
                _mpb.SetFloat(DustId, Dust);
                // V6 §20: the mineral's own identity: reflectance, clarity, phantom bands, prism striations, inclusion colour, wet film
                _mpb.SetFloat(F0Id, fam.F0);
                _mpb.SetFloat(ClarityId, clarity);
                _mpb.SetFloat(ZoneBandsId, fam.ZoneBands);
                _mpb.SetFloat(StriationId, fam.Striation);
                var incCol = Color.Lerp(baseCol, Color.white, 0.55f); incCol.a = 1f;
                _mpb.SetColor(InclusionColorId, fam.Metallic > 0.5f ? Color.Lerp(baseCol, Color.black, 0.4f) : incCol);
                _mpb.SetFloat(CrystalWetId, Wetness);
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

        /// <summary>Damage fraction weighted by crystal size: what the appraisal sees (a piece's sawn crystals are its cut, not damage).</summary>
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

        /// <summary>Crystals within this distance of a piece-frame height, for the saw's chipping along the kerf.</summary>
        public List<int> CrystalsNearPlane(float y, float distance)
        {
            var list = new List<int>();
            if (Geometry == null) return list;
            foreach (var c in Geometry.Crystals)
            {
                float top = c.Position.y + (c.Rotation * Vector3.up).y * c.Height;
                float lo = Mathf.Min(c.Position.y, top), hi = Mathf.Max(c.Position.y, top);
                if (y > lo - distance && y < hi + distance) list.Add(c.Index);
            }
            return list;
        }

        private void OnDestroy()
        {
            foreach (var m in _ownedMeshes) if (m != null) DestroyObj(m);
            _ownedMeshes.Clear();
        }
    }
}
