using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using GeodeEmpire.UI;

namespace GeodeEmpire.EditorTools
{
    public static class TitleSceneBuilder
    {
        public const string ScenePath = "Assets/GeodeEmpire/Scenes/Title.unity";

        [MenuItem("GeodeEmpire/Build Title Scene")]
        public static void Build()
        {
            var panel = WorkshopSceneBuilder.EnsurePanelSettings();
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(WorkshopSceneBuilder.VolumeProfilePath);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.08f, 0.08f, 0.1f);
            RenderSettings.skybox = null;

            var camGo = new GameObject("Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.03f, 0.035f);
            cam.fieldOfView = 34f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 20f;
            camGo.transform.position = new Vector3(0.36f, 0.22f, -0.62f);
            camGo.transform.LookAt(new Vector3(0.12f, 0.02f, 0f));
            camGo.AddComponent<AudioListener>();
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

            // backdrop table
            var table = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            table.name = "Turntable";
            table.transform.position = new Vector3(0.12f, -0.03f, 0f);
            table.transform.localScale = new Vector3(0.9f, 0.03f, 0.9f);
            table.GetComponent<MeshRenderer>().sharedMaterial = WorkshopMaterials.Get("M_WoodDark");
            var back = GameObject.CreatePrimitive(PrimitiveType.Quad);
            back.name = "Backdrop";
            back.transform.position = new Vector3(0f, 0.6f, 1.6f);
            back.transform.localScale = new Vector3(8f, 4f, 1f);
            back.GetComponent<MeshRenderer>().sharedMaterial = WorkshopMaterials.Get("M_Plaster");

            var hero = new GameObject("Hero");
            hero.transform.position = new Vector3(0.12f, 0f, 0f);
            hero.AddComponent<TitleHero>();

            Light(new Vector3(-0.4f, 0.9f, -0.5f), new Vector3(58f, 35f, 0f), new Color(1f, 0.94f, 0.85f), 3.2f, 4f, 55f, true);
            Light(new Vector3(0.9f, 0.5f, -0.3f), new Vector3(30f, -70f, 0f), new Color(0.75f, 0.85f, 1f), 1.4f, 4f, 70f, false);
            Light(new Vector3(0.2f, 0.7f, 0.9f), new Vector3(120f, 0f, 0f), new Color(1f, 1f, 1f), 1.6f, 3f, 60f, false);

            var vol = new GameObject("GlobalVolume");
            var v = vol.AddComponent<Volume>();
            v.isGlobal = true;
            v.sharedProfile = profile;

            var ui = new GameObject("TitleUI");
            var doc = ui.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            ui.AddComponent<TitleScreen>();
            var pm = ui.AddComponent<PauseMenu>();
            pm.ShowSettingsOnly = true;

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[TitleSceneBuilder] built " + ScenePath);
        }

        private static Light Light(Vector3 pos, Vector3 euler, Color color, float intensity, float range, float spot, bool shadows)
        {
            var go = new GameObject("Light");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(euler);
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = color; l.intensity = intensity; l.range = range; l.spotAngle = spot; l.innerSpotAngle = spot * 0.5f;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return l;
        }
    }
}
