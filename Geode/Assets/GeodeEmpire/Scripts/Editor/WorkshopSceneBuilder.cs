using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;
using GeodeEmpire.Cracking;
using GeodeEmpire.Interaction;
using GeodeEmpire.Player;
using GeodeEmpire.Specimens;
using GeodeEmpire.UI;
using GeodeEmpire.VFX;
using GeodeEmpire.Workshop;

namespace GeodeEmpire.EditorTools
{
    /// <summary>URP Lit materials for the workshop, generated from procedural textures.</summary>
    public static class WorkshopMaterials
    {
        public const string Folder = "Assets/GeodeEmpire/Materials";

        public static Material Get(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/{name}.mat");

        public static void EnsureAll()
        {
            WorkshopTextures.EnsureAll();
            Directory.CreateDirectory(Folder);
            Lit("M_Concrete", "T_Concrete", Color.white, 0.22f, 0f, 3.5f);
            Lit("M_Plaster", "T_Plaster", new Color(0.9f, 0.87f, 0.8f), 0.12f, 0f, 2f);
            Lit("M_Ceiling", "T_Plaster", new Color(0.75f, 0.74f, 0.7f), 0.1f, 0f, 2f);
            Lit("M_Wood", "T_Wood", Color.white, 0.32f, 0f, 1f);
            Lit("M_WoodDark", "T_WoodDark", Color.white, 0.38f, 0f, 1f);
            Lit("M_WoodPainted", null, new Color(0.62f, 0.66f, 0.6f), 0.45f, 0f, 1f);
            Lit("M_Metal", "T_Metal", new Color(0.82f, 0.82f, 0.84f), 0.62f, 0.85f, 1f);
            Lit("M_MetalDark", "T_Metal", new Color(0.32f, 0.32f, 0.34f), 0.55f, 0.7f, 1f);
            Lit("M_Cardboard", "T_Cardboard", Color.white, 0.1f, 0f, 1f);
            Lit("M_Straw", "T_Straw", Color.white, 0.2f, 0f, 1f);
            Lit("M_Rubber", null, new Color(0.12f, 0.11f, 0.1f), 0.25f, 0f, 1f);
            Lit("M_Plastic", null, new Color(0.24f, 0.25f, 0.28f), 0.55f, 0f, 1f);
            Lit("M_PlasticBlue", null, new Color(0.22f, 0.32f, 0.45f), 0.5f, 0f, 1f);
            Lit("M_Tarp", null, new Color(0.22f, 0.3f, 0.24f), 0.2f, 0f, 1f);
            Lit("M_Paper", null, new Color(0.92f, 0.9f, 0.84f), 0.2f, 0f, 1f);
            Lit("M_Screen", null, new Color(0.05f, 0.06f, 0.08f), 0.9f, 0f, 1f, emission: new Color(0.1f, 0.14f, 0.18f));
            Lit("M_Brick", "T_Concrete", new Color(0.55f, 0.42f, 0.36f), 0.15f, 0f, 3f);
            Lit("M_PlasterWarm", "T_Plaster", new Color(0.84f, 0.81f, 0.74f), 0.12f, 0f, 2f);
            Lit("M_Wainscot", "T_WoodDark", new Color(0.7f, 0.66f, 0.6f), 0.35f, 0f, 1f);
            var glass = Lit("M_Glass", null, new Color(0.7f, 0.85f, 0.95f, 0.18f), 0.95f, 0f, 1f);
            SetTransparent(glass);
            AssetDatabase.SaveAssets();
        }

        private static Material Lit(string name, string texName, Color color, float smoothness, float metallic, float tiling, Color? emission = null)
        {
            string path = $"{Folder}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            if (m.shader != shader) m.shader = shader;
            m.SetColor("_BaseColor", color);
            var tex = texName != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(ProceduralTextureFactory.TextureFolder + "/" + texName + ".png") : null;
            m.SetTexture("_BaseMap", tex);
            m.SetTextureScale("_BaseMap", new Vector2(tiling, tiling));
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void SetTransparent(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_ZWrite", 0f);
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(m);
        }
    }

    /// <summary>Simple box meshes with world-scaled UVs so tiling matches the props (0.5 m per tile).</summary>
    public static class MeshFactory
    {
        public const string Folder = "Assets/GeodeEmpire/Models/Generated";

        public static Mesh Box(string name, Vector3 size, float uvPerMeter = 2f)
        {
            Directory.CreateDirectory(Folder);
            string path = $"{Folder}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var m = existing != null ? existing : new Mesh();
            m.Clear();
            m.name = name;
            Vector3 h = size * 0.5f;
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            void Face(Vector3 n, Vector3 u, Vector3 v, float su, float sv)
            {
                int b = verts.Count;
                Vector3 c = Vector3.Scale(n, h);
                Vector3 uu = Vector3.Scale(u, h), vv = Vector3.Scale(v, h);
                verts.Add(c - uu - vv); verts.Add(c + uu - vv); verts.Add(c + uu + vv); verts.Add(c - uu + vv);
                for (int i = 0; i < 4; i++) norms.Add(n);
                uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(su * uvPerMeter, 0f)); uvs.Add(new Vector2(su * uvPerMeter, sv * uvPerMeter)); uvs.Add(new Vector2(0f, sv * uvPerMeter));
                tris.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
            }
            Face(Vector3.up, Vector3.right, Vector3.forward, size.x, size.z);
            Face(Vector3.down, Vector3.right, Vector3.back, size.x, size.z);
            Face(Vector3.forward, Vector3.left, Vector3.up, size.x, size.y);
            Face(Vector3.back, Vector3.right, Vector3.up, size.x, size.y);
            Face(Vector3.right, Vector3.forward, Vector3.up, size.z, size.y);
            Face(Vector3.left, Vector3.back, Vector3.up, size.z, size.y);
            m.SetVertices(verts); m.SetNormals(norms); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
            m.RecalculateTangents();
            m.RecalculateBounds();
            if (existing == null) AssetDatabase.CreateAsset(m, path); else EditorUtility.SetDirty(m);
            return m;
        }
    }

    public static class WorkshopSceneBuilder
    {
        public const string ScenePath = "Assets/GeodeEmpire/Scenes/Workshop.unity";
        public const string PropFolder = "Assets/GeodeEmpire/Models/Props";
        public const string PanelSettingsPath = "Assets/GeodeEmpire/UI/GeodePanelSettings.asset";
        public const string VolumeProfilePath = "Assets/GeodeEmpire/Data/WorkshopVolume.asset";
        public const string CratePrefabPath = "Assets/GeodeEmpire/Resources/Prefabs/Crate.prefab";

        // Room dimensions (metres)
        const float RoomW = 7.2f, RoomD = 5.4f, RoomH = 3.0f;

        [MenuItem("GeodeEmpire/Build Workshop Scene")]
        public static void Build()
        {
            AssetLibraryBuilder.Build();
            WorkshopMaterials.EnsureAll();
            var panel = EnsurePanelSettings();
            EnsureCratePrefab();
            var volumeProfile = EnsureVolumeProfile();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.44f, 0.5f);
            RenderSettings.ambientEquatorColor = new Color(0.36f, 0.32f, 0.28f);
            RenderSettings.ambientGroundColor = new Color(0.16f, 0.14f, 0.12f);
            RenderSettings.fog = false;
            RenderSettings.skybox = null;

            var env = new GameObject("Environment");
            BuildRoom(env.transform);
            BuildLighting(env.transform);
            var stations = new GameObject("Stations");
            BuildStations(stations.transform);
            BuildPlayer();
            BuildSystems(panel, volumeProfile);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            Debug.Log("[WorkshopSceneBuilder] built " + ScenePath);
        }

        // ------------------------------------------------------------------------------------
        private static PanelSettings EnsurePanelSettings()
        {
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (ps == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PanelSettingsPath));
                AssetDatabase.Refresh();
                ps = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(ps, PanelSettingsPath);
            }
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.match = 0.5f;
            ps.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/GeodeEmpire/UI/GeodeTheme.tss");
            EditorUtility.SetDirty(ps);
            return ps;
        }

        private static VolumeProfile EnsureVolumeProfile()
        {
            var vp = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (vp != null) return vp;
            Directory.CreateDirectory(Path.GetDirectoryName(VolumeProfilePath));
            AssetDatabase.Refresh();
            vp = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(vp, VolumeProfilePath);
            var tm = vp.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES); AssetDatabase.AddObjectToAsset(tm, vp);
            var bloom = vp.Add<Bloom>(true); bloom.intensity.Override(0.28f); bloom.threshold.Override(1.15f); bloom.scatter.Override(0.6f); AssetDatabase.AddObjectToAsset(bloom, vp);
            var vig = vp.Add<Vignette>(true); vig.intensity.Override(0.22f); vig.smoothness.Override(0.4f); AssetDatabase.AddObjectToAsset(vig, vp);
            var ca = vp.Add<ColorAdjustments>(true); ca.contrast.Override(8f); ca.saturation.Override(6f); ca.postExposure.Override(0.15f); AssetDatabase.AddObjectToAsset(ca, vp);
            EditorUtility.SetDirty(vp);
            AssetDatabase.SaveAssets();
            return vp;
        }

        private static void EnsureCratePrefab()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CratePrefabPath));
            AssetDatabase.Refresh();
            var root = new GameObject("Crate");
            var body = Prop("prop_crate_body", root.transform, Vector3.zero, 0f, "M_Wood", collider: true);
            // straw bed material on the straw disc: the prop is one mesh, so whole crate is wood; add a straw disc
            var straw = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            straw.name = "Straw";
            straw.transform.SetParent(root.transform, false);
            straw.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            straw.transform.localScale = new Vector3(0.5f, 0.02f, 0.36f);
            straw.GetComponent<MeshRenderer>().sharedMaterial = WorkshopMaterials.Get("M_Straw");
            Object.DestroyImmediate(straw.GetComponent<Collider>());
            var lid = Prop("prop_crate_lid", root.transform, new Vector3(0f, 0.34f, 0f), 0f, "M_Wood", collider: false);
            lid.name = "Lid";
            var bed = new GameObject("Bed");
            bed.transform.SetParent(root.transform, false);
            bed.transform.localPosition = new Vector3(0f, 0.07f, 0f);
            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.18f, 0f);
            box.size = new Vector3(0.64f, 0.36f, 0.48f);
            var ce = root.AddComponent<CrateEntity>();
            ce.Lid = lid.transform;
            ce.Bed = bed.transform;
            PrefabUtility.SaveAsPrefabAsset(root, CratePrefabPath);
            Object.DestroyImmediate(root);
        }

        // ------------------------------------------------------------------------------------
        private static GameObject Prop(string file, Transform parent, Vector3 pos, float yaw, string material, bool collider = true, Vector3? scale = null)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropFolder}/{file}.fbx");
            if (asset == null) { Debug.LogWarning("missing prop " + file); return new GameObject(file); }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = file.Replace("prop_", "");
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            if (scale.HasValue) go.transform.localScale = scale.Value;
            var mat = WorkshopMaterials.Get(material);
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
                r.shadowCastingMode = ShadowCastingMode.On;
            }
            if (collider)
            {
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                {
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
            }
            return go;
        }

        private static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, string material, bool isStatic = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = MeshFactory.Box("Box_" + name, size);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = WorkshopMaterials.Get(material);
            var bc = go.AddComponent<BoxCollider>();
            bc.size = size;
            if (isStatic) GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic | StaticEditorFlags.OccludeeStatic);
            return go;
        }

        private static void BuildRoom(Transform parent)
        {
            float t = 0.2f;
            Box("Floor", parent, new Vector3(0f, -0.05f, 0f), new Vector3(RoomW, 0.1f, RoomD), "M_Concrete");
            Box("Ceiling", parent, new Vector3(0f, RoomH + 0.05f, 0f), new Vector3(RoomW, 0.1f, RoomD), "M_Ceiling");
            Box("WallNorth", parent, new Vector3(0f, RoomH / 2f, RoomD / 2f + t / 2f), new Vector3(RoomW + 2 * t, RoomH, t), "M_PlasterWarm");
            Box("WainscotN", parent, new Vector3(0f, 0.55f, RoomD / 2f - 0.015f), new Vector3(RoomW, 1.1f, 0.03f), "M_Wainscot");
            Box("WainscotW", parent, new Vector3(-RoomW / 2f + 0.015f, 0.55f, 0f), new Vector3(0.03f, 1.1f, RoomD), "M_Wainscot");
            Box("WallSouth", parent, new Vector3(0f, RoomH / 2f, -RoomD / 2f - t / 2f), new Vector3(RoomW + 2 * t, RoomH, t), "M_Plaster");
            Box("WallEast", parent, new Vector3(RoomW / 2f + t / 2f, RoomH / 2f, 0f), new Vector3(t, RoomH, RoomD), "M_Plaster");
            Box("WallWest", parent, new Vector3(-RoomW / 2f - t / 2f, RoomH / 2f, 0f), new Vector3(t, RoomH, RoomD), "M_Plaster");
            // skirting / trim
            Box("SkirtN", parent, new Vector3(0f, 0.05f, RoomD / 2f - 0.02f), new Vector3(RoomW, 0.1f, 0.04f), "M_WoodDark");
            Box("SkirtS", parent, new Vector3(0f, 0.05f, -RoomD / 2f + 0.02f), new Vector3(RoomW, 0.1f, 0.04f), "M_WoodDark");
            Box("SkirtE", parent, new Vector3(RoomW / 2f - 0.02f, 0.05f, 0f), new Vector3(0.04f, 0.1f, RoomD), "M_WoodDark");
            Box("SkirtW", parent, new Vector3(-RoomW / 2f + 0.02f, 0.05f, 0f), new Vector3(0.04f, 0.1f, RoomD), "M_WoodDark");
            // rubber mat in front of the bench
            Box("BenchMat", parent, new Vector3(0f, 0.006f, 1.35f), new Vector3(2.2f, 0.012f, 1.0f), "M_Rubber");

            // window on the east wall (frame + glass + outside backdrop)
            var win = Prop("prop_window_frame", parent, new Vector3(RoomW / 2f - 0.02f, 1.2f, 0.4f), -90f, "M_WoodPainted", collider: false);
            var glass = Box("WindowGlass", parent, new Vector3(RoomW / 2f + 0.06f, 1.7f, 0.4f), new Vector3(0.02f, 0.96f, 1.16f), "M_Glass");
            glass.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Object.DestroyImmediate(glass.GetComponent<Collider>());
            var backdrop = Box("WindowBackdrop", parent, new Vector3(RoomW / 2f + 0.9f, 1.7f, 0.4f), new Vector3(0.05f, 2.4f, 3f), "M_Plaster");
            backdrop.GetComponent<MeshRenderer>().sharedMaterial = WorkshopMaterials.Get("M_Glass");
            Object.DestroyImmediate(backdrop.GetComponent<Collider>());
            // wall cut-out behind the window: a bright emissive panel reads as daylight
            var sky = Box("WindowSky", parent, new Vector3(RoomW / 2f + 0.16f, 1.7f, 0.4f), new Vector3(0.02f, 0.96f, 1.16f), "M_Paper");
            var skyMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "M_WindowSky" };
            skyMat.SetColor("_BaseColor", new Color(1.6f, 1.75f, 2.0f));
            AssetDatabase.CreateAsset(skyMat, WorkshopMaterials.Folder + "/M_WindowSky.mat");
            sky.GetComponent<MeshRenderer>().sharedMaterial = skyMat;
            Object.DestroyImmediate(sky.GetComponent<Collider>());

            // door on the south wall
            Prop("prop_door", parent, new Vector3(-2.3f, 0f, -RoomD / 2f + 0.06f), 0f, "M_WoodPainted", collider: true);
        }

        private static Light MakeLight(Transform parent, string name, Vector3 pos, Vector3 euler, LightType type, Color color, float intensity, float range, float spot, bool shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            var l = go.AddComponent<Light>();
            l.type = type;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.spotAngle = spot;
            l.innerSpotAngle = spot * 0.6f;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            l.shadowStrength = 0.85f;
            l.shadowBias = 0.03f;
            l.shadowNormalBias = 0.4f;
            return l;
        }

        private static void BuildLighting(Transform parent)
        {
            var lights = new GameObject("Lights").transform;
            lights.SetParent(parent, false);
            // soft daylight through the window
            var sun = MakeLight(lights, "WindowLight", new Vector3(0f, 2.5f, 0f), new Vector3(28f, -68f, 0f), LightType.Directional, new Color(0.78f, 0.86f, 1f), 0.55f, 10f, 0f, true);
            sun.shadowStrength = 0.6f;
            // warm ceiling pendant
            MakeLight(lights, "CeilingLamp", new Vector3(0f, 2.75f, -0.2f), Vector3.zero, LightType.Point, new Color(1f, 0.88f, 0.72f), 3.6f, 10f, 0f, true);
            // second pendant near the door/receiving side
            MakeLight(lights, "CeilingLamp2", new Vector3(2.4f, 2.7f, -1.4f), Vector3.zero, LightType.Point, new Color(1f, 0.9f, 0.76f), 2.2f, 7f, 0f, false);
            MakeLight(lights, "CeilingLamp3", new Vector3(-2.4f, 2.7f, -1.2f), Vector3.zero, LightType.Point, new Color(1f, 0.9f, 0.76f), 2.0f, 7f, 0f, false);
            // cabinet spots (cool white, on the east wall cabinet)
            MakeLight(lights, "CabinetSpotA", new Vector3(2.55f, 2.3f, 0.45f), new Vector3(62f, 90f, 0f), LightType.Spot, new Color(0.92f, 0.95f, 1f), 2.2f, 3.2f, 60f, false);
            MakeLight(lights, "CabinetSpotB", new Vector3(2.55f, 2.3f, 1.35f), new Vector3(62f, 90f, 0f), LightType.Spot, new Color(0.92f, 0.95f, 1f), 2.2f, 3.2f, 60f, false);
            // reflection probe for crystals
            var probeGo = new GameObject("ReflectionProbe");
            probeGo.transform.SetParent(lights, false);
            probeGo.transform.localPosition = new Vector3(0f, 1.4f, 0.5f);
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.resolution = 128;
            probe.size = new Vector3(RoomW, RoomH, RoomD);
            probe.boxProjection = true;
            probe.intensity = 1f;
        }

        private static PlacementZone Zone(Transform parent, string name, Vector3 localPos, ZoneKind kind, string label, int capacity, bool opened, bool unopened, Vector3 triggerSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var z = go.AddComponent<PlacementZone>();
            z.Kind = kind;
            z.DisplayLabel = label;
            z.Capacity = capacity;
            z.AcceptsOpened = opened;
            z.AcceptsUnopened = unopened;
            var bc = go.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = triggerSize;
            bc.center = new Vector3(0f, triggerSize.y * 0.5f, 0f);
            return z;
        }

        private static void BuildStations(Transform parent)
        {
            // ---- Cracking bench (north wall) ---------------------------------------------------
            var bench = new GameObject("CrackingBench").transform;
            bench.SetParent(parent, false);
            bench.localPosition = new Vector3(0f, 0f, 2.15f);
            Prop("prop_workbench", bench, Vector3.zero, 0f, "M_Wood");
            var cradleProp = Prop("prop_cradle", bench, new Vector3(0.25f, 0.9f, -0.05f), 0f, "M_Rubber", collider: false);
            var cradleZone = Zone(bench, "CradleZone", new Vector3(0.25f, 0.9f, -0.05f), ZoneKind.Cradle, "the cradle", 1, true, true, new Vector3(0.32f, 0.22f, 0.32f));
            cradleZone.SetHighlightRenderers(cradleProp.GetComponentsInChildren<Renderer>());
            var cradleAnchor = new GameObject("Anchor").transform;
            cradleAnchor.SetParent(cradleZone.transform, false);
            cradleAnchor.localPosition = new Vector3(0f, 0.055f, 0f);
            cradleZone.Anchor = cradleAnchor;
            var camAnchor = new GameObject("BenchCamera").transform;
            camAnchor.SetParent(bench, false);
            camAnchor.localPosition = new Vector3(0.25f, 1.3f, -0.5f);
            camAnchor.LookAt(bench.TransformPoint(new Vector3(0.25f, 0.97f, -0.05f)));
            var chisel = Prop("prop_chisel", bench, new Vector3(-0.35f, 0.935f, -0.15f), 0f, "M_Metal", collider: false);
            chisel.transform.localRotation = Quaternion.Euler(90f, 30f, 0f);
            var hammer = Prop("prop_hammer", bench, new Vector3(-0.55f, 0.935f, 0.05f), 0f, "M_MetalDark", collider: false);
            hammer.transform.localRotation = Quaternion.Euler(0f, -20f, 90f);
            var lampProp = Prop("prop_task_lamp", bench, new Vector3(1.05f, 0.9f, 0.36f), 220f, "M_MetalDark", collider: false);
            var taskLight = MakeLight(bench, "TaskLight", new Vector3(0.62f, 1.32f, 0.05f), new Vector3(58f, -110f, 0f), LightType.Spot, new Color(1f, 0.92f, 0.8f), 2.4f, 2.6f, 62f, true);
            Prop("prop_pegboard", bench, new Vector3(0f, 1.35f, 0.5f), 0f, "M_Wood", collider: false);
            Prop("prop_bucket", bench, new Vector3(-1.2f, 0f, -0.1f), 0f, "M_PlasticBlue");
            Prop("prop_stool", bench, new Vector3(0.95f, 0f, -0.75f), 25f, "M_WoodDark");
            var cb = bench.gameObject.AddComponent<CrackingBench>();
            cb.Cradle = cradleZone;
            cb.CradleCenter = cradleAnchor;
            cb.CameraAnchor = camAnchor;
            cb.ChiselVisual = chisel.transform;
            cb.HammerVisual = hammer.transform;
            cb.TaskLight = taskLight;

            // ---- Appraisal bench (west wall) --------------------------------------------------
            var appraisal = new GameObject("AppraisalStation").transform;
            appraisal.SetParent(parent, false);
            appraisal.localPosition = new Vector3(-3.0f, 0f, 0.55f);
            appraisal.localRotation = Quaternion.Euler(0f, 90f, 0f);
            Prop("prop_workbench", appraisal, Vector3.zero, 0f, "M_WoodDark", scale: new Vector3(0.75f, 1f, 0.85f));
            var scaleProp = Prop("prop_scale_station", appraisal, new Vector3(0.15f, 0.9f, -0.02f), 180f, "M_MetalDark", collider: false);
            var scaleZone = Zone(appraisal, "ScaleZone", new Vector3(0.15f, 0.945f, -0.04f), ZoneKind.Scale, "the scale", 1, true, false, new Vector3(0.3f, 0.2f, 0.3f));
            scaleZone.SetHighlightRenderers(scaleProp.GetComponentsInChildren<Renderer>());
            var scaleAnchor = new GameObject("Anchor").transform;
            scaleAnchor.SetParent(scaleZone.transform, false);
            scaleZone.Anchor = scaleAnchor;
            var ap = appraisal.gameObject.AddComponent<AppraisalStation>();
            ap.Scale = scaleZone;
            var tabletProp = Prop("prop_tablet", appraisal, new Vector3(-0.42f, 0.9f, 0.1f), 15f, "M_Plastic", collider: true);
            var tablet = tabletProp.AddComponent<OrderTablet>();
            var screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screen.name = "Screen";
            screen.transform.SetParent(tabletProp.transform, false);
            screen.transform.localPosition = new Vector3(0f, 0.09f, -0.008f);
            screen.transform.localRotation = Quaternion.Euler(-20f, 180f, 0f);
            screen.transform.localScale = new Vector3(0.23f, 0.15f, 1f);
            screen.GetComponent<MeshRenderer>().sharedMaterial = WorkshopMaterials.Get("M_Screen");
            Object.DestroyImmediate(screen.GetComponent<Collider>());
            var shelf = new GameObject("StorageShelf").transform;
            shelf.SetParent(parent, false);
            shelf.localPosition = new Vector3(-3.4f, 0f, 2.1f);
            shelf.localRotation = Quaternion.Euler(0f, 90f, 0f);
            Prop("prop_shelf_unit", shelf, Vector3.zero, 0f, "M_WoodDark");
            Prop("prop_cardboard_box", shelf, new Vector3(-0.2f, 0.135f, 0f), 12f, "M_Cardboard");
            Prop("prop_cardboard_box", shelf, new Vector3(0.25f, 0.685f, 0f), -8f, "M_Cardboard", scale: new Vector3(0.7f, 0.7f, 0.7f));
            Prop("prop_bucket", shelf, new Vector3(0.1f, 1.235f, 0f), 0f, "M_Plastic", scale: new Vector3(0.7f, 0.7f, 0.7f));

            // ---- Dealer outbox + intercom (south-west, near the door) --------------------------
            var outbox = new GameObject("SellOutbox").transform;
            outbox.SetParent(parent, false);
            outbox.localPosition = new Vector3(-1.2f, 0f, -2.1f);
            Prop("prop_pallet", outbox, Vector3.zero, 0f, "M_Wood");
            var trayProp = Prop("prop_tray", outbox, new Vector3(0f, 0.12f, 0f), 0f, "M_PlasticBlue", collider: true, scale: new Vector3(1.6f, 1.2f, 1.6f));
            var trayZone = Zone(outbox, "OutboxZone", new Vector3(0f, 0.135f, 0f), ZoneKind.SellTray, "the dealer outbox", 12, true, false, new Vector3(0.78f, 0.25f, 0.56f));
            trayZone.GridColumns = 4;
            trayZone.GridSpacing = new Vector2(0.17f, 0.15f);
            trayZone.SetHighlightRenderers(trayProp.GetComponentsInChildren<Renderer>());
            var trayAnchor = new GameObject("Anchor").transform;
            trayAnchor.SetParent(trayZone.transform, false);
            trayZone.Anchor = trayAnchor;
            var so = outbox.gameObject.AddComponent<SellOutbox>();
            so.Tray = trayZone;
            var intercom = Box("DealerIntercom", outbox, new Vector3(0.75f, 1.35f, -0.56f), new Vector3(0.16f, 0.22f, 0.06f), "M_Plastic", isStatic: false);
            var button = Box("Button", intercom.transform, new Vector3(0f, -0.05f, -0.035f), new Vector3(0.06f, 0.03f, 0.02f), "M_Metal", isStatic: false);
            var ic = intercom.AddComponent<DealerIntercom>();
            ic.Outbox = so;
            var signOut = Prop("prop_label_stand", outbox, new Vector3(0.45f, 0.125f, -0.25f), -20f, "M_Paper", collider: false, scale: new Vector3(2.5f, 2.5f, 2.5f));

            // ---- Receiving pallet (south-east, near the door) ----------------------------------
            var receiving = new GameObject("ReceivingArea").transform;
            receiving.SetParent(parent, false);
            receiving.localPosition = new Vector3(2.3f, 0f, -2.0f);
            Prop("prop_pallet", receiving, Vector3.zero, 0f, "M_Wood");
            receiving.gameObject.AddComponent<ReceivingArea>();

            // ---- Display cabinet (east wall, visible from the bench) -------------------------
            var cabinet = new GameObject("DisplayCabinet").transform;
            cabinet.SetParent(parent, false);
            cabinet.localPosition = new Vector3(3.35f, 0f, 0.9f);
            cabinet.localRotation = Quaternion.Euler(0f, -90f, 0f);
            var cabProp = Prop("prop_display_cabinet", cabinet, Vector3.zero, 0f, "M_WoodDark");
            var dc = cabinet.gameObject.AddComponent<DisplayCabinet>();
            dc.LabelFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/GeodeEmpire/UI/Fonts/Roboto-Medium.ttf");
            int slot = 0;
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    float y = 0.215f + row * 0.5f;
                    float x = (col - 1.5f) * 0.29f;
                    var z = Zone(cabinet, $"Slot{slot}", new Vector3(x, y, 0.04f), ZoneKind.DisplaySlot, $"display slot {slot + 1}", 1, true, false, new Vector3(0.28f, 0.4f, 0.4f));
                    z.SlotIndex = slot;
                    var a = new GameObject("Anchor").transform;
                    a.SetParent(z.transform, false);
                    z.Anchor = a;
                    dc.Slots.Add(z);
                    slot++;
                }
            }

            // ---- Saw teaser + clutter -----------------------------------------------------------
            var teaser = Prop("prop_saw_teaser", parent, new Vector3(2.55f, 0f, -0.45f), -90f, "M_Tarp");
            var ts = teaser.AddComponent<TeaserSign>();
            Prop("prop_cardboard_box", parent, new Vector3(-2.9f, 0f, -1.4f), 30f, "M_Cardboard");
            Prop("prop_cardboard_box", parent, new Vector3(-2.7f, 0f, -0.9f), -10f, "M_Cardboard", scale: new Vector3(0.8f, 0.9f, 0.8f));
            Prop("prop_bucket", parent, new Vector3(2.9f, 0f, 2.2f), 0f, "M_Plastic");

            var start = new GameObject("PlayerStart");
            start.transform.SetParent(parent, false);
            start.transform.localPosition = new Vector3(-0.3f, 0f, -0.6f);
            start.transform.localRotation = Quaternion.Euler(0f, 10f, 0f);
            start.AddComponent<PlayerStart>();
        }

        private static void BuildPlayer()
        {
            var player = new GameObject("Player");
            player.layer = 2;
            player.transform.position = new Vector3(-0.3f, 0f, -0.6f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 45f;
            cc.skinWidth = 0.04f;
            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(player.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            var camGo = new GameObject("Camera");
            camGo.transform.SetParent(pivot.transform, false);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.04f;
            cam.farClipPlane = 60f;
            cam.fieldOfView = 70f;
            camGo.AddComponent<AudioListener>();
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            var fpc = player.AddComponent<FirstPersonController>();
            fpc.CameraPivot = pivot.transform;
            fpc.Camera = cam;
            var pi = player.AddComponent<PlayerInteractor>();
            pi.Cam = cam;
            pi.Controller = fpc;
        }

        private static void BuildSystems(PanelSettings panel, VolumeProfile profile)
        {
            var sys = new GameObject("GameSession");
            sys.AddComponent<GameSession>();
            var fx = new GameObject("Effects");
            fx.AddComponent<EffectsFactory>();
            var hud = new GameObject("HUD");
            var doc = hud.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            doc.sortingOrder = 0;
            hud.AddComponent<HudController>();
            hud.AddComponent<BenchHud>();
            hud.AddComponent<TabletUI>();
            hud.AddComponent<AppraisalUI>();
            hud.AddComponent<PauseMenu>();
            hud.AddComponent<SliceDirector>();
            var vol = new GameObject("GlobalVolume");
            var v = vol.AddComponent<Volume>();
            v.isGlobal = true;
            v.sharedProfile = profile;
            var amb = new GameObject("Ambience");
            var src = amb.AddComponent<AudioSource>();
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            amb.AddComponent<AmbiencePlayer>();
        }

        private static void AddToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == scenePath || s.path.EndsWith("SampleScene.unity"));
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
