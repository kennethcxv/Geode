using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.EditorTools
{
    /// <summary>
    /// Gate A tooling: renders N deterministic specimens under identical lighting into a grid PNG
    /// (plus a CSV describing each cell) so visual variety can be judged, not assumed.
    /// </summary>
    public static class ContactSheetGenerator
    {
        public const string OutputFolder = "Output";   // project-relative (git-ignored)

        [MenuItem("GeodeEmpire/Contact Sheet/Interiors 200 (2 sheets)")]
        public static void Interiors200()
        {
            Generate(100, 1000, "contact_interiors_A", true);
            Generate(100, 1100, "contact_interiors_B", true);
        }

        [MenuItem("GeodeEmpire/Contact Sheet/Exteriors 100")]
        public static void Exteriors100() => Generate(100, 1000, "contact_exteriors", false);

        public static string Generate(int count, ulong firstSeed, string fileName, bool opened, int cell = 256, int cols = 10, ulong[] seeds = null)
        {
            var lib = SpecimenAssetLibrary.Load();
            if (lib == null || lib.CrystalMaterial == null) lib = AssetLibraryBuilder.Build();

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
            csv.AppendLine("cell,seed,mineral,palette,cavity,exterior,tier,value,size_m,mass_kg,crystalScale,density,saturation,clarity,secondary,traits,druzy,crystals");
            try
            {
                for (int n = 0; n < count; n++)
                {
                    ulong seed = seeds != null ? seeds[n] : firstSeed + (ulong)n;
                    var g = SpecimenGenerator.Generate(seed);
                    var go = new GameObject("Specimen");
                    go.transform.SetParent(rig.transform, false);
                    var vis = go.AddComponent<SpecimenVisual>();
                    vis.Build(g, new SpecimenCondition { Opened = opened }, lib);
                    float radius = vis.Geometry.MaxRadius;
                    if (opened)
                    {
                        vis.TopHalf.gameObject.SetActive(false);
                        // camera looks down into the cavity from a slight tilt
                        float dist = radius * 4.4f;
                        camGo.transform.localPosition = new Vector3(0f, dist * 0.74f, -dist * 0.62f);
                        camGo.transform.LookAt(rig.transform.position + new Vector3(0f, -radius * 0.3f, 0f));
                    }
                    else
                    {
                        float dist = radius * 4.6f;
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
                    csv.AppendLine($"{n},{g.SeedString},{g.Mineral},{g.Palette.Name.Replace(',', ' ')},{g.Cavity},{g.Exterior},{g.Tier},{g.BaseValue},{g.Size:F3},{g.MassKg:F2},{g.CrystalScale:F2},{g.CrystalDensity:F2},{g.Saturation:F2},{g.Clarity:F2},{(g.HasSecondary ? g.Secondary.ToString() : "")},{string.Join("|", g.Traits)},{g.IsDruzy},{crystals}");
                    Object.DestroyImmediate(go);
                    if (n % 10 == 0) EditorUtility.DisplayProgressBar("Contact sheet", $"{n}/{count}", n / (float)count);
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
