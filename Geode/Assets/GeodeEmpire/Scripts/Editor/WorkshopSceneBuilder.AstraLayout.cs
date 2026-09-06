using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using GeodeEmpire.Build;

namespace GeodeEmpire.EditorTools
{
    public static partial class WorkshopSceneBuilder
    {
        public const string AstraStudyPath = "Assets/GeodeEmpire/Scenes/AstraLayoutStudy.unity";

        /// <summary>
        /// A reversible, measured architecture study in the existing builder. It contains no career/gameplay
        /// systems and is never added to build settings. Production apply and migration are a separate gate.
        /// </summary>
        [MenuItem("GeodeEmpire/Astra/Create Measured Layout Study")]
        public static void CreateAstraLayoutStudy()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play before the layout study.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("A loaded scene has unsaved changes; study creation stopped.");
            var errors = AstraLayout.Audit();
            if (errors.Count != 0) throw new InvalidOperationException("Layout envelopes fail: " + string.Join("; ", errors));

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("AstraLayoutStudy_ONLY");
            Undo.RegisterCreatedObjectUndo(root, "Create Astra architecture study");
            var plaster = StudyMaterial("Study_Plaster", new Color(0.76f, 0.74f, 0.68f));
            var floor = StudyMaterial("Study_Concrete", new Color(0.48f, 0.49f, 0.47f));
            var steel = StudyMaterial("Study_BlueSteel", new Color(0.15f, 0.28f, 0.34f));
            var cream = StudyMaterial("Study_Cream", new Color(0.78f, 0.76f, 0.68f));
            var cardboard = StudyMaterial("Study_Receiving", new Color(0.46f, 0.32f, 0.18f));
            var work = StudyMaterial("Study_Operator", new Color(0.27f, 0.53f, 0.42f));
            var publicRoute = StudyMaterial("Study_PublicRoute", new Color(0.22f, 0.49f, 0.68f));

            foreach (var zone in new[] { "Starter", "Processing", "Showroom", "Office" })
            {
                var rect = AstraLayout.Bounds(zone);
                StudyBox(root.transform, zone + "_Floor", new Vector3(rect.center.x, -0.06f, rect.center.y),
                    new Vector3(rect.width, 0.12f, rect.height), floor);
            }
            // New storefronts: left starter door/window and mature street entrance; no sealed brick porch.
            StudyWallX(root.transform, "Front", -6.4f, 7f, -2.77f,
                new[] { (-6.175f, -5.025f, 2.2f), (-4.70f, -0.75f, 2.35f), (5.025f, 6.175f, 2.2f) }, plaster);
            StudyBox(root.transform, "StarterWindowSill", new Vector3(-2.725f, 0.42f, -2.77f), new Vector3(3.95f, 0.84f, 0.14f), cream);
            StudyWallX(root.transform, "North", -6.4f, 7f, 6.07f, new[] { (-5.4f, -2.6f, 2.5f) }, plaster);
            StudyWallX(root.transform, "ProcessingPartition", -6.4f, 1.4f, 1.30f,
                new[] { (-3.9f, -2.7f, 2.2f), (-0.1f, 1.1f, 2.2f) }, plaster);
            StudyBox(root.transform, "WestWall", new Vector3(-6.47f, 1.45f, 1.65f), new Vector3(0.14f, 2.9f, 8.7f), plaster);
            StudyBox(root.transform, "EastWall", new Vector3(7.07f, 1.45f, 1.65f), new Vector3(0.14f, 2.9f, 8.7f), plaster);
            StudyBox(root.transform, "StarterEastPartition", new Vector3(-0.40f, 1.45f, -0.70f), new Vector3(0.14f, 2.9f, 4f), plaster);
            // Staff connection to the showroom at z 2.0…3.2.
            StudyBox(root.transform, "ShowroomPartitionS", new Vector3(1.40f, 1.45f, -0.35f), new Vector3(0.14f, 2.9f, 4.7f), plaster);
            StudyBox(root.transform, "ShowroomPartitionN", new Vector3(1.40f, 1.45f, 4.60f), new Vector3(0.14f, 2.9f, 2.8f), plaster);
            StudyBox(root.transform, "ShowroomDoorHeader", new Vector3(1.40f, 2.55f, 2.60f), new Vector3(0.14f, 0.7f, 1.2f), plaster);
            StudyBox(root.transform, "StreetPavement", new Vector3(0.30f, -0.08f, -3.75f), new Vector3(15f, 0.12f, 2.0f), floor);

            foreach (var space in AstraLayout.Spaces)
            {
                var colour = space.Id.Contains("checkout") || space.Id.Contains("display") ? cream
                    : space.Id.Contains("receiving") || space.Id.StartsWith("bay_") ? cardboard : steel;
                StudyBox(root.transform, space.Id, new Vector3(space.Centre.x, space.Height * 0.5f, space.Centre.y),
                    new Vector3(space.Size.x, space.Height, space.Size.y), colour);
                if (space.HasWork)
                    StudyBox(root.transform, space.Id + "_Operator", new Vector3(space.Operator.x, 0.008f, space.Operator.y),
                        new Vector3(space.OperatorSize.x, 0.008f, space.OperatorSize.y), work, false);
            }
            foreach (var point in AstraLayout.StarterQueue.Append(AstraLayout.StarterCustomer))
                StudyBox(root.transform, "CustomerPosition", new Vector3(point.x, 0.012f, point.y), new Vector3(0.56f, 0.008f, 0.56f), publicRoute, false);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.65f, 0.68f);
            var light = new GameObject("StudyDaylight").AddComponent<Light>();
            light.transform.SetParent(root.transform, false); light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(55f, -30f, 0f); light.intensity = 1.1f; light.shadows = LightShadows.Soft;
            var cam = new GameObject("StudyCamera").AddComponent<Camera>();
            cam.transform.SetParent(root.transform, false); cam.tag = "MainCamera";
            cam.transform.position = new Vector3(0.3f, 17f, 1.1f); cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            cam.orthographic = true; cam.orthographicSize = 7.1f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 40f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.18f, 0.20f, 0.22f);
            Physics.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, AstraStudyPath)) throw new InvalidOperationException("Could not save the layout study.");
        }

        private static Material StudyMaterial(string name, Color colour)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", colour); material.SetFloat("_Smoothness", 0.15f);
            return material;
        }

        private static GameObject StudyBox(Transform parent, string name, Vector3 centre, Vector3 size, Material material, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(go, "Create study envelope");
            go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = centre; go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) Undo.DestroyObjectImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void StudyWallX(Transform parent, string name, float start, float end, float z,
            (float a, float b, float h)[] holes, Material material)
        {
            float x = start; int part = 0;
            foreach (var hole in holes.OrderBy(h => h.a))
            {
                if (hole.a > x) StudyBox(parent, name + part++, new Vector3((x + hole.a) * 0.5f, 1.45f, z), new Vector3(hole.a - x, 2.9f, 0.14f), material);
                StudyBox(parent, name + "Header" + part++, new Vector3((hole.a + hole.b) * 0.5f, (hole.h + 2.9f) * 0.5f, z),
                    new Vector3(hole.b - hole.a, 2.9f - hole.h, 0.14f), material);
                x = hole.b;
            }
            if (x < end) StudyBox(parent, name + part, new Vector3((x + end) * 0.5f, 1.45f, z), new Vector3(end - x, 2.9f, 0.14f), material);
        }
    }
}
