using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using GeodeEmpire.Core;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.EditorTools
{
    /// <summary>
    /// Gate A tooling: renders deterministic specimens under identical lighting into grid PNGs (plus a CSV describing
    /// each cell) so visual variety can be judged, not assumed. V4 adds same-family comparison sheets (ten of a
    /// family, then low / median / high / exceptional / damaged), size-class sheets at a fixed camera distance, and a
    /// numeric near-duplicate report.
    /// </summary>
    public static class ContactSheetGenerator
    {
        public const string OutputFolder = "Output";   // project-relative (git-ignored)

        /// <summary>One specimen to render: geology, condition, and an optional caption for the CSV.</summary>
        public sealed class Cell
        {
            public SpecimenGeology Geology;
            public SpecimenCondition Condition;
            public string Caption;
        }

        [MenuItem("GeodeEmpire/Contact Sheet/Interiors 200 (2 sheets)")]
        public static void Interiors200()
        {
            Generate(100, 1000, "contact_interiors_A", true);
            Generate(100, 1100, "contact_interiors_B", true);
        }

        [MenuItem("GeodeEmpire/Contact Sheet/Exteriors 100")]
        public static void Exteriors100() => Generate(100, 1000, "contact_exteriors", false);

        [MenuItem("GeodeEmpire/Contact Sheet/Family comparison (all)")]
        public static void FamilySheetsAll() { foreach (var f in MineralCatalog.All) FamilySheet(f.Id); }

        [MenuItem("GeodeEmpire/Contact Sheet/Size classes")]
        public static void SizeSheet() => SizeClasses();

        public static string Generate(int count, ulong firstSeed, string fileName, bool opened, int cell = 256, int cols = 10, ulong[] seeds = null)
        {
            var cells = new List<Cell>();
            for (int n = 0; n < count; n++)
            {
                ulong seed = seeds != null ? seeds[n] : firstSeed + (ulong)n;
                cells.Add(new Cell { Geology = SpecimenGenerator.Generate(seed), Condition = new SpecimenCondition { Opened = opened, Cleaned = 1f } });
            }
            return Render(cells, fileName, opened, cell, cols, 0f);
        }

        /// <summary>
        /// Same-family sheet: ten ordinary draws of the family (two rows), then a row of low / median / high /
        /// exceptional / damaged examples chosen by quality quantile. Interior and exterior versions.
        /// </summary>
        public static string FamilySheet(MineralId mineral, ulong firstSeed = 5000, int cell = 256)
        {
            var draws = new List<SpecimenGeology>();
            var all = new List<SpecimenGeology>();
            ulong seed = firstSeed;
            while (all.Count < 600 && seed < firstSeed + 60000UL)
            {
                var g = SpecimenGenerator.Generate(seed++);
                if (g.Mineral != mineral) continue;
                all.Add(g);
                if (draws.Count < 10) draws.Add(g);
            }
            // ranked by what the player is told: the appraised (pristine) value
            all.Sort((a, b) => a.BaseValue.CompareTo(b.BaseValue));
            SpecimenGeology Pick(float quantile) => all[Mathf.Clamp(Mathf.RoundToInt(quantile * (all.Count - 1)), 0, all.Count - 1)];
            var low = Pick(0.05f); var median = Pick(0.5f); var high = Pick(0.85f); var best = all[all.Count - 1];
            SpecimenGeology exceptional = best;
            for (int i = all.Count - 1; i >= 0; i--) if (all[i].Tier >= QualityTier.Exceptional) { exceptional = all[i]; break; }
            var cells = new List<Cell>();
            foreach (var g in draws) cells.Add(new Cell { Geology = g, Condition = new SpecimenCondition { Opened = true, Cleaned = 1f }, Caption = "draw" });
            cells.Add(new Cell { Geology = low, Condition = new SpecimenCondition { Opened = true, Cleaned = 1f }, Caption = "low" });
            cells.Add(new Cell { Geology = median, Condition = new SpecimenCondition { Opened = true, Cleaned = 1f }, Caption = "median" });
            cells.Add(new Cell { Geology = high, Condition = new SpecimenCondition { Opened = true, Cleaned = 1f }, Caption = "high" });
            cells.Add(new Cell { Geology = exceptional, Condition = new SpecimenCondition { Opened = true, Cleaned = 1f }, Caption = "exceptional" });
            cells.Add(new Cell { Geology = high, Condition = Damaged(high, 0.45f), Caption = "damaged" });
            string name = mineral.ToString().ToLower();
            string png = Render(cells, $"family_{name}_interior", true, cell, 5, 0f);
            var ext = new List<Cell>();
            foreach (var c in cells) ext.Add(new Cell { Geology = c.Geology, Condition = new SpecimenCondition { Opened = false, Cleaned = c.Caption == "damaged" ? 0f : 1f }, Caption = c.Caption });
            Render(ext, $"family_{name}_exterior", false, cell, 5, 0f);
            return png;
        }

        /// <summary>Four rows (small, medium, large, oversized) of exteriors at one fixed camera distance so size reads.</summary>
        public static string SizeClasses(ulong firstSeed = 7000, int perRow = 8, int cell = 256)
        {
            var rows = new List<SpecimenGeology>[4];
            for (int i = 0; i < 4; i++) rows[i] = new List<SpecimenGeology>();
            ulong seed = firstSeed;
            while (seed < firstSeed + 20000UL && (rows[0].Count < perRow || rows[1].Count < perRow || rows[2].Count < perRow || rows[3].Count < perRow))
            {
                var g = SpecimenGenerator.Generate(seed++);
                var r = rows[(int)g.SizeClass];
                if (r.Count < perRow) r.Add(g);
            }
            var cells = new List<Cell>();
            for (int i = 0; i < 4; i++) foreach (var g in rows[i]) cells.Add(new Cell { Geology = g, Condition = new SpecimenCondition { Opened = false, Cleaned = 0.5f }, Caption = ((SizeClass)i).ToString() });
            return Render(cells, "size_classes", false, cell, perRow, 0.62f);
        }

        /// <summary>A high-quality specimen with a patch of the carpet broken: chipped tips, broken points, stubs.</summary>
        public static SpecimenCondition Damaged(SpecimenGeology g, float fraction)
        {
            var geo = GeodeMeshBuilder.Build(g);
            var cond = new SpecimenCondition { Opened = true, Cleaned = 1f };
            cond.EnsureSize(geo.Crystals.Count);
            var rng = new SeededRandom(g.Seed ^ 0x5A5AUL);
            float centre = rng.Range(0f, Mathf.PI * 2f);
            foreach (var c in geo.Crystals)
            {
                float d = Mathf.Abs(Mathf.DeltaAngle(c.Azimuth * Mathf.Rad2Deg, centre * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                if (d > 1.1f * fraction * 2f) continue;
                float roll = rng.NextFloat();
                cond.CrystalDamage[c.Index] = roll < 0.25f ? CrystalDamage.Missing : roll < 0.6f ? CrystalDamage.Broken : CrystalDamage.Chipped;
            }
            cond.ShellChipping = 0.4f;
            return cond;
        }

        /// <summary>
        /// Near-duplicate report: a coarse perceptual descriptor per specimen (size, shape, cavity layout, crystal scale
        /// and density, colour, clarity, texture) and the nearest-neighbour distance across the batch. Pairs closer than
        /// the threshold are listed so they can be looked at on a sheet. Writes a CSV and returns the summary.
        /// </summary>
        public static string SimilarityReport(int count = 300, ulong firstSeed = 9000, float threshold = 0.16f)
        {
            var gs = new List<SpecimenGeology>(count);
            for (int i = 0; i < count; i++) gs.Add(SpecimenGenerator.Generate(firstSeed + (ulong)i));
            var desc = new List<float[]>(count);
            foreach (var g in gs) desc.Add(Descriptor(g));
            var sb = new StringBuilder();
            var csv = new StringBuilder("seed,nearest,dist,mineral,tier\n");
            int close = 0; float sum = 0f, min = float.MaxValue;
            var pairs = new List<(float d, int a, int b)>();
            for (int i = 0; i < count; i++)
            {
                float best = float.MaxValue; int bi = -1;
                for (int j = 0; j < count; j++)
                {
                    if (i == j || gs[i].Mineral != gs[j].Mineral) continue;
                    float d = Distance(desc[i], desc[j]);
                    if (d < best) { best = d; bi = j; }
                }
                if (bi < 0) continue;
                sum += best; if (best < min) min = best;
                if (best < threshold) { close++; if (i < bi) pairs.Add((best, i, bi)); }
                csv.AppendLine($"{gs[i].SeedString},{gs[bi].SeedString},{best:F3},{gs[i].Mineral},{gs[i].Tier}");
            }
            pairs.Sort((a, b) => a.d.CompareTo(b.d));
            sb.AppendLine($"[Similarity] {count} specimens: nearest same-family neighbour mean={sum / count:F3} min={min:F3}; {close} ({100f * close / count:F1}%) closer than {threshold:F2}");
            for (int i = 0; i < Mathf.Min(8, pairs.Count); i++) sb.AppendLine($"  {pairs[i].d:F3}  {gs[pairs[i].a].SeedString} ~ {gs[pairs[i].b].SeedString}  ({gs[pairs[i].a].Mineral} {gs[pairs[i].a].Cavity}/{gs[pairs[i].b].Cavity})");
            Directory.CreateDirectory(OutputFolder);
            File.WriteAllText(Path.Combine(OutputFolder, "similarity.csv"), csv.ToString());
            if (pairs.Count > 0)
            {
                var cells = new List<Cell>();
                for (int i = 0; i < Mathf.Min(10, pairs.Count); i++)
                {
                    cells.Add(new Cell { Geology = gs[pairs[i].a], Condition = new SpecimenCondition { Opened = true, Cleaned = 1f }, Caption = "pair" + i });
                    cells.Add(new Cell { Geology = gs[pairs[i].b], Condition = new SpecimenCondition { Opened = true, Cleaned = 1f }, Caption = "pair" + i });
                }
                Render(cells, "similarity_pairs", true, 256, 4, 0f);
            }
            Debug.Log(sb.ToString());
            return sb.ToString();
        }

        private static float[] Descriptor(SpecimenGeology g)
        {
            var d = new float[18];
            d[0] = Mathf.InverseLerp(0.034f, 0.165f, g.Size) * 1.5f;
            d[1] = (g.Axes.x - 1f) * 2f; d[2] = (g.Axes.y - 1f) * 2f; d[3] = (g.Axes.z - 1f) * 2f;
            d[4] = (int)g.Exterior * 0.35f;
            d[5] = (int)g.Cavity * 0.4f;
            d[6] = g.CavityFraction * 1.2f;
            d[7] = g.CrystalScale * 1.3f;
            d[8] = g.CrystalDensity;
            d[9] = g.Saturation * 1.3f;
            d[10] = g.Clarity * 0.9f;
            d[11] = g.PaletteIndex * 0.5f;
            d[12] = g.HasSecondary ? 0.5f : 0f;
            d[13] = g.Traits.Count * 0.6f;
            d[14] = g.LobeCenters != null && g.LobeCenters.Length > 1 ? g.LobeCenters[1].x / Mathf.Max(0.01f, g.Size) : 0f;
            d[15] = g.ExteriorRoughness * 5f;
            d[16] = (int)g.Texture * 0.3f;
            d[17] = g.Zoning * 0.6f;
            return d;
        }

        private static float Distance(float[] a, float[] b)
        {
            float s = 0f;
            for (int i = 0; i < a.Length; i++) { float d = a[i] - b[i]; s += d * d; }
            return Mathf.Sqrt(s / a.Length);
        }

        /// <summary>Render a list of cells into a grid. fixedDistance > 0 keeps the camera at that distance for every cell (sizes read); 0 frames each rock.</summary>
        public static string Render(List<Cell> cells, string fileName, bool opened, int cell, int cols, float fixedDistance)
        {
            var lib = SpecimenAssetLibrary.Load();
            if (lib == null || lib.CrystalMaterial == null) lib = AssetLibraryBuilder.Build();
            int count = cells.Count;
            int rows = Mathf.CeilToInt(count / (float)cols);
            var sheet = new Texture2D(cols * cell, rows * cell, TextureFormat.RGB24, false, false);
            var rt = new RenderTexture(cell, cell, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            rt.Create();

            // disable scene lights so every cell gets identical lighting
            var sceneLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            var wasEnabled = new List<bool>();
            foreach (var l in sceneLights) { wasEnabled.Add(l.enabled); l.enabled = false; }
            var prevAmbient = RenderSettings.ambientMode;
            var prevAmbientColor = RenderSettings.ambientLight;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.15f, 0.15f, 0.17f);

            var rig = new GameObject("_ContactSheetRig") { hideFlags = HideFlags.HideAndDontSave };
            rig.transform.position = new Vector3(0f, -400f, 0f);
            var camGo = new GameObject("Cam");
            camGo.transform.SetParent(rig.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.07f, 0.08f, 1f);
            cam.fieldOfView = 28f;
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 10f;
            cam.targetTexture = rt;
            cam.enabled = false;
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = false;
            camData.renderShadows = true;
            camData.antialiasing = AntialiasingMode.None;

            MakeLight(rig.transform, "Key", new Vector3(55f, -30f, 0f), 1.5f, new Color(1f, 0.96f, 0.9f), true);
            MakeLight(rig.transform, "Fill", new Vector3(30f, 140f, 0f), 0.45f, new Color(0.85f, 0.9f, 1f), false);
            MakeLight(rig.transform, "Rim", new Vector3(15f, 210f, 0f), 0.7f, new Color(1f, 1f, 1f), false);

            var csv = new StringBuilder();
            csv.AppendLine("cell,caption,seed,mineral,palette,cavity,exterior,sizeClass,texture,tier,value,size_m,mass_kg,crystalScale,density,saturation,clarity,dirt,stain,chip,secondary,traits,druzy,crystals");
            try
            {
                for (int n = 0; n < count; n++)
                {
                    var g = cells[n].Geology;
                    var go = new GameObject("Specimen");
                    go.transform.SetParent(rig.transform, false);
                    var vis = go.AddComponent<SpecimenVisual>();
                    vis.Build(g, cells[n].Condition ?? new SpecimenCondition { Opened = opened, Cleaned = 1f }, lib);
                    float radius = vis.Geometry.MaxRadius;
                    if (opened)
                    {
                        vis.TopHalf.gameObject.SetActive(false);
                        // camera looks down into the cavity from a slight tilt
                        float dist = fixedDistance > 0f ? fixedDistance : radius * 4.4f;
                        camGo.transform.localPosition = new Vector3(0f, dist * 0.74f, -dist * 0.62f);
                        camGo.transform.LookAt(rig.transform.position + new Vector3(0f, -radius * 0.3f, 0f));
                    }
                    else
                    {
                        float dist = fixedDistance > 0f ? fixedDistance : radius * 4.6f;
                        camGo.transform.localPosition = new Vector3(dist * 0.35f, dist * 0.55f, -dist * 0.76f);
                        camGo.transform.LookAt(rig.transform.position);
                        go.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
                    }
                    cam.Render();
                    RenderTexture.active = rt;
                    int col = n % cols, row = n / cols;
                    sheet.ReadPixels(new Rect(0, 0, cell, cell), col * cell, (rows - 1 - row) * cell);
                    RenderTexture.active = null;
                    int crystals = vis.Geometry.Crystals.Count;
                    csv.AppendLine($"{n},{cells[n].Caption},{g.SeedString},{g.Mineral},{g.Palette.Name.Replace(',', ' ')},{g.Cavity},{g.Exterior},{g.SizeClass},{g.Texture},{g.Tier},{g.BaseValue},{g.Size:F3},{g.MassKg:F2},{g.CrystalScale:F2},{g.CrystalDensity:F2},{g.Saturation:F2},{g.Clarity:F2},{g.Dirt:F2},{g.Stain:F2},{g.HasNaturalChip},{(g.HasSecondary ? g.Secondary.ToString() : "")},{string.Join("|", g.Traits)},{g.IsDruzy},{crystals}");
                    Object.DestroyImmediate(go);
                    if (n % 10 == 0) EditorUtility.DisplayProgressBar("Contact sheet", $"{fileName} {n}/{count}", n / (float)count);
                }
                sheet.Apply();
                Directory.CreateDirectory(OutputFolder);
                string png = Path.Combine(OutputFolder, fileName + ".png");
                File.WriteAllBytes(png, sheet.EncodeToPNG());
                File.WriteAllText(Path.Combine(OutputFolder, fileName + ".csv"), csv.ToString());
                Debug.Log($"[ContactSheet] wrote {png} ({cols}x{rows} cells of {cell}px)");
                return png;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                cam.targetTexture = null;
                Object.DestroyImmediate(rig);
                rt.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(sheet);
                for (int i = 0; i < sceneLights.Length; i++) if (sceneLights[i] != null) sceneLights[i].enabled = wasEnabled[i];
                RenderSettings.ambientMode = prevAmbient;
                RenderSettings.ambientLight = prevAmbientColor;
            }
        }

        private static Light MakeLight(Transform parent, string name, Vector3 euler, float intensity, Color color, bool shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(euler);
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = intensity;
            l.color = color;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            l.shadowStrength = 0.7f;
            return l;
        }
    }
}
