using System;
using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Core;

namespace GeodeEmpire.Specimens
{
    /// <summary>One placed crystal. Specimen-local space, +Y of the archetype mesh is the growth axis.</summary>
    [Serializable]
    public struct CrystalInstance
    {
        public int Index;
        public bool TopHalf;
        public bool Secondary;
        public CrystalArchetype Archetype;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public Color Tint;
        public bool Centerpiece;
        public float Fragility;
        public float Azimuth;       // radians around Y
        public float Latitude;      // 0 at fracture plane .. 1 at pole
        public float Footprint;     // approx radius (m)
        public float Height;        // m
        /// <summary>The saw took the top off this crystal (sawn pieces only).</summary>
        public bool Truncated;
    }

    public sealed class GeodeHalfGeometry
    {
        public bool IsTop;
        public Vector3[] Vertices;
        public Vector2[] UVs;
        /// <summary>Surface coordinates for the fracture overlay: x = longitude fraction, y = signed latitude fraction (0 at the seam).</summary>
        public Vector2[] UV2;
        public Color[] Colors;
        public int[] Triangles;
        public float[] EquatorOuterRadius;   // per longitude, for crack ribbons
        public float[] EquatorY;             // per longitude, rim jitter (already signed)
        public float PoleY;

        /// <summary>Coarse convex hull source (exterior every 4th longitude / 3rd ring + rim) so PhysX stays under its polygon limit.</summary>
        public Mesh ToColliderMesh(string name, int longitudes, int latitudes)
        {
            int N = longitudes, M = latitudes;
            int stepLon = 4, stepLat = 3;
            var pts = new List<Vector3>();
            for (int k = 0; k < M; k += stepLat)
                for (int i = 0; i < N; i += stepLon)
                    pts.Add(Vertices[k * N + i]);
            pts.Add(Vertices[M * N]); // pole
            int cols = N / stepLon;
            var tris = new List<int>();
            int rows = (M + stepLat - 1) / stepLat;
            for (int r = 0; r < rows - 1; r++)
                for (int c = 0; c < cols; c++)
                {
                    int a = r * cols + c, b = r * cols + (c + 1) % cols, d = (r + 1) * cols + c, e = (r + 1) * cols + (c + 1) % cols;
                    tris.Add(a); tris.Add(b); tris.Add(e); tris.Add(a); tris.Add(e); tris.Add(d);
                }
            int pole = pts.Count - 1;
            for (int c = 0; c < cols; c++) { tris.Add((rows - 1) * cols + c); tris.Add((rows - 1) * cols + (c + 1) % cols); tris.Add(pole); }
            var m = new Mesh { name = name };
            m.SetVertices(pts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name };
            m.indexFormat = Vertices.Length > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            m.vertices = Vertices;
            m.uv = UVs;
            m.uv2 = UV2;
            m.colors = Colors;
            m.triangles = Triangles;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }

    public sealed class GeodeGeometry
    {
        public GeodeHalfGeometry Top;
        public GeodeHalfGeometry Bottom;
        public List<CrystalInstance> Crystals;
        public float MaxRadius;
        public float MeanEquatorRadius;
        public float MeanCavityRadius;
        public float BottomY;
        public float TopY;
        public int Longitudes;
        /// <summary>Set for sawn pieces: the shape that was cut, and what the cut exposed.</summary>
        public PieceShape Piece;
        public bool IsPiece;
        /// <summary>Rotation applied to the specimen frame so the piece's primary cut face points up (+Y).</summary>
        public Quaternion PieceRotation = Quaternion.identity;
        /// <summary>Crystal weight kept by this piece as a fraction of the whole specimen's, with truncated crystals at 40%.</summary>
        public float RetainedCrystalFraction = 1f;
        /// <summary>0..1: how much of the main cavity's cross-section the primary cut face opens (0 = solid face).</summary>
        public float CavityOpening;
        /// <summary>0..1: how central the primary cut runs through the main cavity (1 = through its centre).</summary>
        public float CutSymmetry = 1f;
        /// <summary>Area of the primary cut face relative to the specimen's central cross-section.</summary>
        public float FaceAreaFraction;
        /// <summary>True when the piece contains any cavity wall at all.</summary>
        public bool HasCavity = true;
        /// <summary>Piece frame heights of the cut faces (NaN when that end is natural): crystals are clipped to them.</summary>
        public float ClipTopY = float.NaN, ClipBottomY = float.NaN;
        /// <summary>Coarse exterior point set for the convex collider of a piece.</summary>
        public Vector3[] HullPoints;
    }

    /// <summary>
    /// A sawn piece: the part of the specimen between two parallel planes normal to Normal (specimen-local),
    /// at heights Lo and Hi along it. A missing end (HasLo/HasHi false) is the rock's natural outside.
    /// Every cut of a piece keeps the same Normal, so a lineage is just a list of heights.
    /// </summary>
    [Serializable]
    public struct PieceShape
    {
        public Vector3 Normal;
        public float Lo, Hi;
        public bool HasLo, HasHi;

        public static PieceShape Below(Vector3 n, float h) => new PieceShape { Normal = n.normalized, Hi = h, HasHi = true, Lo = -1f, HasLo = false };
        public static PieceShape Above(Vector3 n, float h) => new PieceShape { Normal = n.normalized, Lo = h, HasLo = true, Hi = 1f, HasHi = false };
        public bool IsSlab => HasLo && HasHi;
        /// <summary>The outward normal of the face that should point up on a shelf: the primary cut face.</summary>
        public Vector3 UpNormal => HasHi ? Normal : -Normal;
        public float Thickness => IsSlab ? Hi - Lo : float.PositiveInfinity;
    }

    /// <summary>
    /// Pure, deterministic geometry: two half shells (exterior + cut face + cavity wall) and a crystal
    /// placement list. No UnityEngine.Object allocation, so it is safe in EditMode tests and threads.
    /// </summary>
    public static class GeodeMeshBuilder
    {
        // V5: 64 x 20 shell rings per half so a fist-sized rock reads as a rounded lump, not a faceted blob, at
        // arm's length. Crystal placement keeps its own coarser cell grid (PlaceLon/PlaceLat) so the crystal
        // statistics do not change with the mesh resolution.
        // V6: 96x30 rings per half (the shell is a small share of the rock's triangles next to its crystals) so the
        // knobs, flats, facet arrises and the conchoidal rim are carried by geometry rather than only by normal maps
        public const int Longitudes = 96;
        public const int Latitudes = 30;
        public const int RimRings = 12;
        public const int PlaceLon = 40;
        public const int PlaceLat = 14;

        private sealed class Shape
        {
            private readonly SpecimenGeology _g;
            private readonly Noise3D _lump, _bump, _wall, _rim, _billow, _fracture;
            private readonly Vector3 _off1, _off2, _off3, _off4;
            private readonly float _lumpFreq, _lumpAmp, _bumpFreq, _bumpAmp, _wallAmp, _billowFreq, _billowAmp;
            private readonly bool _angular;
            // angular rough: a dozen random fracture planes clip the ellipsoid into flat faces with sharp arrises
            private readonly Vector3[] _facetN;
            private readonly float[] _facetH;
            // V6 §12: no two rocks share a silhouette. One end heavier (lean), a broad low swell, a few soft flat
            // spots where the nodule sat in its bed, and shallow erosion dents where softer matrix weathered out.
            private readonly Vector3 _leanAxis;
            private readonly float _asym, _macroAmp, _pitAmp;
            private readonly Vector3[] _flatN;
            private readonly float[] _flatH;

            private static float SoftMin(float a, float b, float k)
            {
                float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / Mathf.Max(1e-6f, k));
                return Mathf.Lerp(b, a, h) - k * h * (1f - h);
            }

            public Shape(SpecimenGeology g)
            {
                _g = g;
                _lump = new Noise3D(SeededRandom.Combine(g.Seed, 11));
                _bump = new Noise3D(SeededRandom.Combine(g.Seed, 12));
                _wall = new Noise3D(SeededRandom.Combine(g.Seed, 13));
                _rim = new Noise3D(SeededRandom.Combine(g.Seed, 14));
                _billow = new Noise3D(SeededRandom.Combine(g.Seed, 16));
                _fracture = new Noise3D(SeededRandom.Combine(g.Seed, 17));
                var rng = new SeededRandom(SeededRandom.Combine(g.Seed, 15));
                _off1 = new Vector3(rng.Range(0f, 50f), rng.Range(0f, 50f), rng.Range(0f, 50f));
                _off2 = new Vector3(rng.Range(0f, 50f), rng.Range(0f, 50f), rng.Range(0f, 50f));
                _off3 = new Vector3(rng.Range(0f, 50f), rng.Range(0f, 50f), rng.Range(0f, 50f));
                _off4 = new Vector3(rng.Range(0f, 50f), rng.Range(0f, 50f), rng.Range(0f, 50f));
                // V6: the mesh carries only band-limited shape (about six ring samples per noise cycle at 96 longitudes);
                // anything finer aliases into diagonal ridges along the quad splits, and the rind tile carries that scale
                _lumpFreq = g.Exterior == ExteriorArchetype.Knobbly ? 2.2f : g.Exterior == ExteriorArchetype.Rounded ? 1.3f : 1.5f;
                // lump amplitude per rind type: knobbly rinds get their relief from the billow domes, rounded and
                // flattened rough stay broad and soft, angular rough is faceted by its fracture planes
                _lumpAmp = g.ExteriorRoughness * (g.Exterior == ExteriorArchetype.Angular ? 0.7f : g.Exterior == ExteriorArchetype.Knobbly ? 1.2f : 1.4f);
                _bumpFreq = g.Exterior == ExteriorArchetype.Knobbly ? 2.4f : 2.0f;
                _bumpAmp = g.Exterior == ExteriorArchetype.Rounded ? 0.02f : 0.03f;
                _wallAmp = g.Mineral == MineralId.Agate ? 0.02f : 0.05f;
                _angular = g.Exterior == ExteriorArchetype.Angular;
                // cauliflower knobs: rounded bulges packed over the rind (billow noise), strongest on knobbly rocks,
                // a faint undulation on rounded ones, none on angular fracture-faced rough
                _billowFreq = g.Exterior == ExteriorArchetype.Knobbly ? 2.4f : 1.8f;
                _billowAmp = _angular ? 0f : (g.Exterior == ExteriorArchetype.Knobbly ? 0.1f : 0.05f) * Mathf.Lerp(0.6f, 1.4f, g.ExteriorRoughness);
                if (_angular)
                {
                    var frng = new SeededRandom(SeededRandom.Combine(g.Seed, 18));
                    int k = 9 + frng.Range(0, 5);
                    _facetN = new Vector3[k];
                    _facetH = new float[k];
                    for (int i = 0; i < k; i++)
                    {
                        _facetN[i] = frng.OnUnitSphere();
                        _facetH[i] = frng.Range(0.8f, 0.97f);   // fraction of the ellipsoid radius along the facet normal
                    }
                }
                var mrng = new SeededRandom(SeededRandom.Combine(g.Seed, 19));
                _leanAxis = mrng.OnUnitSphere();
                _asym = mrng.Range(0.03f, 0.10f);
                _macroAmp = 0.09f * Mathf.Lerp(0.6f, 1.3f, g.ExteriorRoughness);
                _pitAmp = 0.045f * Mathf.Lerp(0.5f, 1.2f, g.Weathering);
                if (!_angular)
                {
                    int k = 2 + mrng.Range(0, 3);
                    _flatN = new Vector3[k];
                    _flatH = new float[k];
                    for (int i = 0; i < k; i++)
                    {
                        var n = mrng.OnUnitSphere();
                        if (i == 0) n = new Vector3(n.x * 0.35f, -Mathf.Abs(n.y) - 0.6f, n.z * 0.35f).normalized;   // the bed it lay in: underneath
                        _flatN[i] = n;
                        _flatH[i] = mrng.Range(0.86f, 0.95f);
                    }
                }
            }

            public float Outer(Vector3 d)
            {
                var a = _g.Axes;
                float e = 1f / Mathf.Sqrt((d.x * d.x) / (a.x * a.x) + (d.y * d.y) / (a.y * a.y) + (d.z * d.z) / (a.z * a.z));
                float lump;
                if (_angular)
                {
                    lump = (_lump.Fbm(d * _lumpFreq + _off1, 2) - 0.5f) * 0.5f;
                    // clip by the fracture planes: the surface along d is the nearest plane the ray meets
                    float rLimit = float.MaxValue;
                    for (int i = 0; i < _facetN.Length; i++)
                    {
                        float dn = Vector3.Dot(d, _facetN[i]);
                        if (dn <= 0.05f) continue;
                        var nn = _facetN[i];
                        float en = 1f / Mathf.Sqrt((nn.x * nn.x) / (a.x * a.x) + (nn.y * nn.y) / (a.y * a.y) + (nn.z * nn.z) / (a.z * a.z));
                        float planeR = _facetH[i] * en / dn;
                        if (planeR < rLimit) rLimit = planeR;
                    }
                    e = Mathf.Min(e, rLimit);
                }
                else
                {
                    lump = _lump.Fbm(d * _lumpFreq + _off1, 2) * 1.6f;
                    // soft flat spots: a few planes shave the ellipsoid without a sharp arris (a rounded nodule that
                    // grew against its bed and got tumbled in a stream keeps flats, not edges)
                    if (_flatN != null)
                        for (int i = 0; i < _flatN.Length; i++)
                        {
                            float dn = Vector3.Dot(d, _flatN[i]);
                            if (dn <= 0.2f) continue;
                            var nn = _flatN[i];
                            float en = 1f / Mathf.Sqrt((nn.x * nn.x) / (a.x * a.x) + (nn.y * nn.y) / (a.y * a.y) + (nn.z * nn.z) / (a.z * a.z));
                            float planeR = _flatH[i] * en / dn;
                            e = SoftMin(e, planeR, 0.08f * e);
                        }
                }
                float bump = _bump.Sample(d * _bumpFreq + _off2);
                float term = 1f + _lumpAmp * lump + _bumpAmp * bump;
                // macro asymmetry and a broad low swell, then erosion dents
                term += _asym * Vector3.Dot(d, _leanAxis) + _macroAmp * (_lump.Fbm(d * 0.75f + _off3, 1) - 0.5f);
                float pitN = _bump.Sample(d * 2.2f + _off4 * 0.7f);
                term -= _pitAmp * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((pitN - 0.62f) / 0.14f));
                if (_billowAmp > 0f)
                {
                    // squared noise makes rounded domes with soft hollows between them (a botryoidal rind) and stays
                    // smooth: an |n| crease or a finer octave would alias into ridges along the quad splits
                    float bn = _billow.Sample(d * _billowFreq + _off4) * 2f - 1f;
                    float b1 = bn * bn;
                    term += _billowAmp * 1.3f * (b1 - 0.4f);
                }
                return _g.Size * e * Mathf.Max(0.55f, term);
            }

            /// <summary>
            /// The hammer break is not a plane: the fracture face rises and dips between the rind and the cavity
            /// edge (conchoidal), the same surface on both halves so the closed rock still mates. Zero at both edges.
            /// </summary>
            public float FractureBulge(float lon, float t)
            {
                float env = Mathf.Sin(t * Mathf.PI);
                float n = _fracture.Sample(Mathf.Cos(lon) * 2.4f + 11f, Mathf.Sin(lon) * 2.4f + 5f, t * 2.5f + 2.2f) * 2f - 1f;
                float n2 = _fracture.Sample(Mathf.Cos(lon) * 7f + 3f, Mathf.Sin(lon) * 7f + 9f, t * 5f) * 2f - 1f;
                // V6 §17 conchoidal character: ripple arcs spreading from where the break started (a seeded longitude
                // on the rim), crisper toward the rind, and a ridged step or two between them. Shared by both halves.
                float impactLon = _off2.x * 0.1256f;   // seeded, 0..2pi-ish
                float dl = Mathf.Abs(Mathf.DeltaAngle(lon * Mathf.Rad2Deg, impactLon * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                float arc = Mathf.Sin((t * 1.4f + dl * 0.7f) * 17f + n * 1.5f);
                arc = Mathf.Sign(arc) * Mathf.Pow(Mathf.Abs(arc), 0.6f) * Mathf.Exp(-dl * 0.9f);
                float ridge = 1f - Mathf.Abs(_fracture.Sample(Mathf.Cos(lon) * 4f + 21f, Mathf.Sin(lon) * 4f + 13f, t * 3.2f) * 2f - 1f);
                ridge = ridge * ridge;
                return _g.Size * (0.02f * n + 0.006f * n2 + 0.009f * arc + 0.008f * (ridge - 0.5f)) * env;
            }

            public float Inner(Vector3 d, float outerR)
            {
                float best = 0f;
                var centers = _g.LobeCenters;
                var radii = _g.LobeRadii;
                for (int i = 0; i < centers.Length; i++)
                {
                    Vector3 c = centers[i];
                    float r = radii[i];
                    float b = Vector3.Dot(d, c);
                    float cc = Vector3.Dot(c, c) - r * r;
                    float disc = b * b - cc;
                    if (disc < 0f) continue;
                    float t = b + Mathf.Sqrt(disc);
                    if (t > best) best = t;
                }
                float wall = _wall.Fbm(d * 2.5f + _off3, 2) * _wallAmp * 1.6f;
                // the chalcedony lining bulges in rounded lobes into the cavity (botryoidal wall), finer on agate
                float lobes = Mathf.Abs(_wall.Sample(d * 5.5f + _off4) * 2f - 1f);
                float lobes2 = Mathf.Abs(_wall.Sample(d * 11f + _off2) * 2f - 1f);
                wall += (lobes - 0.5f) * (_g.Mineral == MineralId.Agate ? 0.035f : 0.075f) + (lobes2 - 0.5f) * 0.025f;
                float rIn = best * (1f + wall);
                // the shell is thicker in some sectors than others: the cut face shows it, and the hammer feels it
                float lon = Mathf.Atan2(d.z, d.x); if (lon < 0f) lon += Mathf.PI * 2f;
                float sectorF = lon / (Mathf.PI * 2f) * SpecimenGenerator.SeamSectors;
                int s0 = Mathf.FloorToInt(sectorF) % SpecimenGenerator.SeamSectors, s1 = (s0 + 1) % SpecimenGenerator.SeamSectors;
                float tf = sectorF - Mathf.Floor(sectorF);
                float thick = Mathf.Lerp(_g.SectorThicknessAt(s0), _g.SectorThicknessAt(s1), tf);
                float maxIn = outerR * (1f - _g.ShellThickness * thick);
                if (rIn > maxIn) rIn = maxIn;
                float minIn = 0.05f * outerR;
                if (rIn < minIn) rIn = minIn;
                return rIn;
            }

            public float RimJitter(float lon)
            {
                return _g.RimRoughness * _g.Size * _rim.Sample(Mathf.Cos(lon) * 2.3f + 3.1f, Mathf.Sin(lon) * 2.3f + 7.7f, 0.7f) * 2.4f;
            }
        }

        private static Vector3 Dir(float latRad, float lonRad, float sign)
        {
            float cl = Mathf.Cos(latRad);
            return new Vector3(Mathf.Cos(lonRad) * cl, sign * Mathf.Sin(latRad), Mathf.Sin(lonRad) * cl);
        }

        // ------------------------------------------------------------------------------------
        // Sawn pieces
        // ------------------------------------------------------------------------------------
        /// <summary>Sawn cut faces carry this in uv2.y so the shell shader draws a flat sawn face instead of a fracture.</summary>
        public const float SawnFlag = -2f;
        public const int PieceRows = 24;

        /// <summary>
        /// Build the part of the specimen between the piece's planes. The shell is walked in rings of constant height
        /// along the cut normal, each ring found by marching rays out of the ring's centre until they leave the shell
        /// (and the cavity), so any cut through the same rock lands on exactly the same surfaces the hammer halves show.
        /// Geometry is returned in the piece frame: the primary cut face points +Y so it rests face-up like a half.
        /// </summary>
        public static GeodeGeometry BuildPiece(SpecimenGeology g, PieceShape piece)
        {
            var shape = new Shape(g);
            Vector3 n = piece.Normal.normalized;
            Vector3 lobe0 = g.LobeCenters != null && g.LobeCenters.Length > 0 ? g.LobeCenters[0] : Vector3.zero;
            float lobeR = g.LobeRadii != null && g.LobeRadii.Length > 0 ? g.LobeRadii[0] : g.Size * g.CavityFraction;
            Vector3 u1 = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            Vector3 u2 = Vector3.Cross(n, u1).normalized;
            float topR = shape.Outer(n), botR = shape.Outer(-n);
            float hMin = piece.HasLo ? piece.Lo : -botR * 0.995f;
            float hMax = piece.HasHi ? piece.Hi : topR * 0.995f;
            if (hMax - hMin < 0.004f) { hMin = (hMin + hMax) * 0.5f - 0.002f; hMax = hMin + 0.004f; }
            bool poleBottom = !piece.HasLo, poleTop = !piece.HasHi;
            int N = Longitudes, M = PieceRows;
            var geo = new GeodeGeometry { Longitudes = N, IsPiece = true, Piece = piece };
            // rotate into the piece frame: primary cut face up
            var rot = Quaternion.FromToRotation(piece.UpNormal, Vector3.up);
            geo.PieceRotation = rot;

            // ring heights: eased toward a pole end so the cap stays round, linear between two cuts
            var heights = new float[M + 1];
            for (int k = 0; k <= M; k++)
            {
                float t = k / (float)M;
                float e;
                if (poleBottom && poleTop) e = 0.5f - 0.5f * Mathf.Cos(t * Mathf.PI);
                else if (poleBottom) e = 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
                else if (poleTop) e = Mathf.Sin(t * Mathf.PI * 0.5f);
                else e = t;
                heights[k] = Mathf.Lerp(hMin, hMax, e);
            }

            var outer = new Vector3[M + 1, N];
            var inner = new Vector3[M + 1, N];
            var hasCav = new bool[M + 1];
            var ringCenter = new Vector3[M + 1];
            bool anyCavity = false;
            for (int k = 0; k <= M; k++)
            {
                float h = heights[k];
                // ring centre: on the plane, pulled toward the main lobe where the shell is wide, on the axis near the ends
                Vector3 F = n * h;
                Vector3 lobeOff = lobe0 - n * Vector3.Dot(lobe0, n);
                float wide = Mathf.Clamp01(1f - Mathf.Abs(h) / (0.75f * (h >= 0f ? topR : botR)));
                Vector3 C = F + lobeOff * wide;
                // the centre must be inside the shell: pull it back toward the axis until it is
                for (int guard = 0; guard < 6 && C.magnitude > 1e-5f && C.magnitude >= shape.Outer(C.normalized) * 0.9f; guard++) C = Vector3.Lerp(C, F, 0.5f);
                ringCenter[k] = C;
                bool cavHere = false;
                for (int i = 0; i < N; i++)
                {
                    float lon = i / (float)N * Mathf.PI * 2f;
                    Vector3 u = Mathf.Cos(lon) * u1 + Mathf.Sin(lon) * u2;
                    float sOut = MarchOut(C, u, shape, false, g.Size * 3f);
                    outer[k, i] = C + u * sOut;
                    float sIn = MarchOut(C, u, shape, true, sOut);
                    if (sIn > 0f) cavHere = true;
                    inner[k, i] = C + u * Mathf.Max(0f, sIn);
                }
                hasCav[k] = cavHere;
                if (cavHere) anyCavity = true;
            }
            geo.HasCavity = anyCavity;

            var verts = new List<Vector3>((M + 1) * N * 2 + N * (RimRings + 1) * 2 + 8);
            var uvs = new List<Vector2>(verts.Capacity);
            var uv2 = new List<Vector2>(verts.Capacity);
            var cols = new List<Color>(verts.Capacity);
            var tris = new List<int>(verts.Capacity * 6);

            // ---- exterior ---------------------------------------------------------------------
            int extBase = verts.Count;
            for (int k = 0; k <= M; k++)
                for (int i = 0; i < N; i++)
                {
                    var p = outer[k, i];
                    verts.Add(rot * p);
                    uvs.Add(new Vector2(i / (float)N, k / (float)M));
                    uv2.Add(SpecimenSurfaceCoord(p));
                    cols.Add(new Color(1f, 0f, 0f, g.Weathering));
                }
            var extTris = new List<int>();
            RingStrips(extTris, extBase, N, M);
            if (poleBottom)
            {
                int pole = verts.Count;
                var pp = -n * botR; verts.Add(rot * pp); uvs.Add(new Vector2(0.5f, 0f)); uv2.Add(SpecimenSurfaceCoord(pp)); cols.Add(new Color(1f, 0f, 0f, g.Weathering));
                for (int i = 0; i < N; i++) { extTris.Add(extBase + i); extTris.Add(pole); extTris.Add(extBase + (i + 1) % N); }
            }
            if (poleTop)
            {
                int pole = verts.Count;
                var pp = n * topR; verts.Add(rot * pp); uvs.Add(new Vector2(0.5f, 1f)); uv2.Add(SpecimenSurfaceCoord(pp)); cols.Add(new Color(1f, 0f, 0f, g.Weathering));
                for (int i = 0; i < N; i++) { extTris.Add(extBase + M * N + i); extTris.Add(extBase + M * N + (i + 1) % N); extTris.Add(pole); }
            }
            OrientSurface(verts, extTris, (a, nn) => Vector3.Dot(nn, a - rot * ringCenter[M / 2]) > 0f);
            tris.AddRange(extTris);

            // ---- cavity ------------------------------------------------------------------------
            if (anyCavity)
            {
                int cavBase = verts.Count;
                for (int k = 0; k <= M; k++)
                    for (int i = 0; i < N; i++)
                    {
                        var p = inner[k, i];
                        verts.Add(rot * p);
                        uvs.Add(new Vector2(i / (float)N, k / (float)M));
                        uv2.Add(SpecimenSurfaceCoord(p));
                        cols.Add(new Color(0f, 1f, 0f, Mathf.Abs(k / (float)M - 0.5f) * 2f));
                    }
                var cavTris = new List<int>();
                RingStrips(cavTris, cavBase, N, M);
                // pole ends of the cavity close on the cavity's own pole point
                if (poleBottom)
                {
                    int pole = verts.Count;
                    var pp = -n * shape.Inner(-n, botR); verts.Add(rot * pp); uvs.Add(new Vector2(0.5f, 0f)); uv2.Add(SpecimenSurfaceCoord(pp)); cols.Add(new Color(0f, 1f, 0f, 1f));
                    for (int i = 0; i < N; i++) { cavTris.Add(cavBase + i); cavTris.Add(pole); cavTris.Add(cavBase + (i + 1) % N); }
                }
                if (poleTop)
                {
                    int pole = verts.Count;
                    var pp = n * shape.Inner(n, topR); verts.Add(rot * pp); uvs.Add(new Vector2(0.5f, 1f)); uv2.Add(SpecimenSurfaceCoord(pp)); cols.Add(new Color(0f, 1f, 0f, 1f));
                    for (int i = 0; i < N; i++) { cavTris.Add(cavBase + M * N + i); cavTris.Add(cavBase + M * N + (i + 1) % N); cavTris.Add(pole); }
                }
                OrientSurface(verts, cavTris, (a, nn) => Vector3.Dot(nn, a - rot * lobe0) < 0f);
                tris.AddRange(cavTris);
            }

            // ---- cut faces: flat rings from the outer edge in to the cavity edge (or the centre) -------------
            float faceArea = 0f;
            if (piece.HasHi) faceArea = Mathf.Max(faceArea, CutFace(verts, uvs, uv2, cols, tris, outer, inner, ringCenter, M, N, rot, n, +1f, hasCav[M] && anyCavity));
            if (piece.HasLo) CutFace(verts, uvs, uv2, cols, tris, outer, inner, ringCenter, 0, N, rot, n, -1f, hasCav[0] && anyCavity);

            var half = new GeodeHalfGeometry
            {
                IsTop = false,
                Vertices = verts.ToArray(), UVs = uvs.ToArray(), UV2 = uv2.ToArray(), Colors = cols.ToArray(), Triangles = tris.ToArray(),
                EquatorOuterRadius = new float[N], EquatorY = new float[N],
            };
            geo.Bottom = half;
            geo.Top = null;

            // ---- hull points for the collider (a coarse subset of the exterior and the cut rings) -----------
            {
                var hull = new List<Vector3>();
                for (int k = 0; k <= M; k += 2) for (int i = 0; i < N; i += 4) hull.Add(rot * outer[k, i]);
                for (int i = 0; i < N; i += 4) { hull.Add(rot * outer[M, i]); hull.Add(rot * outer[0, i]); }
                if (poleBottom) hull.Add(rot * (-n * botR));
                if (poleTop) hull.Add(rot * (n * topR));
                geo.HullPoints = hull.ToArray();
                // clip planes in the piece frame
                bool upIsN = Vector3.Dot(piece.UpNormal, n) > 0f;
                geo.ClipTopY = piece.HasHi && upIsN ? piece.Hi : piece.HasLo && !upIsN ? -piece.Lo : float.NaN;
                geo.ClipBottomY = piece.IsSlab ? (upIsN ? piece.Lo : -piece.Hi) : float.NaN;
            }
            float maxR = 0f, sumEq = 0f, sumCav = 0f;
            int mid = M / 2;
            for (int i = 0; i < N; i++) { sumEq += (outer[mid, i] - ringCenter[mid]).magnitude; sumCav += (inner[mid, i] - ringCenter[mid]).magnitude; }
            foreach (var v in half.Vertices) maxR = Mathf.Max(maxR, v.magnitude);
            geo.MaxRadius = maxR;
            geo.MeanEquatorRadius = sumEq / N;
            geo.MeanCavityRadius = sumCav / N;
            float lowest = float.MaxValue, highest = float.MinValue;
            foreach (var v in half.Vertices) { lowest = Mathf.Min(lowest, v.y); highest = Mathf.Max(highest, v.y); }
            geo.BottomY = lowest; geo.TopY = highest;

            // ---- crystals: those rooted inside the slab, rotated into the piece frame; the truncated ones marked ----
            var all = PlaceCrystals(g, shape, MeanCavity(g, shape));
            var kept = new List<CrystalInstance>();
            float wAll = 0f, wKept = 0f;
            foreach (var c in all)
            {
                float w = c.Height * c.Height * (c.Centerpiece ? 4f : 1f);
                wAll += w;
                float d = Vector3.Dot(c.Position, n);
                if ((piece.HasLo && d < piece.Lo) || (piece.HasHi && d > piece.Hi)) continue;
                var inst = c;
                inst.Position = rot * c.Position;
                inst.Rotation = rot * c.Rotation;
                // tip past a cut plane: the saw took the top off it
                Vector3 tip = c.Position + (c.Rotation * Vector3.up) * c.Height;
                float dt = Vector3.Dot(tip, n);
                bool cut = (piece.HasHi && dt > piece.Hi) || (piece.HasLo && dt < piece.Lo);
                inst.Truncated = cut;
                wKept += cut ? w * 0.4f : w;
                kept.Add(inst);
            }
            geo.Crystals = kept;
            geo.RetainedCrystalFraction = wAll > 0f ? wKept / wAll : 1f;

            // ---- what the primary face shows -----------------------------------------------------------
            {
                int k = piece.HasHi ? M : 0;
                float h = heights[k];
                float dLobe = Vector3.Dot(lobe0, n) - h;
                float opening = hasCav[k] && lobeR > 0f ? Mathf.Sqrt(Mathf.Max(0f, lobeR * lobeR - dLobe * dLobe)) / lobeR : 0f;
                geo.CavityOpening = Mathf.Clamp01(opening);
                geo.CutSymmetry = Mathf.Clamp01(1f - Mathf.Abs(dLobe) / Mathf.Max(0.001f, lobeR));
                float area = 0f;
                for (int i = 0; i < N; i++) { float ro = (outer[k, i] - ringCenter[k]).magnitude; area += ro * ro; }
                area *= Mathf.PI / N;
                geo.FaceAreaFraction = Mathf.Clamp01(area / (Mathf.PI * g.Size * g.Size));
            }
            return geo;
        }

        private static float MeanCavity(SpecimenGeology g, Shape shape)
        {
            float sumCav = 0f;
            for (int i = 0; i < Longitudes; i++)
            {
                float lon = i / (float)Longitudes * Mathf.PI * 2f;
                var d = Dir(0.3f, lon, -1f);
                sumCav += shape.Inner(d, shape.Outer(d));
            }
            return sumCav / Longitudes;
        }

        /// <summary>Longitude fraction and signed latitude fraction of a specimen-frame point, the overlay's surface coordinates.</summary>
        private static Vector2 SpecimenSurfaceCoord(Vector3 p)
        {
            float lon = Mathf.Atan2(p.z, p.x);
            float u = (lon < 0f ? lon + Mathf.PI * 2f : lon) / (Mathf.PI * 2f);
            float len = Mathf.Max(0.0001f, p.magnitude);
            float v = Mathf.Asin(Mathf.Clamp(p.y / len, -1f, 1f)) / (Mathf.PI * 0.5f);
            return new Vector2(u, v);
        }

        /// <summary>Distance along a ray from an interior point to where it leaves the shell (or, for cavity=true, the cavity; 0 if the start is outside it).</summary>
        private static float MarchOut(Vector3 from, Vector3 u, Shape shape, bool cavity, float maxS)
        {
            float f0 = SurfaceGap(from, shape, cavity);
            if (f0 >= 0f) return 0f;
            float lo = 0f, hi = Mathf.Max(0.002f, maxS);
            if (SurfaceGap(from + u * hi, shape, cavity) < 0f) return hi;
            for (int it = 0; it < 18; it++)
            {
                float mid = (lo + hi) * 0.5f;
                if (SurfaceGap(from + u * mid, shape, cavity) < 0f) lo = mid; else hi = mid;
            }
            return (lo + hi) * 0.5f;
        }

        /// <summary>Negative inside the surface, positive outside.</summary>
        private static float SurfaceGap(Vector3 p, Shape shape, bool cavity)
        {
            float m = p.magnitude;
            if (m < 1e-5f) return cavity ? -1f : -1f;
            var d = p / m;
            float ro = shape.Outer(d);
            return cavity ? m - shape.Inner(d, ro) : m - ro;
        }

        private static void RingStrips(List<int> tris, int baseIndex, int N, int M)
        {
            for (int k = 0; k < M; k++)
                for (int i = 0; i < N; i++)
                {
                    int i1 = (i + 1) % N;
                    int a = baseIndex + k * N + i, b = baseIndex + k * N + i1;
                    int c = baseIndex + (k + 1) * N + i1, d = baseIndex + (k + 1) * N + i;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(a); tris.Add(c); tris.Add(d);
                }
        }

        /// <summary>A sawn face at ring k: RimRings flat rings from the outer edge to the cavity edge (or to the centre point). Returns its area.</summary>
        private static float CutFace(List<Vector3> verts, List<Vector2> uvs, List<Vector2> uv2, List<Color> cols, List<int> tris,
            Vector3[,] outer, Vector3[,] inner, Vector3[] centers, int k, int N, Quaternion rot, Vector3 n, float sign, bool cavity)
        {
            int rimBase = verts.Count;
            float area = 0f;
            for (int b = 0; b <= RimRings; b++)
            {
                float t = b / (float)RimRings;
                for (int i = 0; i < N; i++)
                {
                    Vector3 o = outer[k, i];
                    Vector3 q = cavity ? inner[k, i] : centers[k];
                    Vector3 p = Vector3.Lerp(o, q, t);
                    verts.Add(rot * p);
                    uvs.Add(new Vector2(i / (float)N, t));
                    uv2.Add(new Vector2(i / (float)N, SawnFlag));
                    cols.Add(new Color(0f, 0f, 1f, t));
                    if (b == 0) { float ro = (o - centers[k]).magnitude, ri = (q - centers[k]).magnitude; area += (ro * ro - ri * ri); }
                }
            }
            area *= Mathf.PI / N;
            var rimTris = new List<int>();
            for (int b = 0; b < RimRings; b++)
                for (int i = 0; i < N; i++)
                {
                    int i1 = (i + 1) % N;
                    int a = rimBase + b * N + i, bb = rimBase + b * N + i1;
                    int c = rimBase + (b + 1) * N + i1, dd = rimBase + (b + 1) * N + i;
                    rimTris.Add(a); rimTris.Add(bb); rimTris.Add(c);
                    rimTris.Add(a); rimTris.Add(c); rimTris.Add(dd);
                }
            // faces point away from the piece: the top ring's face along +normal, the bottom ring's along -normal
            // (both expressed in the piece frame through rot)
            Vector3 faceN = rot * (n * sign);
            OrientSurface(verts, rimTris, (a, nn) => Vector3.Dot(nn, faceN) > 0f);
            tris.AddRange(rimTris);
            return Mathf.Max(0f, area);
        }

        public static GeodeGeometry Build(SpecimenGeology g)
        {
            var shape = new Shape(g);
            var geo = new GeodeGeometry { Longitudes = Longitudes };
            geo.Top = BuildHalf(g, shape, +1f);
            geo.Bottom = BuildHalf(g, shape, -1f);

            float maxR = 0f, sumEq = 0f, sumCav = 0f;
            for (int i = 0; i < Longitudes; i++)
            {
                sumEq += geo.Bottom.EquatorOuterRadius[i];
                float lon = i / (float)Longitudes * Mathf.PI * 2f;
                var d = Dir(0.3f, lon, -1f);
                float o = shape.Outer(d);
                sumCav += shape.Inner(d, o);
            }
            foreach (var v in geo.Bottom.Vertices) maxR = Mathf.Max(maxR, v.magnitude);
            foreach (var v in geo.Top.Vertices) maxR = Mathf.Max(maxR, v.magnitude);
            geo.MaxRadius = maxR;
            geo.MeanEquatorRadius = sumEq / Longitudes;
            geo.MeanCavityRadius = sumCav / Longitudes;
            geo.BottomY = geo.Bottom.PoleY;
            geo.TopY = geo.Top.PoleY;
            geo.Crystals = PlaceCrystals(g, shape, geo.MeanCavityRadius);
            return geo;
        }

        private static GeodeHalfGeometry BuildHalf(SpecimenGeology g, Shape shape, float s)
        {
            int N = Longitudes, M = Latitudes;
            var verts = new List<Vector3>(N * (M + 1) * 2 + N * (RimRings + 1) + 2);
            var uvs = new List<Vector2>(verts.Capacity);
            var uv2 = new List<Vector2>(verts.Capacity);
            var cols = new List<Color>(verts.Capacity);
            var tris = new List<int>(verts.Capacity * 6);
            var half = new GeodeHalfGeometry { IsTop = s > 0, EquatorOuterRadius = new float[N], EquatorY = new float[N] };

            var jitter = new float[N];
            for (int i = 0; i < N; i++)
            {
                float lon = i / (float)N * Mathf.PI * 2f;
                jitter[i] = shape.RimJitter(lon);   // same for both halves so the closed rock mates seamlessly
                half.EquatorY[i] = jitter[i];
            }

            // ---- exterior -------------------------------------------------------------------
            int extBase = verts.Count;
            for (int k = 0; k < M; k++)
            {
                float lat = k / (float)M * Mathf.PI * 0.5f;
                for (int i = 0; i < N; i++)
                {
                    float lon = i / (float)N * Mathf.PI * 2f;
                    var d = Dir(lat, lon, s);
                    float r = shape.Outer(d);
                    var p = d * r;
                    if (k == 0) { p.y = jitter[i]; half.EquatorOuterRadius[i] = r; }
                    verts.Add(p);
                    uvs.Add(new Vector2(i / (float)N, k / (float)M));
                    uv2.Add(new Vector2(i / (float)N, s * k / (float)M));
                    cols.Add(new Color(1f, 0f, 0f, g.Weathering));
                }
            }
            {
                var d = new Vector3(0f, s, 0f);
                var p = d * shape.Outer(d);
                half.PoleY = p.y;
                verts.Add(p);
                uvs.Add(new Vector2(0.5f, 1f));
                uv2.Add(new Vector2(0.5f, s));
                cols.Add(new Color(1f, 0f, 0f, g.Weathering));
            }
            int extPole = verts.Count - 1;
            var extTris = new List<int>();
            GridTriangles(extTris, extBase, extPole, N, M);
            OrientSurface(verts, extTris, (a, n) => Vector3.Dot(n, a) > 0f);
            tris.AddRange(extTris);

            // ---- cavity ---------------------------------------------------------------------
            int cavBase = verts.Count;
            for (int k = 0; k < M; k++)
            {
                float lat = k / (float)M * Mathf.PI * 0.5f;
                for (int i = 0; i < N; i++)
                {
                    float lon = i / (float)N * Mathf.PI * 2f;
                    var d = Dir(lat, lon, s);
                    float ro = shape.Outer(d);
                    float r = shape.Inner(d, ro);
                    var p = d * r;
                    if (k == 0) p.y = jitter[i];
                    verts.Add(p);
                    uvs.Add(new Vector2(i / (float)N, k / (float)M));
                    uv2.Add(new Vector2(i / (float)N, s * k / (float)M));
                    cols.Add(new Color(0f, 1f, 0f, k / (float)M));
                }
            }
            {
                var d = new Vector3(0f, s, 0f);
                float ro = shape.Outer(d);
                verts.Add(d * shape.Inner(d, ro));
                uvs.Add(new Vector2(0.5f, 1f));
                uv2.Add(new Vector2(0.5f, s));
                cols.Add(new Color(0f, 1f, 0f, 1f));
            }
            int cavPole = verts.Count - 1;
            var cavTris = new List<int>();
            GridTriangles(cavTris, cavBase, cavPole, N, M);
            OrientSurface(verts, cavTris, (a, n) => Vector3.Dot(n, a) < 0f);
            tris.AddRange(cavTris);

            // ---- rim (cut face) -------------------------------------------------------------
            int rimBase = verts.Count;
            for (int b = 0; b <= RimRings; b++)
            {
                float t = b / (float)RimRings;
                for (int i = 0; i < N; i++)
                {
                    float lon = i / (float)N * Mathf.PI * 2f;
                    var d = Dir(0f, lon, s);
                    float ro = shape.Outer(d);
                    float ri = shape.Inner(d, ro);
                    float r = Mathf.Lerp(ro, ri, t);
                    verts.Add(new Vector3(Mathf.Cos(lon) * r, jitter[i] + shape.FractureBulge(lon, t), Mathf.Sin(lon) * r));
                    uvs.Add(new Vector2(i / (float)N, t));
                    uv2.Add(new Vector2(i / (float)N, 0f));
                    cols.Add(new Color(0f, 0f, 1f, t));
                }
            }
            var rimTris = new List<int>();
            for (int b = 0; b < RimRings; b++)
            {
                for (int i = 0; i < N; i++)
                {
                    int i1 = (i + 1) % N;
                    int a = rimBase + b * N + i, bb = rimBase + b * N + i1;
                    int c = rimBase + (b + 1) * N + i1, dd = rimBase + (b + 1) * N + i;
                    rimTris.Add(a); rimTris.Add(bb); rimTris.Add(c);
                    rimTris.Add(a); rimTris.Add(c); rimTris.Add(dd);
                }
            }
            float sign = s;
            OrientSurface(verts, rimTris, (a, n) => n.y * -sign > 0f);
            tris.AddRange(rimTris);

            half.Vertices = verts.ToArray();
            half.UVs = uvs.ToArray();
            half.UV2 = uv2.ToArray();
            half.Colors = cols.ToArray();
            half.Triangles = tris.ToArray();
            return half;
        }

        private static void GridTriangles(List<int> tris, int baseIndex, int pole, int N, int M)
        {
            for (int k = 0; k < M - 1; k++)
            {
                for (int i = 0; i < N; i++)
                {
                    int i1 = (i + 1) % N;
                    int a = baseIndex + k * N + i, b = baseIndex + k * N + i1;
                    int c = baseIndex + (k + 1) * N + i1, d = baseIndex + (k + 1) * N + i;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(a); tris.Add(c); tris.Add(d);
                }
            }
            for (int i = 0; i < N; i++)
            {
                int i1 = (i + 1) % N;
                tris.Add(baseIndex + (M - 1) * N + i);
                tris.Add(baseIndex + (M - 1) * N + i1);
                tris.Add(pole);
            }
        }

        /// <summary>Flip winding of a whole surface if its first non-degenerate triangle faces the wrong way.</summary>
        private static void OrientSurface(List<Vector3> verts, List<int> tris, Func<Vector3, Vector3, bool> isCorrect)
        {
            for (int t = 0; t + 2 < tris.Count; t += 3)
            {
                Vector3 a = verts[tris[t]], b = verts[tris[t + 1]], c = verts[tris[t + 2]];
                Vector3 n = Vector3.Cross(b - a, c - a);
                if (n.sqrMagnitude < 1e-12f) continue;
                Vector3 centroid = (a + b + c) / 3f;
                if (isCorrect(centroid, n)) return;
                for (int u = 0; u < tris.Count; u += 3) (tris[u + 1], tris[u + 2]) = (tris[u + 2], tris[u + 1]);
                return;
            }
        }

        // ------------------------------------------------------------------------------------
        // Crystal placement
        // ------------------------------------------------------------------------------------
        private struct Cell
        {
            public float Sign; public int K; public int I; public Vector3 Dir; public Vector3 Pos; public float Width; public float Lat;
         public float Thickness; }

        private static List<CrystalInstance> PlaceCrystals(SpecimenGeology g, Shape shape, float cavR)
        {
            var fam = g.Family;
            var rng = new SeededRandom(SeededRandom.Combine(g.Seed, 0xC7));
            var list = new List<CrystalInstance>(400);
            bool druzy = g.IsDruzy;
            var style = druzy ? PlacementStyle.Carpet : fam.Placement;
            // a carpet packs hundreds of similar points shoulder to shoulder (V6 §19), so it samples a finer cell grid
            int N = style == PlacementStyle.Carpet && !druzy ? 56 : PlaceLon, M = style == PlacementStyle.Carpet && !druzy ? 20 : PlaceLat;
            // patches: a slow noise over the cavity makes some regions crowd and others thin out, so the carpet
            // never reads as an even grid
            var patch = new Noise3D(SeededRandom.Combine(g.Seed, 0xD1));

            // Candidate cells on both cavity walls (bottom first).
            var cells = new List<Cell>(N * M * 2);
            foreach (float s in new[] { -1f, 1f })
            {
                for (int k = 0; k < M; k++)
                {
                    float lat = (k + 0.5f) / M * Mathf.PI * 0.5f;
                    float cl = Mathf.Cos(lat);
                    int nEff = Mathf.Max(6, Mathf.RoundToInt(N * cl));  // fewer cells toward the pole
                    for (int i = 0; i < nEff; i++)
                    {
                        float lon = (i + 0.5f + rng.Range(-0.5f, 0.5f)) / nEff * Mathf.PI * 2f;
                        float latJ = lat + rng.Range(-0.5f, 0.5f) / M * Mathf.PI * 0.5f;
                        var d = Dir(latJ, lon, s);
                        float ro = shape.Outer(d);
                        float ri = shape.Inner(d, ro);
                        cells.Add(new Cell { Sign = s, K = k, I = i, Dir = d, Pos = d * ri, Width = 2f * Mathf.PI * ri * cl / nEff, Lat = (k + 0.5f) / M, Thickness = ro - ri });
                    }
                }
            }

            // V6 §16: crystals line a cavity that still reads deep: the family's scale range is taken at 0.68 of its V5 value
            // and the growth cores / giants bring the big ones back where they belong; a carpet's points are smaller still
            float baseH = cavR * Mathf.Lerp(fam.ScaleMin, fam.ScaleMax, g.CrystalScale) * (style == PlacementStyle.Carpet && !druzy ? 0.56f : 0.68f);
            float density = Mathf.Lerp(fam.DensityMin, fam.DensityMax, g.CrystalDensity);
            var palette = g.Palette;

            // cluster centres: clustered placement crowds round them; every other style still grows from a few
            // growth centres (V6 §18-19: irregular clustering, bigger crystals with buried bases in the cores, small
            // shallow ones on the fringes, never an even carpet)
            var clusterDirs = new List<Vector3>();
            {
                int c = style == PlacementStyle.Clustered ? 2 + Mathf.RoundToInt(density * 4f) : 3 + rng.Range(0, 3);
                for (int i = 0; i < c; i++) clusterDirs.Add(cells[rng.Range(0, cells.Count)].Dir);
            }
            float ClusterWeight(Vector3 dir, float sigma)
            {
                float best = 0f;
                foreach (var cd in clusterDirs)
                {
                    float ang = Mathf.Acos(Mathf.Clamp(Vector3.Dot(cd, dir), -1f, 1f));
                    best = Mathf.Max(best, Mathf.Exp(-(ang * ang) / (sigma * sigma)));
                }
                return best;
            }

            var placed = new List<CrystalInstance>();
            float symmetry = g.Symmetry;

            foreach (var cell in cells)
            {
                float p;
                CrystalArchetype arch;
                float h;
                float inset = 0.06f;
                float tilt = fam.TiltDeg;
                bool isTile = false;

                if (druzy)
                {
                    p = 0.97f;
                    isTile = true;
                    arch = rng.Chance(0.06f) ? CrystalArchetype.QuartzPoint : CrystalArchetype.DruzyTile;
                    h = arch == CrystalArchetype.DruzyTile ? cell.Width * 1.15f : baseH * rng.Range(1.6f, 2.6f);
                    if (arch == CrystalArchetype.QuartzPoint) isTile = false;
                }
                else
                {
                    switch (style)
                    {
                        case PlacementStyle.Carpet: p = Mathf.Min(1f, density * 1.15f); break;
                        case PlacementStyle.Clustered:
                        {
                            float best = ClusterWeight(cell.Dir, 0.45f);
                            p = density * best * 1.3f + 0.03f;
                            break;
                        }
                        case PlacementStyle.Scattered: p = density * 0.55f; break;
                        case PlacementStyle.Embedded: p = density * 0.6f; inset = -0.3f; tilt = 60f; break;
                        case PlacementStyle.Sprays: p = density * 0.28f; break;
                        default: p = cell.Lat > 0.42f ? density : 0.02f; isTile = true; break; // Banded
                    }
                    arch = fam.Archetypes[rng.PickWeighted(fam.ArchetypeWeights)];
                    if (style == PlacementStyle.Sprays && arch == CrystalArchetype.Needle) p *= 0.8f;
                    // a carpet's points are alike in size (the odd giant and a few runts aside); other habits vary more
                    float var = style == PlacementStyle.Carpet
                        ? Mathf.Lerp(rng.Range(0.78f, 1.28f), rng.Range(0.9f, 1.1f), symmetry)
                        : Mathf.Lerp(rng.Range(0.55f, 1.55f), rng.Range(0.85f, 1.15f), symmetry);
                    // growth centres: cores carry the big crystals with their bases buried in the substrate, fringes
                    // the small shallow ones; the odd giant and plenty of runts break the even look
                    float core = ClusterWeight(cell.Dir, 0.6f);
                    // V6 §19: a carpet is mostly terminations (the prisms are buried in the crowd, only the tips stand
                    // proud); the full points stay in the growth cores, and a carpet grows nearly normal to the wall
                    if (style == PlacementStyle.Carpet && (arch == CrystalArchetype.QuartzPoint || arch == CrystalArchetype.QuartzStubby) && rng.Chance(Mathf.Lerp(0.75f, 0.25f, core)))
                        arch = CrystalArchetype.QuartzTermination;
                    if (style == PlacementStyle.Carpet) tilt = Mathf.Min(tilt, 12f);
                    if (style == PlacementStyle.Carpet)
                    {
                        var *= Mathf.Lerp(0.85f, 1.3f, core);
                        if (rng.Chance(0.04f)) var *= rng.Range(1.3f, 1.7f);
                        else if (rng.Chance(0.2f)) var *= rng.Range(0.6f, 0.8f);
                    }
                    else
                    {
                        var *= Mathf.Lerp(0.7f, 1.55f, core);
                        if (rng.Chance(0.07f)) var *= rng.Range(1.6f, 2.3f);
                        else if (rng.Chance(0.25f)) var *= rng.Range(0.45f, 0.7f);
                    }
                    inset = Mathf.Lerp(0.05f, 0.3f, core) * (style == PlacementStyle.Embedded ? -1f : 1f);
                    if (style == PlacementStyle.Embedded) inset = -0.3f;
                    h = baseH * var;
                    // a buried base may not push out through a thin shell (it would fail containment and leave the
                    // thin half bare): the burial is capped by the wall under this cell
                    if (inset > 0f) inset = Mathf.Min(inset, Mathf.Max(0.02f, cell.Thickness * 0.7f / Mathf.Max(0.001f, h)));
                    if (isTile)
                    {
                        h = cell.Width * rng.Range(1.05f, 1.3f);
                        if (arch == CrystalArchetype.QuartzPoint) { isTile = false; h = baseH * rng.Range(1.2f, 2.2f); }
                    }
                }

                // crowd / thin by the cavity patch field (tiles keep their carpet coverage)
                if (!isTile) p *= Mathf.Lerp(0.55f, 1.35f, patch.Fbm(cell.Dir * 2.2f, 2));
                if (!rng.Chance(p)) continue;

                float elong = isTile ? 1f : Mathf.Lerp(fam.ElongationMin, fam.ElongationMax, rng.NextFloat());
                float width = ArchetypeWidth(arch) * h / elong;
                float footprint = width * 0.5f;
                if (isTile) footprint = h * 0.42f;

                // spacing rejection (crystals may touch, tiles may overlap a little)
                bool blocked = false;
                float spacing = isTile ? 0.55f : style == PlacementStyle.Carpet ? 0.36f : 0.42f;   // carpet crystals touch
                for (int j = 0; j < placed.Count; j++)
                {
                    var o = placed[j];
                    float minD = (footprint + o.Footprint) * spacing;
                    if ((o.Position - cell.Pos).sqrMagnitude < minD * minD) { blocked = true; break; }
                }
                if (blocked) continue;

                var inst = MakeInstance(ref rng, g, palette, cell, arch, h, elong, inset, tilt, isTile, false);
                if (!IsInsideShell(shape, inst)) continue;
                inst.Fragility = fam.Fragility * Mathf.Lerp(0.8f, 1.3f, Mathf.Clamp01(h / Mathf.Max(0.001f, baseH) - 0.5f));
                placed.Add(inst);
            }

            // ---- centrepiece ------------------------------------------------------------------
            if (g.HasTrait(RareTrait.GiantCenterpiece))
            {
                // deepest cells of the bottom half
                Cell bestCell = default; bool found = false;
                for (int c = 0; c < cells.Count; c++)
                {
                    if (cells[c].Sign > 0) continue;
                    if (cells[c].K >= M - 3 && (!found || rng.Chance(0.3f))) { bestCell = cells[c]; found = true; }
                }
                if (found)
                {
                    float h = Mathf.Min(baseH * 2.6f, cavR * 1.15f);
                    var arch = fam.Archetypes[0];
                    float elong = Mathf.Lerp(fam.ElongationMin, fam.ElongationMax, 0.7f);
                    var inst = MakeInstance(ref rng, g, palette, bestCell, arch, h, elong, 0.08f, 6f, false, false);
                    inst.Centerpiece = true;
                    inst.Fragility = fam.Fragility * 1.4f;
                    placed.RemoveAll(o => (o.Position - inst.Position).sqrMagnitude < (o.Footprint + inst.Footprint * 0.9f) * (o.Footprint + inst.Footprint * 0.9f));
                    placed.Add(inst);
                }
            }

            // ---- secondary mineral -----------------------------------------------------------
            if (g.HasSecondary && placed.Count > 0)
            {
                var sec = g.SecondaryFamily;
                var secPal = sec.Palettes[rng.PickWeighted(sec.PaletteWeights)];
                bool overgrow = g.HasTrait(RareTrait.CrystalOnCrystal);
                int count = Mathf.RoundToInt(placed.Count * g.SecondaryAmount);
                float secH = cavR * Mathf.Lerp(sec.ScaleMin, sec.ScaleMax, Mathf.Clamp01(g.CrystalScale * 0.8f)) * 0.8f;
                var extra = new List<CrystalInstance>();
                for (int n = 0; n < count; n++)
                {
                    int idx = rng.Range(0, placed.Count);
                    var host = placed[idx];
                    if (host.Centerpiece || host.Secondary) continue;
                    var arch = sec.Archetypes[rng.PickWeighted(sec.ArchetypeWeights)];
                    if (arch == CrystalArchetype.DruzyTile || arch == CrystalArchetype.Botryoidal) arch = CrystalArchetype.QuartzPoint;
                    float h = secH * rng.Range(0.6f, 1.2f);
                    float elong = Mathf.Lerp(sec.ElongationMin, sec.ElongationMax, rng.NextFloat());
                    var cell = new Cell { Sign = host.TopHalf ? 1f : -1f, Dir = -(host.Rotation * Vector3.up), Pos = host.Position, Lat = host.Latitude, Width = host.Footprint * 2f };
                    var inst = MakeInstance(ref rng, g, secPal, cell, arch, h, elong, 0.05f, sec.TiltDeg, false, true);
                    inst.Fragility = sec.Fragility;
                    if (overgrow)
                    {
                        inst.Position = host.Position + (host.Rotation * Vector3.up) * (host.Height * rng.Range(0.35f, 0.75f));
                        extra.Add(inst);
                    }
                    else
                    {
                        placed[idx] = inst;
                    }
                }
                placed.AddRange(extra);
            }

            // stable ordering: bottom half first, then top; assign indices
            placed.Sort((a, b) =>
            {
                int c = a.TopHalf.CompareTo(b.TopHalf);
                if (c != 0) return c;
                c = a.Latitude.CompareTo(b.Latitude);
                if (c != 0) return c;
                return a.Azimuth.CompareTo(b.Azimuth);
            });
            for (int i = 0; i < placed.Count; i++)
            {
                var inst = placed[i];
                inst.Index = i;
                placed[i] = inst;
            }
            list.AddRange(placed);
            return list;
        }

        /// <summary>Reject crystals whose base, tip or lateral extent would exit the outer shell surface.</summary>
        private static bool IsInsideShell(Shape shape, CrystalInstance inst)
        {
            Vector3 axis = inst.Rotation * Vector3.up;
            Vector3 right = inst.Rotation * Vector3.right;
            Vector3 fwd = inst.Rotation * Vector3.forward;
            float r = inst.Footprint;
            Vector3 mid = inst.Position + axis * (inst.Height * 0.5f);
            Vector3[] samples =
            {
                inst.Position, inst.Position + axis * inst.Height,
                mid + right * r, mid - right * r, mid + fwd * r, mid - fwd * r,
                inst.Position + right * r, inst.Position - right * r, inst.Position + fwd * r, inst.Position - fwd * r,
            };
            foreach (var p in samples)
            {
                float m = p.magnitude;
                if (m < 1e-4f) continue;
                if (m > shape.Outer(p / m) * 0.965f) return false;
            }
            return true;
        }

        private static float ArchetypeWidth(CrystalArchetype a) => a switch
        {
            CrystalArchetype.QuartzPoint => 0.35f,
            CrystalArchetype.QuartzStubby => 0.72f,
            CrystalArchetype.QuartzTermination => 0.72f,
            CrystalArchetype.QuartzCluster => 0.62f,
            CrystalArchetype.Cube => 1.05f,
            CrystalArchetype.Octahedron => 1.3f,
            CrystalArchetype.Rhomb => 1.3f,
            CrystalArchetype.Dogtooth => 0.38f,
            CrystalArchetype.Nailhead => 1.0f,
            CrystalArchetype.Blade => 0.36f,
            CrystalArchetype.Needle => 0.07f,
            CrystalArchetype.Pyritohedron => 1.15f,
            CrystalArchetype.DruzyTile => 2.8f,
            CrystalArchetype.Botryoidal => 1.9f,
            CrystalArchetype.AragoniteSpray => 1.4f,
            CrystalArchetype.BarrelPrism => 1.05f,
            CrystalArchetype.Rosette => 1.9f,
            CrystalArchetype.Tetragonal => 0.4f,
            CrystalArchetype.Tetrahedron => 1.2f,
            CrystalArchetype.Sheaf => 1.0f,
            CrystalArchetype.Hopper => 1.05f,
            _ => 0.5f,
        };

        private static CrystalInstance MakeInstance(ref SeededRandom rng, SpecimenGeology g, MineralPalette palette, Cell cell,
            CrystalArchetype arch, float h, float elong, float inset, float tiltDeg, bool isTile, bool secondary)
        {
            Vector3 inward = -cell.Dir;
            var tiltVec = rng.OnUnitSphere() * Mathf.Tan(Mathf.Deg2Rad * Mathf.Min(80f, tiltDeg)) * rng.NextFloat();
            Vector3 axis = (inward + tiltVec).normalized;
            if (isTile) axis = inward;
            var rot = Quaternion.FromToRotation(Vector3.up, axis) * Quaternion.AngleAxis(rng.Range(0f, 360f), Vector3.up);
            float width = ArchetypeWidth(arch);
            float sx = h / elong, sz = h / elong;
            if (isTile) { sx = h; sz = h; }
            var pos = cell.Pos + axis * (-inset * h);
            float tintVar = 0.14f;
            float v1 = 1f + rng.Range(-tintVar, tintVar);
            float hueT = Mathf.Clamp01(0.5f + g.HueShift * 0.35f + rng.Range(-0.25f, 0.25f));
            var baseTint = Color.Lerp(palette.SurfaceA, palette.SurfaceB, hueT);
            // store tint as a multiplier relative to SurfaceA so the material colour still rules
            var tint = new Color(
                Mathf.Clamp(v1 * baseTint.r / Mathf.Max(0.05f, palette.SurfaceA.r), 0.5f, 1.5f),
                Mathf.Clamp(v1 * baseTint.g / Mathf.Max(0.05f, palette.SurfaceA.g), 0.5f, 1.5f),
                Mathf.Clamp(v1 * baseTint.b / Mathf.Max(0.05f, palette.SurfaceA.b), 0.5f, 1.5f), 1f);
            return new CrystalInstance
            {
                TopHalf = cell.Sign > 0f,
                Secondary = secondary,
                Archetype = arch,
                Position = pos,
                Rotation = rot,
                Scale = new Vector3(sx, h, sz),
                Tint = tint,
                Azimuth = Mathf.Atan2(cell.Pos.z, cell.Pos.x),
                Latitude = cell.Lat,
                Footprint = isTile ? h * 0.42f : width * h / elong * 0.5f,
                Height = h,
            };
        }
    }
}
