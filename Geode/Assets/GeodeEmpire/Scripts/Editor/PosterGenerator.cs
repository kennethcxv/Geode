using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.EditorTools
{
    /// <summary>
    /// Renders wall posters from the game's own specimens: a mineral chart (one opened specimen per family)
    /// and a rough-rock chart. Keeps the workshop art on-brand without any external content.
    /// </summary>
    public static class PosterGenerator
    {
        public const string InteriorsPath = ProceduralTextureFactory.TextureFolder + "/T_PosterMinerals.png";
        public const string ExteriorsPath = ProceduralTextureFactory.TextureFolder + "/T_PosterRocks.png";

        public static void EnsurePosters(bool force = false)
        {
            if (force || !File.Exists(InteriorsPath)) Render(InteriorsPath, true);
            if (force || !File.Exists(ExteriorsPath)) Render(ExteriorsPath, false);
            AssetDatabase.Refresh();
        }

        private static ulong[] PickSeeds(bool opened)
        {
            var seeds = new ulong[9];
            var families = MineralCatalog.All;
            for (int f = 0; f < 9 && f < families.Count; f++)
            {
                var id = families[f].Id;
                ulong best = 0; float bestScore = -1f;
                for (ulong s = 7000 + (ulong)f * 4000; s < 7000 + (ulong)f * 4000 + 4000; s++)
                {
                    var g = SpecimenGenerator.Generate(s);
                    if (g.Mineral != id) continue;
                    if (g.Cavity == CavityArchetype.Nodule && id != MineralId.Agate) continue;
                    float score = Valuation.VisualScore(g) + (g.Tier >= QualityTier.Good ? 0.3f : 0f);
                    if (score > bestScore) { bestScore = score; best = s; }
                    if (bestScore > 1.1f) break;
                }
                seeds[f] = best;
            }
            return seeds;
        }

        public static void Render(string path, bool opened)
        {
            var lib = SpecimenAssetLibrary.Load();
            if (lib == null || lib.CrystalMaterial == null) lib = AssetLibraryBuilder.Build();
            const int cell = 256, cols = 3, rows = 3, margin = 24;
            int size = cols * cell + margin * 2;
            var sheet = new Texture2D(size, size, TextureFormat.RGB24, false, false);
            var bg = new Color(0.09f, 0.085f, 0.08f);
            var fill = new Color[size * size];
            for (int i = 0; i < fill.Length; i++) fill[i] = bg;
            sheet.SetPixels(fill);
            var rt = new RenderTexture(cell, cell, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            rt.Create();
            var sceneLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            var wasEnabled = new bool[sceneLights.Length];
            for (int i = 0; i < sceneLights.Length; i++) { wasEnabled[i] = sceneLights[i].enabled; sceneLights[i].enabled = false; }
            var prevAmbient = RenderSettings.ambientMode; var prevAmbientColor = RenderSettings.ambientLight;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.14f, 0.14f, 0.16f);
            var rig = new GameObject("_PosterRig") { hideFlags = HideFlags.HideAndDontSave };
            rig.transform.position = new Vector3(0f, -450f, 0f);
            var camGo = new GameObject("Cam"); camGo.transform.SetParent(rig.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = bg; cam.fieldOfView = 26f; cam.nearClipPlane = 0.02f; cam.farClipPlane = 10f; cam.targetTexture = rt; cam.enabled = false;
            var camData = cam.GetUniversalAdditionalCameraData(); camData.renderPostProcessing = false; camData.antialiasing = AntialiasingMode.None;
            Light(rig.transform, new Vector3(52f, -30f, 0f), 1.6f, new Color(1f, 0.96f, 0.9f), true);
            Light(rig.transform, new Vector3(30f, 140f, 0f), 0.5f, new Color(0.85f, 0.9f, 1f), false);
            Light(rig.transform, new Vector3(15f, 210f, 0f), 0.7f, Color.white, false);
            try
            {
                var seeds = PickSeeds(opened);
                for (int n = 0; n < 9; n++)
                {
                    var g = SpecimenGenerator.Generate(seeds[n]);
                    var go = new GameObject("Specimen"); go.transform.SetParent(rig.transform, false);
                    var vis = go.AddComponent<SpecimenVisual>();
                    vis.Build(g, new SpecimenCondition { Opened = opened }, lib);
                    float radius = vis.Geometry.MaxRadius;
                    if (opened)
                    {
                        vis.TopHalf.gameObject.SetActive(false);
                        float dist = radius * 4.3f;
                        camGo.transform.localPosition = new Vector3(0f, dist * 0.78f, -dist * 0.58f);
                        camGo.transform.LookAt(rig.transform.position + new Vector3(0f, -radius * 0.3f, 0f));
                    }
                    else
                    {
                        float dist = radius * 4.6f;
                        camGo.transform.localPosition = new Vector3(dist * 0.35f, dist * 0.5f, -dist * 0.78f);
                        camGo.transform.LookAt(rig.transform.position);
                        go.transform.localRotation = Quaternion.Euler(0f, 40f, 0f);
                    }
                    cam.Render();
                    RenderTexture.active = rt;
                    int col = n % cols, row = n / cols;
                    sheet.ReadPixels(new Rect(0, 0, cell, cell), margin + col * cell, margin + (rows - 1 - row) * cell);
                    RenderTexture.active = null;
                    Object.DestroyImmediate(go);
                }
                // thin cell separators
                var px = sheet.GetPixels();
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool line = ((x - margin) % cell == 0 || (y - margin) % cell == 0) && x >= margin && y >= margin && x <= size - margin && y <= size - margin;
                    if (line) px[y * size + x] = new Color(0.2f, 0.19f, 0.18f);
                }
                sheet.SetPixels(px);
                sheet.Apply();
                File.WriteAllBytes(path, sheet.EncodeToPNG());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.sRGBTexture = true; imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 1024; imp.textureCompression = TextureImporterCompression.Compressed;
                imp.SaveAndReimport();
                Debug.Log("[PosterGenerator] wrote " + path);
            }
            finally
            {
                cam.targetTexture = null;
                Object.DestroyImmediate(rig);
                rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(sheet);
                for (int i = 0; i < sceneLights.Length; i++) if (sceneLights[i] != null) sceneLights[i].enabled = wasEnabled[i];
                RenderSettings.ambientMode = prevAmbient; RenderSettings.ambientLight = prevAmbientColor;
            }
        }

        private static void Light(Transform parent, Vector3 euler, float intensity, Color color, bool shadows)
        {
            var go = new GameObject("L"); go.transform.SetParent(parent, false); go.transform.localRotation = Quaternion.Euler(euler);
            var l = go.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = intensity; l.color = color; l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        }
    }
}
