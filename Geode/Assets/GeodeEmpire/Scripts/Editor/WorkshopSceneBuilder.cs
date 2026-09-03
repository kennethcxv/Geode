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
using GeodeEmpire.Retail;
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
            Lit("M_Steel", "T_Metal", new Color(0.58f, 0.6f, 0.63f), 0.7f, 0.9f, 1f);
            Lit("M_Felt", null, new Color(0.12f, 0.24f, 0.18f), 0.08f, 0f, 1f);
            Lit("M_CaseLight", null, new Color(1f, 0.97f, 0.9f), 0.3f, 0f, 1f, emission: new Color(2.4f, 2.2f, 1.9f));
            Lit("M_Register", null, new Color(0.2f, 0.2f, 0.21f), 0.5f, 0.1f, 1f);
            Lit("M_CounterPaint", null, new Color(0.24f, 0.3f, 0.28f), 0.4f, 0f, 1f);
            Lit("M_Brass", "T_Metal", new Color(0.78f, 0.62f, 0.34f), 0.68f, 0.9f, 1f);
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
            Lit("M_Red", null, new Color(0.62f, 0.1f, 0.08f), 0.55f, 0.2f, 1f);
            Lit("M_Bulb", null, new Color(1f, 0.9f, 0.75f), 0.4f, 0f, 1f, emission: new Color(2.2f, 1.7f, 1.1f));
            Lit("M_JarGlass", null, new Color(0.8f, 0.85f, 0.8f, 0.35f), 0.9f, 0f, 1f);
            Lit("M_Cream", null, new Color(0.88f, 0.86f, 0.8f), 0.25f, 0f, 1f);
            PosterGenerator.EnsurePosters();
            Lit("M_PosterMinerals", "T_PosterMinerals", Color.white, 0.25f, 0f, 1f);
            Lit("M_PosterRocks", "T_PosterRocks", Color.white, 0.25f, 0f, 1f);
            Lit("M_PlasterWarm", "T_Plaster", new Color(0.84f, 0.81f, 0.74f), 0.12f, 0f, 2f);
            Lit("M_Wainscot", "T_WoodDark", new Color(0.7f, 0.66f, 0.6f), 0.35f, 0f, 1f);
            var glass = Lit("M_Glass", null, new Color(0.7f, 0.85f, 0.95f, 0.18f), 0.95f, 0f, 1f);
            SetTransparent(glass);
            // loupe lens: magnifies the opaque scene behind it
            string lensPath = Folder + "/M_LoupeLens.mat";
            var lens = AssetDatabase.LoadAssetAtPath<Material>(lensPath);
            var lensShader = Shader.Find("GeodeEmpire/LoupeLens");
            if (lens == null) { lens = new Material(lensShader); AssetDatabase.CreateAsset(lens, lensPath); }
            if (lens.shader != lensShader) lens.shader = lensShader;
            EditorUtility.SetDirty(lens);
            SetTransparent(AssetDatabase.LoadAssetAtPath<Material>(Folder + "/M_JarGlass.mat"));
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
        public const string PanelSettingsPath = "Assets/GeodeEmpire/Resources/UI/GeodePanelSettings.asset";
        public const string VolumeProfilePath = "Assets/GeodeEmpire/Data/WorkshopVolume.asset";
        public const string CratePrefabPath = "Assets/GeodeEmpire/Resources/Prefabs/Crate.prefab";

        // Room dimensions (metres): the workshop (x < PartitionX) and the showroom (x > PartitionX) share one building
        const float RoomXMin = -3.6f, RoomXMax = 7.0f, RoomD = 5.4f, RoomH = 3.0f;
        const float RoomW = RoomXMax - RoomXMin;
        const float RoomCX = (RoomXMin + RoomXMax) * 0.5f;
        const float PartitionX = 2.55f;
        const float ShopDoorX = 5.6f;

        /// <summary>Forward+ so a room full of lamps is not capped at four lights per object.</summary>
        private static void EnsureAlwaysIncludedShader(string name)
        {
            var sh = Shader.Find(name);
            if (sh == null) { Debug.LogWarning("[SceneBuilder] shader not found: " + name); return; }
            var gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            if (gs == null) return;
            var so = new SerializedObject(gs);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            for (int i = 0; i < arr.arraySize; i++) if (arr.GetArrayElementAtIndex(i).objectReferenceValue == sh) return;
            arr.InsertArrayElementAtIndex(arr.arraySize);
            arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        public static void EnsureRendererSettings()
        {
            // shaders the runtime creates materials for by name are not referenced by any asset, so a player build
            // would strip them: pin them in Always Included Shaders
            EnsureAlwaysIncludedShader("Universal Render Pipeline/Particles/Unlit");
            var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            if (rd != null && rd.renderingMode != RenderingMode.ForwardPlus)
            {
                rd.renderingMode = RenderingMode.ForwardPlus;
                EditorUtility.SetDirty(rd);
            }
            var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            if (rp != null)
            {
                rp.shadowDistance = 18f;
                rp.shadowCascadeCount = 2;
                rp.supportsHDR = true;
                // specimens drive their shell/crystal materials through property blocks; the resident drawer keeps
                // drawing such renderers with material defaults on top of the classic draw (z-fighting patches)
                rp.gpuResidentDrawerMode = GPUResidentDrawerMode.Disabled;
                // the loupe lens samples the opaque texture: keep the copy full resolution (property is read-only at runtime)
                var so = new SerializedObject(rp);
                var od = so.FindProperty("m_OpaqueDownsampling");
                if (od != null) { od.intValue = 0; so.ApplyModifiedPropertiesWithoutUndo(); }
                EditorUtility.SetDirty(rp);
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("GeodeEmpire/Build Workshop Scene")]
        public static void Build()
        {
            EnsureRendererSettings();
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
        public static PanelSettings EnsurePanelSettings()
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
            var body = Prop("prop_crate_body", root.transform, Vector3.zero, 0f, "M_Wood", collider: true, scale: new Vector3(1.35f, 1.15f, 1.35f));
            // straw bed material on the straw disc: the prop is one mesh, so whole crate is wood; add a straw disc
            var straw = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            straw.name = "Straw";
            straw.transform.SetParent(root.transform, false);
            // the body's floor top is ~0.085 up (scaled plank); the straw bed shows above it and the rocks rest on it
            straw.transform.localPosition = new Vector3(0f, 0.072f, 0f);
            straw.transform.localScale = new Vector3(0.7f, 0.012f, 0.5f);
            straw.GetComponent<MeshRenderer>().sharedMaterial = WorkshopMaterials.Get("M_Straw");
            Object.DestroyImmediate(straw.GetComponent<Collider>());
            var lid = Prop("prop_crate_lid", root.transform, new Vector3(0f, 0.39f, 0f), 0f, "M_Wood", collider: false, scale: new Vector3(1.35f, 1f, 1.35f));
            lid.name = "Lid";
            var bed = new GameObject("Bed");
            bed.transform.SetParent(root.transform, false);
            bed.transform.localPosition = new Vector3(0f, 0.09f, 0f);
            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.2f, 0f);
            box.size = new Vector3(0.86f, 0.4f, 0.64f);
            var ce = root.AddComponent<CrateEntity>();
            ce.BedSize = new Vector2(0.72f, 0.5f);
            ce.Lid = lid.transform;
            ce.Bed = bed.transform;
            PrefabUtility.SaveAsPrefabAsset(root, CratePrefabPath);
            Object.DestroyImmediate(root);
        }

        // ------------------------------------------------------------------------------------
        /// <param name="material">material for every slot, or a comma-separated list per material slot (e.g. "M_WoodDark,M_Metal")</param>
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
            var names = material.Split(',');
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = WorkshopMaterials.Get(names[Mathf.Min(i, names.Length - 1)].Trim());
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
            Box("Floor", parent, new Vector3(RoomCX, -0.05f, 0f), new Vector3(RoomW, 0.1f, RoomD), "M_Concrete");
            // the showroom side gets a warmer plank floor laid over the slab
            Box("ShopFloor", parent, new Vector3((PartitionX + RoomXMax) * 0.5f, 0.004f, 0f), new Vector3(RoomXMax - PartitionX, 0.008f, RoomD), "M_WoodDark");
            Box("Ceiling", parent, new Vector3(RoomCX, RoomH + 0.05f, 0f), new Vector3(RoomW, 0.1f, RoomD), "M_Ceiling");
            Box("WallNorth", parent, new Vector3(RoomCX, RoomH / 2f, RoomD / 2f + t / 2f), new Vector3(RoomW + 2 * t, RoomH, t), "M_PlasterWarm");
            Box("WainscotN", parent, new Vector3(RoomCX, 0.55f, RoomD / 2f - 0.015f), new Vector3(RoomW, 1.1f, 0.03f), "M_Wainscot");
            Box("WainscotW", parent, new Vector3(RoomXMin + 0.015f, 0.55f, 0f), new Vector3(0.03f, 1.1f, RoomD), "M_Wainscot");
            Box("WainscotE", parent, new Vector3(RoomXMax - 0.015f, 0.55f, 0f), new Vector3(0.03f, 1.1f, RoomD), "M_Wainscot");
            // south wall in two pieces around the shop entrance, with a lintel above the door
            float doorHalf = 0.5f;
            float sA0 = RoomXMin - t, sA1 = ShopDoorX - doorHalf, sB0 = ShopDoorX + doorHalf, sB1 = RoomXMax + t;
            Box("WallSouthA", parent, new Vector3((sA0 + sA1) * 0.5f, RoomH / 2f, -RoomD / 2f - t / 2f), new Vector3(sA1 - sA0, RoomH, t), "M_Plaster");
            Box("WallSouthB", parent, new Vector3((sB0 + sB1) * 0.5f, RoomH / 2f, -RoomD / 2f - t / 2f), new Vector3(sB1 - sB0, RoomH, t), "M_Plaster");
            Box("DoorLintel", parent, new Vector3(ShopDoorX, (2.15f + RoomH) * 0.5f, -RoomD / 2f - t / 2f), new Vector3(doorHalf * 2f + 0.02f, RoomH - 2.15f, t), "M_Plaster");
            Box("WainscotSA", parent, new Vector3((RoomXMin + sA1) * 0.5f, 0.55f, -RoomD / 2f + 0.015f), new Vector3(sA1 - RoomXMin, 1.1f, 0.03f), "M_Wainscot");
            Box("WainscotSB", parent, new Vector3((sB0 + RoomXMax) * 0.5f, 0.55f, -RoomD / 2f + 0.015f), new Vector3(RoomXMax - sB0, 1.1f, 0.03f), "M_Wainscot");
            Box("WallEast", parent, new Vector3(RoomXMax + t / 2f, RoomH / 2f, 0f), new Vector3(t, RoomH, RoomD), "M_Plaster");
            Box("WallWest", parent, new Vector3(RoomXMin - t / 2f, RoomH / 2f, 0f), new Vector3(t, RoomH, RoomD), "M_Plaster");
            // partition between workshop and showroom: solid to the south of the counter, solid north of it, open by the bench
            Box("PartitionS", parent, new Vector3(PartitionX, RoomH / 2f, (-RoomD / 2f - 1.65f) * 0.5f), new Vector3(0.15f, RoomH, -1.65f - (-RoomD / 2f)), "M_PlasterWarm");   // south segment: z -2.7 .. -1.65, the counter gap starts at -1.65
            Box("PartitionN", parent, new Vector3(PartitionX, RoomH / 2f, (-0.35f + 0.9f) * 0.5f), new Vector3(0.15f, RoomH, 1.25f), "M_PlasterWarm");
            Box("PartitionHeader", parent, new Vector3(PartitionX, (2.25f + RoomH) * 0.5f, 1.8f), new Vector3(0.15f, RoomH - 2.25f, 1.8f), "M_PlasterWarm");
            Box("PartitionTrimS", parent, new Vector3(PartitionX, 0.55f, (-RoomD / 2f - 1.65f) * 0.5f), new Vector3(0.19f, 1.1f, -1.65f - (-RoomD / 2f)), "M_Wainscot");
            Box("PartitionTrimN", parent, new Vector3(PartitionX, 0.55f, (-0.35f + 0.9f) * 0.5f), new Vector3(0.19f, 1.1f, 1.25f), "M_Wainscot");
            // ceiling beams and a service pipe give the ceiling some structure
            for (int i = -2; i <= 2; i++) Box("Beam" + i, parent, new Vector3(RoomCX + i * 2.4f, RoomH - 0.08f, 0f), new Vector3(0.14f, 0.16f, RoomD), "M_WoodDark");
            var pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipe.name = "Pipe"; pipe.transform.SetParent(parent, false);
            pipe.transform.localPosition = new Vector3(RoomCX, RoomH - 0.3f, RoomD / 2f - 0.08f);
            pipe.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            pipe.transform.localScale = new Vector3(0.06f, RoomW / 2f, 0.06f);
            pipe.GetComponent<MeshRenderer>().sharedMaterial = WorkshopMaterials.Get("M_MetalDark");
            Object.DestroyImmediate(pipe.GetComponent<Collider>());
            Box("DoorMat", parent, new Vector3(-2.3f, 0.006f, -RoomD / 2f + 0.5f), new Vector3(0.9f, 0.012f, 0.55f), "M_Rubber");
            Box("ShopMat", parent, new Vector3(ShopDoorX, 0.012f, -RoomD / 2f + 0.5f), new Vector3(1.1f, 0.012f, 0.6f), "M_Rubber");
            // skirting / trim
            Box("SkirtN", parent, new Vector3(RoomCX, 0.05f, RoomD / 2f - 0.02f), new Vector3(RoomW, 0.1f, 0.04f), "M_WoodDark");
            Box("SkirtSA", parent, new Vector3((RoomXMin + sA1) * 0.5f, 0.05f, -RoomD / 2f + 0.02f), new Vector3(sA1 - RoomXMin, 0.1f, 0.04f), "M_WoodDark");
            Box("SkirtSB", parent, new Vector3((sB0 + RoomXMax) * 0.5f, 0.05f, -RoomD / 2f + 0.02f), new Vector3(RoomXMax - sB0, 0.1f, 0.04f), "M_WoodDark");
            Box("SkirtE", parent, new Vector3(RoomXMax - 0.02f, 0.05f, 0f), new Vector3(0.04f, 0.1f, RoomD), "M_WoodDark");
            Box("SkirtW", parent, new Vector3(RoomXMin + 0.02f, 0.05f, 0f), new Vector3(0.04f, 0.1f, RoomD), "M_WoodDark");
            // rubber mat in front of the bench
            Box("BenchMat", parent, new Vector3(0f, 0.006f, 1.35f), new Vector3(2.2f, 0.012f, 1.0f), "M_Rubber");

            // shop window on the east wall, by the queue (frame + glass + outside backdrop)
            Prop("prop_window_frame", parent, new Vector3(RoomXMax - 0.02f, 1.2f, -1.7f), -90f, "M_WoodPainted", collider: false);
            var glass = Box("WindowGlass", parent, new Vector3(RoomXMax - 0.035f, 1.7f, -1.7f), new Vector3(0.01f, 0.96f, 1.16f), "M_Glass");
            glass.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Object.DestroyImmediate(glass.GetComponent<Collider>());
            var sky = Box("WindowSky", parent, new Vector3(RoomXMax - 0.012f, 1.7f, -1.7f), new Vector3(0.01f, 0.96f, 1.16f), "M_Paper");
            var skyMat = AssetDatabase.LoadAssetAtPath<Material>(WorkshopMaterials.Folder + "/M_WindowSky.mat");
            if (skyMat == null)
            {
                skyMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "M_WindowSky" };
                skyMat.SetColor("_BaseColor", new Color(1.6f, 1.75f, 2.0f));
                AssetDatabase.CreateAsset(skyMat, WorkshopMaterials.Folder + "/M_WindowSky.mat");
            }
            sky.GetComponent<MeshRenderer>().sharedMaterial = skyMat;
            Object.DestroyImmediate(sky.GetComponent<Collider>());

            // workshop back door on the south wall (decorative)
            Prop("prop_door", parent, new Vector3(-2.3f, 0f, -RoomD / 2f + 0.06f), 0f, "M_WoodPainted", collider: true);

            // the shop entrance: a hinged painted door in the gap, and a porch outside it
            var hinge = new GameObject("ShopDoorHinge");
            hinge.transform.SetParent(parent, false);
            hinge.transform.localPosition = new Vector3(ShopDoorX - doorHalf + 0.03f, 0f, -RoomD / 2f - 0.02f);
            var leaf = Box("ShopDoorLeaf", hinge.transform, new Vector3(doorHalf - 0.05f, 1.06f, 0f), new Vector3(doorHalf * 2f - 0.08f, 2.1f, 0.045f), "M_WoodPainted", isStatic: false);
            leaf.GetComponent<Collider>().enabled = false;   // the leaf swings; the wall gap is the doorway
            var pane = Box("DoorPane", leaf.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.5f, 0.8f, 0.06f), "M_Glass", isStatic: false);
            Object.DestroyImmediate(pane.GetComponent<Collider>());
            var knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            knob.name = "Knob"; knob.transform.SetParent(leaf.transform, false);
            knob.transform.localPosition = new Vector3(0.36f, -0.05f, 0.04f); knob.transform.localScale = Vector3.one * 0.05f;
            knob.GetComponent<MeshRenderer>().sharedMaterial = WorkshopMaterials.Get("M_Brass");
            Object.DestroyImmediate(knob.GetComponent<Collider>());
            Box("Porch", parent, new Vector3(ShopDoorX, -0.05f, -RoomD / 2f - 1.0f), new Vector3(3.2f, 0.1f, 2.0f), "M_Concrete");
            Box("PorchWallS", parent, new Vector3(ShopDoorX, RoomH / 2f, -RoomD / 2f - 2.05f), new Vector3(3.6f, RoomH, 0.2f), "M_Brick");
            Box("PorchWallW", parent, new Vector3(ShopDoorX - 1.7f, RoomH / 2f, -RoomD / 2f - 1.0f), new Vector3(0.2f, RoomH, 2.2f), "M_Brick");
            Box("PorchWallE", parent, new Vector3(ShopDoorX + 1.7f, RoomH / 2f, -RoomD / 2f - 1.0f), new Vector3(0.2f, RoomH, 2.2f), "M_Brick");
            Box("PorchRoof", parent, new Vector3(ShopDoorX, RoomH + 0.05f, -RoomD / 2f - 1.0f), new Vector3(3.6f, 0.1f, 2.2f), "M_Ceiling");
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
            // soft daylight through the shop window
            var sun = MakeLight(lights, "WindowLight", new Vector3(0f, 2.5f, 0f), new Vector3(28f, -68f, 0f), LightType.Directional, new Color(0.78f, 0.86f, 1f), 0.5f, 10f, 0f, true);
            sun.shadowStrength = 0.6f;
            // warm ceiling pendants (with visible fixtures): three in the workshop, two in the showroom
            foreach (var p in new[] { new Vector3(0f, 0f, -0.2f), new Vector3(1.4f, 0f, -1.6f), new Vector3(-2.4f, 0f, -1.2f), new Vector3(4.5f, 0f, -0.7f), new Vector3(5.7f, 0f, 1.3f) })
            {
                Pendant(parent, new Vector3(p.x, RoomH, p.z));
                MakeLight(lights, "Pendant", new Vector3(p.x, 2.2f, p.z), Vector3.zero, LightType.Point, new Color(1f, 0.9f, 0.76f), 2.6f, 7.5f, 0f, false);
            }
            // personal cabinet spots (cool white) from the room side of the partition
            MakeLight(lights, "CabinetSpotA", new Vector3(1.55f, 2.45f, -0.05f), new Vector3(62f, 90f, 0f), LightType.Spot, new Color(0.92f, 0.95f, 1f), 4.2f, 3.2f, 55f, false);
            MakeLight(lights, "CabinetSpotB", new Vector3(1.55f, 2.45f, 0.65f), new Vector3(62f, 90f, 0f), LightType.Spot, new Color(0.92f, 0.95f, 1f), 4.2f, 3.2f, 55f, false);
            // showroom: case spots and a counter light
            MakeLight(lights, "CaseSpotA", new Vector3(6.05f, 2.55f, -0.1f), new Vector3(58f, 90f, 0f), LightType.Spot, new Color(0.96f, 0.97f, 1f), 4.6f, 3.4f, 60f, false);
            MakeLight(lights, "CaseSpotB", new Vector3(6.05f, 2.55f, 0.9f), new Vector3(58f, 90f, 0f), LightType.Spot, new Color(0.96f, 0.97f, 1f), 4.6f, 3.4f, 60f, false);
            MakeLight(lights, "CounterLight", new Vector3(3.1f, 2.5f, -1.0f), new Vector3(75f, -90f, 0f), LightType.Spot, new Color(1f, 0.95f, 0.86f), 3.2f, 3.5f, 60f, false);
            MakeLight(lights, "PorchLamp", new Vector3(ShopDoorX, 2.5f, -RoomD / 2f - 0.6f), Vector3.zero, LightType.Point, new Color(0.8f, 0.88f, 1f), 1.6f, 4f, 0f, false);
            // reflection probe for crystals
            var probeGo = new GameObject("ReflectionProbe");
            probeGo.transform.SetParent(lights, false);
            probeGo.transform.localPosition = new Vector3(RoomCX, 1.4f, 0.5f);
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.resolution = 128;
            probe.size = new Vector3(RoomW, RoomH, RoomD);
            probe.boxProjection = true;
            probe.intensity = 1f;
        }

        private static void Pendant(Transform parent, Vector3 ceilingPoint)
        {
            var lamp = Prop("prop_pendant_lamp", parent, ceilingPoint, 0f, "M_MetalDark", collider: false);
            var bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "Bulb"; bulb.transform.SetParent(lamp.transform, false);
            bulb.transform.localPosition = new Vector3(0f, -0.72f, 0f);
            bulb.transform.localScale = Vector3.one * 0.07f;
            bulb.GetComponent<MeshRenderer>().sharedMaterial = WorkshopMaterials.Get("M_Bulb");
            Object.DestroyImmediate(bulb.GetComponent<Collider>());
        }

        /// <param name="intoWallYaw">world yaw of the direction pointing into the wall the sign hangs on</param>
        private static void Sign(Transform parent, string text, Vector3 pos, float intoWallYaw, float scale = 1f)
        {
            // text first, so the board can be sized to it; the label is a sibling (not a child) of the scaled board
            var rot = Quaternion.Euler(0f, intoWallYaw, 0f);
            var label = new GameObject("SignText");
            label.transform.SetParent(parent, false);
            label.transform.SetPositionAndRotation(pos + rot * new Vector3(0f, 0.112f * scale, -0.06f), rot);
            var tm = label.AddComponent<TextMesh>();
            tm.text = text; tm.characterSize = 0.03f * scale; tm.fontSize = 64; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.93f, 0.88f, 0.76f);
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/GeodeEmpire/UI/Fonts/Roboto-Bold.ttf");
            if (font != null) { tm.font = font; label.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
            var b = label.GetComponent<MeshRenderer>().bounds;
            float textWidth = Mathf.Max(b.size.x, b.size.z);
            if (textWidth < 0.1f) textWidth = text.Length * 0.115f * scale;          // mesh not generated yet in batch mode
            float boardWidth = textWidth + 0.22f * scale;
            // board: its thin axis is local Z; rotate so local +Z points into the wall. Base mesh is 0.5 x 0.14 m.
            Prop("prop_sign_board", parent, pos, intoWallYaw, "M_WoodDark", collider: false, scale: new Vector3(boardWidth / 0.5f, 1.6f * scale, 2.5f));
        }

        private static void Poster(Transform parent, string material, Vector3 pos, float yaw)
        {
            var frame = Prop("prop_poster_frame", parent, pos, yaw, "M_WoodDark", collider: false);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Print"; quad.transform.SetParent(frame.transform, false);
            quad.transform.localPosition = new Vector3(0f, 0.31f, -0.016f);
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(0.58f, 0.58f, 1f);
            quad.GetComponent<MeshRenderer>().sharedMaterial = WorkshopMaterials.Get(material);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
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
            var cradleProp = Prop("prop_cradle", bench, new Vector3(0.25f, 0.9f, -0.05f), 0f, "M_Rubber", collider: true);
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
            var chisel = Prop("prop_chisel", bench, new Vector3(-0.35f, 0.935f, -0.15f), 0f, "M_Steel", collider: false);
            chisel.transform.localRotation = Quaternion.Euler(90f, 30f, 0f);
            var hammer = Prop("prop_hammer", bench, new Vector3(-0.55f, 0.935f, 0.05f), 0f, "M_WoodDark,M_Steel", collider: false);
            hammer.transform.localRotation = Quaternion.Euler(0f, -20f, 90f);
            var lampProp = Prop("prop_task_lamp", bench, new Vector3(1.05f, 0.9f, 0.36f), 220f, "M_MetalDark", collider: false);
            var taskLight = MakeLight(bench, "TaskLight", new Vector3(0.62f, 1.32f, 0.05f), new Vector3(58f, -110f, 0f), LightType.Spot, new Color(0.97f, 0.97f, 0.95f), 2.2f, 2.6f, 62f, true);   // neutral daylight lamp: crystal colour stays honest
            Prop("prop_pegboard", bench, new Vector3(0f, 1.35f, 0.5f), 0f, "M_Wood", collider: false);
            Prop("prop_bucket", bench, new Vector3(-1.2f, 0f, -0.1f), 0f, "M_PlasticBlue");
            Prop("prop_stool", bench, new Vector3(0.95f, 0f, -0.75f), 25f, "M_WoodDark");
            var cb = bench.gameObject.AddComponent<CrackingBench>();
            cb.Cradle = cradleZone;
            cb.CradleCenter = cradleAnchor;
            cb.CameraAnchor = camAnchor;
            cb.ChiselVisual = chisel.transform;
            cb.ChiselLength = 0.17f;
            cb.HammerLen = 0.312f;
            cb.HammerHeadHalf = 0.066f;
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
            shelf.localRotation = Quaternion.Euler(0f, -90f, 0f);
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
            receiving.localPosition = new Vector3(1.5f, 0f, -1.9f);
            foreach (var cell in new[] { new Vector3(-0.6f, 0f, 0.4f), new Vector3(0.6f, 0f, 0.4f), new Vector3(-0.6f, 0f, -0.4f), new Vector3(0.6f, 0f, -0.4f) })
                Prop("prop_pallet", receiving, cell, 0f, "M_Wood");
            receiving.gameObject.AddComponent<ReceivingArea>();

            // ---- Display cabinet (east wall, visible from the bench) -------------------------
            var cabinet = new GameObject("DisplayCabinet").transform;
            cabinet.SetParent(parent, false);
            cabinet.localPosition = new Vector3(PartitionX - 0.3f, 0f, 0.28f);   // against the partition, facing the workshop
            cabinet.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var cabProp = Prop("prop_display_cabinet", cabinet, Vector3.zero, 0f, "M_WoodDark");
            var dc = cabinet.gameObject.AddComponent<DisplayCabinet>();
            dc.LabelFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/GeodeEmpire/UI/Fonts/Roboto-Medium.ttf");
            // LED strips under each shelf: the cabinet lights its own contents
            for (int row = 0; row < 3; row++)
                for (int side = -1; side <= 1; side += 2)
                {
                    var strip = MakeLight(cabinet, $"Strip{row}{side}", new Vector3(side * 0.3f, 0.2f + row * 0.5f + 0.42f, 0.1f), Vector3.zero, LightType.Point, new Color(1f, 0.97f, 0.9f), 0.55f, 0.85f, 0f, false);
                }
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
            var teaser = Prop("prop_saw_teaser", parent, new Vector3(1.75f, 0f, 2.3f), 180f, "M_Tarp");   // north wall by the partition opening, facing the room
            var ts = teaser.AddComponent<TeaserSign>();
            Prop("prop_cardboard_box", parent, new Vector3(-2.9f, 0f, -1.4f), 30f, "M_Cardboard");
            Prop("prop_cardboard_box", parent, new Vector3(-2.7f, 0f, -0.9f), -10f, "M_Cardboard", scale: new Vector3(0.8f, 0.9f, 0.8f));
            Prop("prop_bucket", parent, new Vector3(-1.45f, 0f, 2.5f), 0f, "M_Plastic");

            // ---- dressing: shelves, signs, posters, clutter ----------------------------------------
            var wallShelf = Prop("prop_wall_shelf", parent, new Vector3(-3.47f, 1.55f, 0.55f), -90f, "M_WoodDark", collider: false);
            Prop("prop_jar", wallShelf.transform, new Vector3(-0.3f, 0.03f, 0.02f), 0f, "M_JarGlass", collider: false);
            Prop("prop_jar", wallShelf.transform, new Vector3(-0.18f, 0.03f, -0.03f), 0f, "M_JarGlass", collider: false);
            Prop("prop_jar", wallShelf.transform, new Vector3(0.05f, 0.03f, 0.0f), 0f, "M_JarGlass", collider: false);
            Prop("prop_cardboard_box", wallShelf.transform, new Vector3(0.28f, 0.03f, 0.0f), 8f, "M_Cardboard", collider: false, scale: new Vector3(0.35f, 0.35f, 0.35f));
            Prop("prop_rock_bin", parent, new Vector3(0.55f, 0f, -2.35f), 8f, "M_WoodDark");
            Prop("prop_extinguisher", parent, new Vector3(-1.55f, 0f, -2.52f), 0f, "M_Red");
            Prop("prop_broom", parent, new Vector3(-3.35f, 0f, -2.35f), 20f, "M_Wood", collider: false).transform.localRotation = Quaternion.Euler(-6f, 20f, 8f);
            Prop("prop_wall_clock", parent, new Vector3(-2.3f, 2.5f, -RoomD / 2f + 0.02f), 180f, "M_Cream", collider: false);
            Poster(parent, "M_PosterMinerals", new Vector3(-1.5f, 1.25f, RoomD / 2f - 0.02f), 0f);
            Poster(parent, "M_PosterRocks", new Vector3(3.6f, 1.5f, RoomD / 2f - 0.02f), 0f);
            Sign(parent, "RECEIVING", new Vector3(1.5f, 1.55f, -RoomD / 2f + 0.03f), 180f);
            Sign(parent, "DEALER OUTBOX", new Vector3(-1.2f, 1.55f, -RoomD / 2f + 0.03f), 180f);
            Sign(parent, "GEODE WORKS", new Vector3(-2.3f, 2.25f, -RoomD / 2f + 0.03f), 180f, 1.3f);
            Sign(parent, "PRIVATE COLLECTION", new Vector3(PartitionX - 0.08f, 2.05f, 0.28f), 90f, 0.85f);
            BuildShowroom(parent, dc.LabelFont);
            // spare tools on the pegboard
            var pegHammer = Prop("prop_hammer", parent, new Vector3(-0.35f, 1.65f, RoomD / 2f - 0.07f), 0f, "M_WoodDark,M_Steel", collider: false);
            pegHammer.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            var pegChisel = Prop("prop_chisel_fine", parent, new Vector3(0.25f, 1.45f, RoomD / 2f - 0.07f), 0f, "M_Steel", collider: false);
            pegChisel.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            Prop("prop_stool", parent, new Vector3(-2.2f, 0f, 1.6f), -40f, "M_WoodDark");

            var start = new GameObject("PlayerStart");
            start.transform.SetParent(parent, false);
            start.transform.localPosition = new Vector3(-0.3f, 0f, -0.6f);
            start.transform.localRotation = Quaternion.Euler(0f, 10f, 0f);
            start.AddComponent<PlayerStart>();
        }


        /// <summary>The showroom east of the partition: pass-through counter with the register, a lit wall case, an island table, queue and door points, navigation.</summary>
        private static void BuildShowroom(Transform parent, Font labelFont)
        {
            var shop = new GameObject("RetailShop").transform;
            shop.SetParent(parent, false);
            var rs = shop.gameObject.AddComponent<RetailShop>();
            rs.LabelFont = labelFont;

            // counter set into the partition: cashier on the workshop side, customers on the shop side
            var counter = Prop("prop_counter", shop, new Vector3(PartitionX, 0f, -1.0f), -90f, "M_WoodDark,M_CounterPaint");
            var register = Prop("prop_register", shop, new Vector3(PartitionX - 0.1f, 0.95f, -1.42f), 90f, "M_Register,M_Screen", collider: true);
            var reg = register.AddComponent<CheckoutRegister>();
            reg.Shop = rs;
            reg.SetHighlightRenderers(register.GetComponentsInChildren<Renderer>());
            var itemPoint = new GameObject("CounterItemPoint").transform; itemPoint.SetParent(shop, false); itemPoint.localPosition = new Vector3(PartitionX + 0.04f, 0.95f, -0.72f); rs.CounterItemPoint = itemPoint;
            var custPoint = new GameObject("CounterCustomerPoint").transform; custPoint.SetParent(shop, false); custPoint.localPosition = new Vector3(3.35f, 0f, -0.85f); rs.CounterCustomerPoint = custPoint;
            foreach (var qx in new[] { 4.0f, 4.65f, 5.3f })
            {
                var q = new GameObject("QueuePoint").transform; q.SetParent(shop, false); q.localPosition = new Vector3(qx, 0f, -1.0f); rs.QueuePoints.Add(q);
            }
            var doorPoint = new GameObject("DoorPoint").transform; doorPoint.SetParent(shop, false); doorPoint.localPosition = new Vector3(ShopDoorX, 0f, -RoomD / 2f + 0.35f); rs.DoorPoint = doorPoint;
            var outside = new GameObject("OutsidePoint").transform; outside.SetParent(shop, false); outside.localPosition = new Vector3(ShopDoorX, 0f, -RoomD / 2f - 1.45f); outside.localRotation = Quaternion.Euler(0f, 180f, 0f); rs.OutsidePoint = outside;
            rs.DoorLeaf = GameObject.Find("ShopDoorHinge")?.transform;

            // wall case: two shelves of three
            var caseProp = Prop("prop_shop_case", shop, new Vector3(RoomXMax - 0.24f, 0f, 0.4f), 90f, "M_WoodDark,M_CaseLight");
            int slot = 0;
            foreach (float shelfY in new[] { 0.5625f, 1.0625f })
                foreach (float lx in new[] { -0.55f, 0f, 0.55f })
                {
                    var z = Zone(caseProp.transform, $"Sale{slot}", new Vector3(lx, shelfY, 0.03f), ZoneKind.SaleSlot, $"sales slot {slot + 1}", 1, true, false, new Vector3(0.36f, 0.34f, 0.32f));
                    z.SlotIndex = slot;
                    var a = new GameObject("Anchor").transform; a.SetParent(z.transform, false); z.Anchor = a;
                    rs.SaleSlots.Add(z);
                    var card = Prop("prop_price_card", caseProp.transform, new Vector3(lx, shelfY, -0.16f), 0f, "M_Paper,M_Paper", collider: false);
                    rs.PriceCards.Add(card.transform);
                    var bp = new GameObject("Browse").transform; bp.SetParent(caseProp.transform, false); bp.localPosition = new Vector3(lx, 0f, -0.9f); rs.BrowsePoints.Add(bp);
                    slot++;
                }
            // jewel-case lighting: a small shadowless lamp under the shelf above each pair of slots, so the pieces glow
            // inside the case instead of sitting in their own shadow
            foreach (float shelfY in new[] { 0.5625f, 1.0625f })
                foreach (float lx in new[] { -0.28f, 0.28f })
                {
                    var lg = new GameObject("CaseLamp");
                    lg.transform.SetParent(caseProp.transform, false);
                    lg.transform.localPosition = new Vector3(lx, shelfY + 0.36f, 0.02f);
                    var pl = lg.AddComponent<Light>();
                    pl.type = LightType.Point;
                    pl.color = new Color(1f, 0.96f, 0.88f);
                    pl.intensity = 0.55f;
                    pl.range = 0.75f;
                    pl.shadows = LightShadows.None;
                }
            // island table: four more, unlocked by the Showroom Island Table upgrade
            var table = Prop("prop_shop_table", shop, new Vector3(4.75f, 0f, 0.7f), 0f, "M_WoodDark,M_Felt");
            foreach (var lp in new[] { new Vector3(-0.32f, 0f, -0.17f), new Vector3(0.32f, 0f, -0.17f), new Vector3(-0.32f, 0f, 0.17f), new Vector3(0.32f, 0f, 0.17f) })
            {
                var z = Zone(table.transform, $"Sale{slot}", new Vector3(lp.x, 0.872f, lp.z), ZoneKind.SaleSlot, $"sales slot {slot + 1}", 1, true, false, new Vector3(0.3f, 0.34f, 0.3f));
                z.SlotIndex = slot;
                var a = new GameObject("Anchor").transform; a.SetParent(z.transform, false); z.Anchor = a;
                rs.SaleSlots.Add(z);
                float side = Mathf.Sign(lp.z);
                var card = Prop("prop_price_card", table.transform, new Vector3(lp.x, 0.872f, lp.z + side * 0.2f), side > 0f ? 0f : 180f, "M_Paper,M_Paper", collider: false);
                rs.PriceCards.Add(card.transform);
                var bp = new GameObject("Browse").transform; bp.SetParent(table.transform, false); bp.localPosition = new Vector3(lp.x, 0f, side * 0.95f); rs.BrowsePoints.Add(bp);
                slot++;
            }

            // signage and dressing
            Sign(parent, "GEODE WORKS  ·  SHOWROOM", new Vector3(PartitionX + 0.08f, 2.3f, -1.0f), -90f, 0.9f);
            Sign(parent, "FOR SALE", new Vector3(RoomXMax - 0.03f, 2.15f, 0.4f), 90f, 0.9f);
            Sign(parent, "OPEN", new Vector3(ShopDoorX + 0.9f, 2.5f, -RoomD / 2f + 0.03f), 180f, 0.7f);
            Prop("prop_stool", shop, new Vector3(2.0f, 0f, -1.35f), 40f, "M_WoodDark");
            Prop("prop_cardboard_box", shop, new Vector3(6.55f, 0f, -2.35f), 20f, "M_Cardboard", scale: new Vector3(0.8f, 0.8f, 0.8f));
            Prop("prop_label_stand", shop, new Vector3(PartitionX + 0.2f, 0.95f, -0.45f), 90f, "M_Paper", collider: false, scale: new Vector3(2f, 2f, 2f));

            // customers: a jointed figure template kept inactive, spawned by the shop
            var template = Prop("prop_customer", shop, Vector3.zero, 0f, "M_Plastic,M_Plastic,M_Cream,M_WoodDark", collider: false);
            template.name = "CustomerTemplate";
            var capsule = template.AddComponent<CapsuleCollider>();
            capsule.radius = 0.28f; capsule.height = 1.7f; capsule.center = new Vector3(0f, 0.85f, 0f);
            var agent = template.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.radius = 0.28f; agent.height = 1.7f;
            template.AddComponent<Customer>();
            template.SetActive(false);
            rs.CustomerTemplate = template;

            // navigation: the whole floor, minus the partition opening (customers stay in the showroom)
            var nav = new GameObject("Navigation");
            nav.transform.SetParent(shop, false);
            var surface = nav.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
            surface.collectObjects = Unity.AI.Navigation.CollectObjects.All;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~0;
            rs.Navigation = surface;
            var block = new GameObject("PartitionOpeningBlock");
            block.transform.SetParent(nav.transform, false);
            block.transform.localPosition = new Vector3(PartitionX, 1f, 1.8f);
            var mod = block.AddComponent<Unity.AI.Navigation.NavMeshModifierVolume>();
            mod.size = new Vector3(0.8f, 2f, 1.9f);
            mod.area = 1;   // Not Walkable
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
            var obstacle = player.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Capsule;
            obstacle.radius = 0.35f; obstacle.height = 1.8f; obstacle.center = new Vector3(0f, 0.9f, 0f);
            obstacle.carving = false;
            // the loupe lives in the hand: parked under the camera, raised by LoupeTool
            var loupe = Prop("prop_loupe", camGo.transform, new Vector3(0.05f, -0.16f, 0.2f), 0f, "M_Brass,M_LoupeLens", collider: false);
            foreach (var r in loupe.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            loupe.SetActive(false);
            var lt = player.AddComponent<LoupeTool>();
            lt.Loupe = loupe.transform;
            lt.Player = pi;
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
            hud.AddComponent<RetailUI>();
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
