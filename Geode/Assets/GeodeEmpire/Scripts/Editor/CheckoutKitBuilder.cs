using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GeodeEmpire.Checkout;

namespace GeodeEmpire.EditorTools
{
    /// <summary>
    /// Turns the imported Golf checkout kit (Models/Checkout/*.fbx + checkout_kit.json, written by
    /// Tools/Blender/import_golf_checkout.py) into Unity assets: URP materials rebuilt from the authored glTF PBR
    /// values, one prefab per model with its collision proxies converted to box colliders and hidden, and a
    /// CheckoutRig whose serialized Transform references are the durable binding for every anchor, socket, screen,
    /// key and drawer well. Nothing is looked up by string at runtime; a renamed node fails here instead.
    /// </summary>
    public static class CheckoutKitBuilder
    {
        public const string ModelFolder = "Assets/GeodeEmpire/Models/Checkout";
        public const string TextureFolder = "Assets/GeodeEmpire/Textures/Checkout";
        public const string MaterialFolder = "Assets/GeodeEmpire/Materials/Checkout";
        public const string PrefabFolder = "Assets/GeodeEmpire/Prefabs/Checkout";
        public const string LibraryPath = "Assets/GeodeEmpire/Resources/CheckoutPropLibrary.asset";
        private const string ManifestPath = ModelFolder + "/checkout_kit.json";

        /// <summary>Node-name prefixes/exact names the runtime binds to; everything matching becomes a serialized reference.</summary>
        private static bool IsRigNode(string name) =>
            name.StartsWith("ANCHOR_") || name.Contains("_SOCKET") || name.EndsWith("_MOUNT") || name.EndsWith("_PLACEMENT")
            || name.EndsWith("_AREA") || name.StartsWith("Terminal_") || name.StartsWith("CashDrawer_") || name.StartsWith("Bag_")
            || name.StartsWith("POS_") || name.StartsWith("CustDisp_") || name.StartsWith("Card_") || name == "CASH_ATTACH"
            || name.EndsWith("_Screen") || name == "Countertop" || name.StartsWith("LED_") || name.StartsWith("Bill_") || name == "Coin_Body";

        [MenuItem("Geode Empire/Build Checkout Kit")]
        public static void BuildAll()
        {
            string json = File.ReadAllText(ManifestPath);
            var root = MiniJson.Obj(MiniJson.Parse(json));
            var models = MiniJson.GetObj(root, "models");
            Directory.CreateDirectory(MaterialFolder);
            Directory.CreateDirectory(PrefabFolder);

            var materials = new Dictionary<string, Material>();
            foreach (var kv in models)
                foreach (var m in MiniJson.GetArr(MiniJson.Obj(kv.Value), "materials").Select(MiniJson.Obj))
                {
                    string name = MiniJson.GetStr(m, "name");
                    if (name == "M_Collision" || materials.ContainsKey(name)) continue;
                    materials[name] = BuildMaterial(name, m);
                }
            AssetDatabase.SaveAssets();

            var library = AssetDatabase.LoadAssetAtPath<CheckoutPropLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<CheckoutPropLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            library.Entries.Clear();

            var stems = models.Keys.OrderBy(s => s).ToList();
            foreach (var stem in stems)
            {
                var prefab = BuildPrefab(stem, MiniJson.Obj(models[stem]), materials);
                if (prefab != null) library.Entries.Add(new CheckoutPropLibrary.Entry { Stem = stem, Prefab = prefab });
            }
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CheckoutKit] built {materials.Count} materials and {library.Entries.Count} prefabs from {stems.Count} models");
        }

        private static Material BuildMaterial(string name, Dictionary<string, object> m)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            if (mat.shader != shader) mat.shader = shader;

            var bc = MiniJson.GetArr(m, "baseColor");
            var color = bc != null && bc.Count >= 3
                ? new Color(MiniJson.Num(bc[0], 1f), MiniJson.Num(bc[1], 1f), MiniJson.Num(bc[2], 1f), bc.Count > 3 ? MiniJson.Num(bc[3], 1f) : 1f)
                : Color.white;
            // the authored base colours are linear glTF factors; a textured slot keeps white so the sheet reads as painted
            string tex = MiniJson.GetStr(m, "texture");
            mat.SetColor("_BaseColor", tex != null ? Color.white : color);
            mat.SetTexture("_BaseMap", tex != null ? AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{tex}") : null);
            string nrm = MiniJson.GetStr(m, "normalTexture");
            var normal = nrm != null ? AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{nrm}") : null;
            mat.SetTexture("_BumpMap", normal);
            if (normal != null) mat.EnableKeyword("_NORMALMAP"); else mat.DisableKeyword("_NORMALMAP");
            mat.SetFloat("_BumpScale", 1f);
            mat.SetFloat("_Metallic", MiniJson.GetNum(m, "metallic"));
            mat.SetFloat("_Smoothness", Mathf.Clamp01(1f - MiniJson.GetNum(m, "roughness", 0.5f)));
            var em = MiniJson.GetArr(m, "emission");
            float strength = MiniJson.GetNum(m, "emissionStrength", 1f);
            var emission = em != null && em.Count >= 3
                ? new Color(MiniJson.Num(em[0]), MiniJson.Num(em[1]), MiniJson.Num(em[2])) * strength
                : Color.black;
            bool emissive = emission.maxColorComponent > 0.001f;
            mat.SetColor("_EmissionColor", emission);
            if (emissive) mat.EnableKeyword("_EMISSION"); else mat.DisableKeyword("_EMISSION");
            mat.globalIlluminationFlags = emissive ? MaterialGlobalIlluminationFlags.RealtimeEmissive : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject BuildPrefab(string stem, Dictionary<string, object> model, Dictionary<string, Material> materials)
        {
            string fbxPath = $"{ModelFolder}/{stem}.fbx";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (asset == null) { Debug.LogWarning($"[CheckoutKit] missing model {fbxPath}"); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = stem;

            // index the instance by node name; Unity keeps the authored names through the FBX round trip
            var byName = new Dictionary<string, Transform>();
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) byName[t.name] = t;

            // materials, per authored slot order
            foreach (var oRaw in MiniJson.GetArr(model, "objects"))
            {
                var o = MiniJson.Obj(oRaw);
                string name = MiniJson.GetStr(o, "name");
                var slots = MiniJson.GetArr(o, "materials");
                if (slots == null || slots.Count == 0 || !byName.TryGetValue(name, out var t)) continue;
                var r = t.GetComponent<MeshRenderer>();
                if (r == null) continue;
                var assigned = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < assigned.Length; i++)
                {
                    string mn = i < slots.Count ? MiniJson.Str(slots[i]) : MiniJson.Str(slots[slots.Count - 1]);
                    assigned[i] = mn != null && materials.TryGetValue(mn, out var mm) ? mm : null;
                }
                r.sharedMaterials = assigned;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
            }

            // collision proxies: a box collider on the root that matches the authored volume, then the proxy goes dark
            foreach (var t in go.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("COL_")).ToList())
            {
                var mf = t.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    var b = mf.sharedMesh.bounds;
                    var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                    var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                    for (int i = 0; i < 8; i++)
                    {
                        var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                        var world = go.transform.InverseTransformPoint(t.TransformPoint(corner));
                        min = Vector3.Min(min, world); max = Vector3.Max(max, world);
                    }
                    var bc = go.AddComponent<BoxCollider>();
                    bc.center = (min + max) * 0.5f;
                    bc.size = max - min;
                    bc.enabled = false;   // the kit builder in the scene turns on only what should block the player
                }
                t.gameObject.SetActive(false);
            }

            // the rig: every anchor, socket, screen, key and well as a serialized reference
            var rig = go.AddComponent<CheckoutRig>();
            rig.Stem = stem;
            var rootExtras = MiniJson.GetObj(MiniJson.GetObj(MiniJson.GetObj(model, "gltfNodes"), stem), "extras") ?? MiniJson.GetObj(model, "rootExtras");
            var dims = MiniJson.GetArr(rootExtras, "target_dimensions_m");
            if (dims != null && dims.Count >= 3) rig.TargetDimensions = new Vector3(MiniJson.Num(dims[0]), MiniJson.Num(dims[1]), MiniJson.Num(dims[2]));

            // the glTF node extras are the typed authority (Blender's own id-property arrays stringify on export)
            var gltfNodes = MiniJson.GetObj(model, "gltfNodes");
            foreach (var oRaw in MiniJson.GetArr(model, "objects"))
            {
                var o = MiniJson.Obj(oRaw);
                string name = MiniJson.GetStr(o, "name");
                if (!byName.TryGetValue(name, out var t)) continue;
                var extras = MiniJson.GetObj(MiniJson.GetObj(gltfNodes, name), "extras") ?? MiniJson.GetObj(o, "extras");
                if (IsRigNode(name)) rig.Refs.Add(new NamedRef { Name = name, Target = t });

                if (extras == null) continue;
                string socket = MiniJson.GetStr(extras, "socket");
                string denom = MiniJson.GetStr(extras, "denomination");
                if ((socket == "bill" || socket == "coin") && denom != null && name.EndsWith("_SOCKET"))
                {
                    string clipName = MiniJson.GetStr(extras, "clip");
                    rig.Wells.Add(new DrawerWellContract
                    {
                        Denomination = denom,
                        Coin = socket == "coin",
                        Socket = t,
                        Clip = clipName != null && byName.TryGetValue(clipName, out var c) ? c : null,
                        WellW = MiniJson.GetNum(extras, "well_w"),
                        WellD = MiniJson.GetNum(extras, "well_d"),
                        WallH = MiniJson.GetNum(extras, "wall_h"),
                        Spacing = MiniJson.GetNum(extras, "spacing_m"),
                        HingeDrop = MiniJson.GetNum(extras, "hinge_drop_m"),
                        PileH = MiniJson.GetNum(extras, "pile_h_m"),
                        MaxPieces = MiniJson.GetInt(extras, "max_pieces"),
                    });
                }
                if (socket == "bag_contents")
                    rig.BagInteriorHalf = new Vector3(MiniJson.GetNum(extras, "interior_half_x"), MiniJson.GetNum(extras, "interior_half_mouth"), MiniJson.GetNum(extras, "interior_half_depth"));
                if (MiniJson.Get(extras, "dynamic_screen") != null)
                {
                    rig.Screen = t.GetComponent<Renderer>();
                    var px = MiniJson.GetArr(extras, "screen_px");
                    if (px != null && px.Count >= 2) rig.ScreenPixels = new Vector2Int(MiniJson.Int(px[0]), MiniJson.Int(px[1]));
                }
                if (MiniJson.GetStr(extras, "movable") == "drawer")
                {
                    rig.Tray = t;
                    rig.TrayTravel = MiniJson.GetNum(extras, "open_travel_m");
                }
            }
            rig.Wells = rig.Wells.OrderBy(w => w.Coin).ThenBy(w => w.Denomination.Length).ThenBy(w => w.Denomination).ToList();

            string prefabPath = $"{PrefabFolder}/{stem}.prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            return saved;
        }
    }
}
