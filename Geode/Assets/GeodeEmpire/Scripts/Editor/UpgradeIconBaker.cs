using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using GeodeEmpire.Economy;

namespace GeodeEmpire.EditorTools
{
    /// <summary>
    /// Photographs the thing each upgrade actually buys. §9.3 of the starter-rebuild spec is blunt about the
    /// alternative — "Do not use identical placeholder circles for everything" — and the reference upgrade screen
    /// (R08) carries a small rendered image of every fixture beside its price. These are baked once into
    /// Resources so the tablet can show them without holding a scene camera open.
    /// </summary>
    public static class UpgradeIconBaker
    {
        private const string PropFolder = "Assets/GeodeEmpire/Models/Props";
        private const string OutFolder = "Assets/GeodeEmpire/Resources/UI/Upgrades";
        private const int Width = 320, Height = 240;

        /// <summary>What each upgrade looks like: the prop file, the materials for its slots, and a framing yaw.</summary>
        private sealed class Shot
        {
            public string Prop;
            public string Materials;
            public float Yaw = 34f;
            public float Pitch = 13f;
            /// <summary>Extra props staged around the first, for an upgrade that is a change to a machine.</summary>
            public (string prop, string mats, Vector3 pos, float yaw)[] Extras;
        }

        private static readonly Dictionary<string, Shot> Shots = new Dictionary<string, Shot>
        {
            [UpgradeCatalog.Loupe] = new Shot { Prop = "prop_loupe", Materials = "M_Brass,M_Glass,M_MetalDark", Pitch = 18f },
            [UpgradeCatalog.InspectionLamp] = new Shot { Prop = "prop_task_lamp", Materials = "M_MetalDark,M_Enamel,M_Bulb,M_PlasticDark" },
            [UpgradeCatalog.BenchClamp] = new Shot { Prop = "prop_cradle", Materials = "M_Leather,M_MetalDark,M_WoodDark", Pitch = 15f },
            [UpgradeCatalog.FineChisel] = new Shot { Prop = "prop_chisel_fine", Materials = "M_Steel,M_PlasticDark", Pitch = 18f },
            [UpgradeCatalog.CalibratedScale] = new Shot { Prop = "prop_scale_station", Materials = "M_Stainless,M_PlasticDark,M_Screen,M_MetalDark", Yaw = 200f, Pitch = 12f },
            [UpgradeCatalog.CollectionCabinet] = new Shot { Prop = "prop_display_cabinet", Materials = "M_WoodDark,M_CaseLight", Yaw = 208f, Pitch = 10f },
            [UpgradeCatalog.DisplayExpansion] = new Shot { Prop = "prop_display_cabinet", Materials = "M_WoodDark,M_CaseLight", Yaw = 152f, Pitch = 8f },
            [UpgradeCatalog.SalesTable] = new Shot { Prop = "prop_glass_counter", Materials = "M_ShopWood,M_CaseGlass,M_Brass", Pitch = 12f },
            [UpgradeCatalog.ShopShelving] = new Shot { Prop = "prop_display_wall", Materials = "M_ShopWood,M_ShelfBack,M_LedStrip", Yaw = 26f, Pitch = 12f },
            [UpgradeCatalog.ShopSignage] = new Shot { Prop = "prop_logo_mountains", Materials = "M_LogoBrass,M_LogoCap", Yaw = 8f, Pitch = 8f },
            [UpgradeCatalog.Wedge] = new Shot { Prop = "prop_wedge", Materials = "M_Steel", Pitch = 14f,
                Extras = new[] { ("prop_lump_hammer", "M_Hickory,M_MetalDark", new Vector3(0.16f, 0f, -0.05f), 60f) } },
            [UpgradeCatalog.HeavyCradle] = new Shot { Prop = "prop_heavy_cradle", Materials = "M_Leather,M_MetalDark,M_Steel", Pitch = 16f },
            [UpgradeCatalog.TrimSaw] = new Shot { Prop = "prop_saw_station", Materials = "M_MachinePaint,M_Coolant,M_Glass,M_Rubber,M_Red,M_Steel,M_Dial,M_MachineIron,M_MachineAlu,M_PlasticDark,M_Nameplate", Yaw = 214f, Pitch = 12f },
            [UpgradeCatalog.SawBlade] = new Shot { Prop = "prop_saw_blade", Materials = "M_Metal,M_MetalDark,M_BladeRim,M_BladeLabel", Yaw = 14f, Pitch = 12f },
            [UpgradeCatalog.ThinBlade] = new Shot { Prop = "prop_saw_blade", Materials = "M_Metal,M_MetalDark,M_BladeRim,M_BladeLabel", Yaw = 76f, Pitch = 8f },
            [UpgradeCatalog.CoolantPump] = new Shot { Prop = "prop_saw_valve", Materials = "M_Brass,M_MetalDark,M_Coolant", Pitch = 16f },
            [UpgradeCatalog.SawClamp] = new Shot { Prop = "prop_saw_vise", Materials = "M_MachinePaint,M_Rubber,M_Steel,M_MachineAlu", Pitch = 16f },
            [UpgradeCatalog.GeodeCracker] = new Shot { Prop = "prop_cracker", Materials = "M_MachinePaint,M_MachineIron,M_Steel,M_Brass,M_SignYellow,M_Rubber,M_MachineAlu,M_Dial,M_Nameplate", Yaw = 208f, Pitch = 10f },
            [UpgradeCatalog.PolishLap] = new Shot { Prop = "prop_polish_lap", Materials = "M_MachinePaint,M_MachineTop,M_Steel,M_Water,M_Rubber,M_MachineAlu,M_PlasticDark", Yaw = 120f, Pitch = 16f },
            [UpgradeCatalog.BackRoom] = new Shot { Prop = "prop_shelf_unit", Materials = "M_MetalDark,M_WoodDark", Yaw = 30f, Pitch = 14f,
                Extras = new[] { ("prop_pallet", "M_Wood,M_MetalDark", new Vector3(0.9f, 0f, 0.35f), 12f) } },
            [UpgradeCatalog.ShopFront] = new Shot { Prop = "prop_shop_case", Materials = "M_WoodDark,M_CaseLight", Yaw = 206f, Pitch = 10f },
            [UpgradeCatalog.Stage2] = new Shot { Prop = "prop_rock_rack", Materials = "M_MetalDark,M_WoodDark", Yaw = 28f, Pitch = 14f },
            [UpgradeCatalog.Stage3] = new Shot { Prop = "prop_saw_station_large", Materials = "M_MachinePaint,M_Coolant,M_Glass,M_Rubber,M_Red,M_Steel,M_Dial,M_MachineIron,M_MachineAlu,M_PlasticDark,M_Nameplate", Yaw = 214f, Pitch = 12f },
        };

        /// <summary>
        /// Crate art for the suppliers screen (§9.2). Four builds rather than twelve near-identical ones: a
        /// supplier's identity on that screen is its accent, its chips and its price, and what the picture has to
        /// say is what turns up on the pallet.
        /// </summary>
        private static readonly (string name, (string prop, string mats, Vector3 pos, float yaw)[] parts)[] Crates =
        {
            ("plain", new[] {
                ("prop_crate_body", "M_Wood,M_Straw", Vector3.zero, 0f),
                ("prop_crate_lid", "M_Wood", new Vector3(0f, 0.37f, 0f), 0f) }),
            ("curated", new[] {
                ("prop_crate_body", "M_Wood,M_Straw", Vector3.zero, 0f),
                ("prop_crate_lid", "M_Wood", new Vector3(0.06f, 0.40f, -0.16f), 13f),
                ("prop_label_stand", "M_Paper", new Vector3(0.0f, 0.42f, -0.30f), 4f) }),
            ("premium", new[] {
                ("prop_crate_body", "M_Wood,M_Straw", Vector3.zero, 0f),
                ("prop_crate_lid", "M_Wood", new Vector3(-0.30f, 0.06f, -0.34f), 62f),
                ("prop_gift_box", "M_BoxWhite,M_NoteBand", new Vector3(0.30f, 0.0f, -0.30f), 22f) }),
            ("bulk", new[] {
                ("prop_pallet", "M_Wood,M_MetalDark", Vector3.zero, 0f),
                ("prop_crate_body", "M_Wood,M_Straw", new Vector3(-0.22f, 0.13f, 0f), 4f),
                ("prop_crate_lid", "M_Wood", new Vector3(-0.22f, 0.50f, 0f), 4f),
                ("prop_crate_body", "M_Wood,M_Straw", new Vector3(0.34f, 0.13f, 0.06f), -8f),
                ("prop_crate_lid", "M_Wood", new Vector3(0.34f, 0.50f, 0.06f), -8f) }),
        };

        [MenuItem("Geode/Bake upgrade icons")]
        public static void Bake()
        {
            WorkshopMaterials.EnsureAll();
            Directory.CreateDirectory(OutFolder);
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 8 };
            rt.Create();

            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            var wasOn = new List<bool>();
            foreach (var l in lights) { wasOn.Add(l.enabled); l.enabled = false; }
            var prevMode = RenderSettings.ambientMode;
            var prevColor = RenderSettings.ambientLight;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.30f, 0.30f, 0.33f);

            var rig = new GameObject("_IconRig") { hideFlags = HideFlags.HideAndDontSave };
            rig.transform.position = new Vector3(0f, -500f, 0f);
            var camGo = new GameObject("Cam");
            camGo.transform.SetParent(rig.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.075f, 0.072f, 0.086f, 0f);   // the card's own surface shows through
            cam.fieldOfView = 26f;
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 60f;
            cam.targetTexture = rt;
            cam.enabled = false;
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = false;
            camData.renderShadows = true;
            camData.antialiasing = AntialiasingMode.None;
            KeyLight(rig.transform, "Key", new Vector3(38f, -34f, 0f), 1.9f, new Color(1f, 0.96f, 0.89f), true);
            KeyLight(rig.transform, "Fill", new Vector3(22f, 128f, 0f), 0.7f, new Color(0.82f, 0.88f, 1f), false);
            KeyLight(rig.transform, "Rim", new Vector3(6f, 202f, 0f), 1.1f, new Color(1f, 1f, 1f), false);
            var flash = new GameObject("Flash");
            flash.transform.SetParent(rig.transform, false);
            var flashLight = flash.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.color = new Color(1f, 0.97f, 0.93f);
            flashLight.shadows = LightShadows.None;

            int done = 0, missing = 0;
            Directory.CreateDirectory("Assets/GeodeEmpire/Resources/UI/Crates");
            try
            {
                foreach (var (crateName, parts) in Crates)
                {
                    var stage = new GameObject("Crate");
                    stage.transform.SetParent(rig.transform, false);
                    foreach (var (prop, mats, pos, yaw) in parts) Place(stage.transform, prop, mats, pos, yaw);
                    var b = Encapsulate(stage.transform);
                    float rad = Mathf.Max(b.extents.magnitude, 0.05f);
                    float d = rad / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.28f;
                    var dr = Quaternion.Euler(17f, 32f, 0f) * Vector3.back;
                    camGo.transform.position = b.center - dr * d;
                    camGo.transform.rotation = Quaternion.LookRotation(dr, Vector3.up);
                    flash.transform.position = camGo.transform.position + camGo.transform.right * rad * 0.4f + Vector3.up * rad * 0.3f;
                    flashLight.range = d * 2.4f;
                    flashLight.intensity = 2.6f + rad * 1.6f;
                    cam.Render();
                    var ctex = new Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
                    RenderTexture.active = rt;
                    ctex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                    ctex.Apply();
                    RenderTexture.active = null;
                    File.WriteAllBytes($"Assets/GeodeEmpire/Resources/UI/Crates/{crateName}.png", ctex.EncodeToPNG());
                    Object.DestroyImmediate(ctex);
                    Object.DestroyImmediate(stage);
                }
                foreach (var u in UpgradeCatalog.All)
                {
                    if (!Shots.TryGetValue(u.Id, out var shot)) { missing++; continue; }
                    var stage = new GameObject("Stage");
                    stage.transform.SetParent(rig.transform, false);
                    if (!Place(stage.transform, shot.Prop, shot.Materials, Vector3.zero, 0f)) { Object.DestroyImmediate(stage); missing++; continue; }
                    if (shot.Extras != null)
                        foreach (var (prop, mats, pos, yaw) in shot.Extras) Place(stage.transform, prop, mats, pos, yaw);

                    // frame on the whole staged group, whatever its size
                    var bounds = Encapsulate(stage.transform);
                    float radius = Mathf.Max(bounds.extents.magnitude, 0.05f);
                    float dist = radius / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.32f;
                    var dir = Quaternion.Euler(shot.Pitch, shot.Yaw, 0f) * Vector3.back;
                    camGo.transform.position = bounds.center - dir * dist;
                    camGo.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

                    // a soft flash from the camera: without it a glazed cabinet or a dark machine reads as a slab
                    flash.transform.position = camGo.transform.position + camGo.transform.right * radius * 0.4f + Vector3.up * radius * 0.3f;
                    flashLight.range = dist * 2.4f;
                    flashLight.intensity = 2.6f + radius * 1.6f;
                    cam.Render();
                    var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false, false);
                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                    tex.Apply();
                    RenderTexture.active = null;
                    File.WriteAllBytes($"{OutFolder}/{u.Id}.png", tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                    Object.DestroyImmediate(stage);
                    done++;
                }
            }
            finally
            {
                Object.DestroyImmediate(rig);
                RenderTexture.active = null;
                rt.Release();
                Object.DestroyImmediate(rt);
                RenderSettings.ambientMode = prevMode;
                RenderSettings.ambientLight = prevColor;
                for (int i = 0; i < lights.Length; i++) lights[i].enabled = wasOn[i];
            }

            AssetDatabase.Refresh();
            var paths = new List<string>();
            foreach (var u in UpgradeCatalog.All) paths.Add($"{OutFolder}/{u.Id}.png");
            foreach (var (crateName, _) in Crates) paths.Add($"Assets/GeodeEmpire/Resources/UI/Crates/{crateName}.png");
            foreach (var path in paths)
            {
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                imp.textureType = TextureImporterType.Default;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.maxTextureSize = 512;
                imp.SaveAndReimport();
            }
            Sheet();
            Debug.Log($"[UpgradeIconBaker] baked {done}, no shot for {missing}");
        }

        /// <summary>One image of every icon, for reviewing the set rather than the file.</summary>
        private static void Sheet()
        {
            var ids = new List<string>();
            foreach (var u in UpgradeCatalog.All) if (File.Exists($"{OutFolder}/{u.Id}.png")) ids.Add(u.Id);
            if (ids.Count == 0) return;
            const int cols = 5;
            int rows = Mathf.CeilToInt(ids.Count / (float)cols);
            var sheet = new Texture2D(cols * Width, rows * Height, TextureFormat.RGB24, false, false);
            var fill = new Color32(20, 19, 24, 255);
            var clear = new Color32[Width * Height];
            for (int i = 0; i < clear.Length; i++) clear[i] = fill;
            for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++) sheet.SetPixels32(c * Width, r * Height, Width, Height, clear);
            for (int i = 0; i < ids.Count; i++)
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                tex.LoadImage(File.ReadAllBytes($"{OutFolder}/{ids[i]}.png"));
                var px = tex.GetPixels32();
                var flat = new Color32[px.Length];
                for (int k = 0; k < px.Length; k++)
                {
                    float a = px[k].a / 255f;
                    flat[k] = new Color32((byte)(px[k].r * a + fill.r * (1f - a)), (byte)(px[k].g * a + fill.g * (1f - a)), (byte)(px[k].b * a + fill.b * (1f - a)), 255);
                }
                sheet.SetPixels32((i % cols) * Width, (rows - 1 - i / cols) * Height, Width, Height, flat);
                Object.DestroyImmediate(tex);
            }
            sheet.Apply();
            Directory.CreateDirectory("Assets/Output/starter");
            File.WriteAllBytes("Assets/Output/starter/upgrade_icons.png", sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
        }

        private static bool Place(Transform parent, string file, string materials, Vector3 pos, float yaw)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropFolder}/{file}.fbx");
            if (asset == null) { Debug.LogWarning("[UpgradeIconBaker] missing prop " + file); return false; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var names = materials.Split(',');
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = WorkshopMaterials.Get(names[Mathf.Min(i, names.Length - 1)].Trim());
                r.sharedMaterials = mats;
            }
            // the collision proxies the generator exports are not part of the picture
            var proxies = new List<Transform>();
            foreach (Transform child in go.transform) if (child.name.StartsWith("COL_")) proxies.Add(child);
            foreach (var p in proxies) Object.DestroyImmediate(p.gameObject);
            return true;
        }

        private static Bounds Encapsulate(Transform root)
        {
            bool first = true;
            var b = new Bounds();
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
            {
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }
            return first ? new Bounds(root.position, Vector3.one * 0.3f) : b;
        }

        private static void KeyLight(Transform parent, string name, Vector3 euler, float intensity, Color color, bool shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(euler);
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = intensity;
            l.color = color;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        }
    }
}
