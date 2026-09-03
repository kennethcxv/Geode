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
    }

    /// <summary>
    /// Pure, deterministic geometry: two half shells (exterior + cut face + cavity wall) and a crystal
    /// placement list. No UnityEngine.Object allocation, so it is safe in EditMode tests and threads.
    /// </summary>
    public static class GeodeMeshBuilder
    {
        public const int Longitudes = 40;
        public const int Latitudes = 14;
        public const int RimRings = 4;

        private sealed class Shape
        {
            private readonly SpecimenGeology _g;
            private readonly Noise3D _lump, _bump, _wall, _rim;
            private readonly Vector3 _off1, _off2, _off3;
            private readonly float _lumpFreq, _lumpAmp, _bumpFreq, _bumpAmp, _wallAmp;
            private readonly bool _angular;

            public Shape(SpecimenGeology g)
            {
                _g = g;
                _lump = new Noise3D(SeededRandom.Combine(g.Seed, 11));
                _bump = new Noise3D(SeededRandom.Combine(g.Seed, 12));
                _wall = new Noise3D(SeededRandom.Combine(g.Seed, 13));
                _rim = new Noise3D(SeededRandom.Combine(g.Seed, 14));
                var rng = new SeededRandom(SeededRandom.Combine(g.Seed, 15));
                _off1 = new Vector3(rng.Range(0f, 50f), rng.Range(0f, 50f), rng.Range(0f, 50f));
                _off2 = new Vector3(rng.Range(0f, 50f), rng.Range(0f, 50f), rng.Range(0f, 50f));
                _off3 = new Vector3(rng.Range(0f, 50f), rng.Range(0f, 50f), rng.Range(0f, 50f));
                _lumpFreq = g.Exterior == ExteriorArchetype.Knobbly ? 3.4f : g.Exterior == ExteriorArchetype.Rounded ? 1.3f : 1.9f;
                _lumpAmp = g.ExteriorRoughness * 2.2f;
                _bumpFreq = g.Exterior == ExteriorArchetype.Knobbly ? 7f : 5f;
                _bumpAmp = g.Exterior == ExteriorArchetype.Rounded ? 0.025f : 0.045f;
                _wallAmp = g.Mineral == MineralId.Agate ? 0.02f : 0.05f;
                _angular = g.Exterior == ExteriorArchetype.Angular;
            }

            public float Outer(Vector3 d)
            {
                var a = _g.Axes;
                float e = 1f / Mathf.Sqrt((d.x * d.x) / (a.x * a.x) + (d.y * d.y) / (a.y * a.y) + (d.z * d.z) / (a.z * a.z));
                float lump;
                if (_angular)
                    lump = (_lump.Ridged(d * _lumpFreq + _off1, 2) * 2f - 1f) * 1.1f;
                else
                    lump = _lump.Fbm(d * _lumpFreq + _off1, 3) * 1.6f;
                float bump = _bump.Sample(d * _bumpFreq + _off2);
                float term = 1f + _lumpAmp * lump + _bumpAmp * bump;
                return _g.Size * e * Mathf.Max(0.55f, term);
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
                float rIn = best * (1f + wall);
                float maxIn = outerR * (1f - _g.ShellThickness);
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
                    verts.Add(new Vector3(Mathf.Cos(lon) * r, jitter[i], Mathf.Sin(lon) * r));
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
        }

        private static List<CrystalInstance> PlaceCrystals(SpecimenGeology g, Shape shape, float cavR)
        {
            var fam = g.Family;
            var rng = new SeededRandom(SeededRandom.Combine(g.Seed, 0xC7));
            var list = new List<CrystalInstance>(400);
            int N = Longitudes, M = Latitudes;

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
                        float lon = (i + 0.5f + rng.Range(-0.35f, 0.35f)) / nEff * Mathf.PI * 2f;
                        float latJ = lat + rng.Range(-0.3f, 0.3f) / M * Mathf.PI * 0.5f;
                        var d = Dir(latJ, lon, s);
                        float ro = shape.Outer(d);
                        float ri = shape.Inner(d, ro);
                        cells.Add(new Cell { Sign = s, K = k, I = i, Dir = d, Pos = d * ri, Width = 2f * Mathf.PI * ri * cl / nEff, Lat = (k + 0.5f) / M });
                    }
                }
            }

            bool druzy = g.IsDruzy;
            float baseH = cavR * Mathf.Lerp(fam.ScaleMin, fam.ScaleMax, g.CrystalScale);
            float density = Mathf.Lerp(fam.DensityMin, fam.DensityMax, g.CrystalDensity);
            var style = druzy ? PlacementStyle.Carpet : fam.Placement;
            var palette = g.Palette;

            // cluster centres for clustered placement
            var clusterDirs = new List<Vector3>();
            if (style == PlacementStyle.Clustered)
            {
                int c = 2 + Mathf.RoundToInt(density * 4f);
                for (int i = 0; i < c; i++) clusterDirs.Add(cells[rng.Range(0, cells.Count)].Dir);
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
                        case PlacementStyle.Carpet: p = density; break;
                        case PlacementStyle.Clustered:
                        {
                            float best = 0f;
                            foreach (var cd in clusterDirs)
                            {
                                float ang = Mathf.Acos(Mathf.Clamp(Vector3.Dot(cd, cell.Dir), -1f, 1f));
                                best = Mathf.Max(best, Mathf.Exp(-(ang * ang) / (0.45f * 0.45f)));
                            }
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
                    float var = Mathf.Lerp(rng.Range(0.55f, 1.55f), rng.Range(0.85f, 1.15f), symmetry);
                    h = baseH * var;
                    if (isTile)
                    {
                        h = cell.Width * rng.Range(1.05f, 1.3f);
                        if (arch == CrystalArchetype.QuartzPoint) { isTile = false; h = baseH * rng.Range(1.2f, 2.2f); }
                    }
                }

                if (!rng.Chance(p)) continue;

                float elong = isTile ? 1f : Mathf.Lerp(fam.ElongationMin, fam.ElongationMax, rng.NextFloat());
                float width = ArchetypeWidth(arch) * h / elong;
                float footprint = width * 0.5f;
                if (isTile) footprint = h * 0.42f;

                // spacing rejection (crystals may touch, tiles may overlap a little)
                bool blocked = false;
                float spacing = isTile ? 0.55f : 0.5f;
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
