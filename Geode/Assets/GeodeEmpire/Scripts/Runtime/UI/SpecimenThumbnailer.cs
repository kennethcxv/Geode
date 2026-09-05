using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using GeodeEmpire.Core;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.UI
{
    /// <summary>
    /// Renders the real specimen into a small texture so a list row, a tile or a card can show the piece
    /// itself instead of a coloured chip. The rig sits four hundred metres under the floor with a camera whose
    /// far plane is a couple of metres, so nothing else in the world can wander into frame and no layer has to
    /// be reserved. One render per key, cached for the session.
    /// </summary>
    public sealed class SpecimenThumbnailer : MonoBehaviour
    {
        /// <summary>The plate is a photograph on a dark ground, the way the pack presents a specimen.</summary>
        public static readonly Color Ground = new Color(0.16f, 0.15f, 0.175f, 1f);
        private const float StageY = -400f;

        private static SpecimenThumbnailer _instance;
        private readonly Dictionary<string, RenderTexture> _cache = new Dictionary<string, RenderTexture>();
        private const int PlateW = 320, PlateH = 148;
        private readonly Dictionary<int, ulong> _familySeeds = new Dictionary<int, ulong>();
        private Camera _cam;
        private Transform _stage;
        // building a specimen mesh costs a few milliseconds, so a page of tiles renders one plate per frame
        // rather than stalling the frame the page opens on
        private readonly List<(VisualElement el, string key, ulong seed, bool opened, Color fallback)> _queue
            = new List<(VisualElement, string, ulong, bool, Color)>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null;

        public static SpecimenThumbnailer Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("SpecimenThumbnailer");
                go.hideFlags = HideFlags.DontSave;
                _instance = go.AddComponent<SpecimenThumbnailer>();
                _instance.BuildRig();
                return _instance;
            }
        }

        private void BuildRig()
        {
            _stage = new GameObject("Stage").transform;
            _stage.SetParent(transform, false);
            _stage.position = new Vector3(0f, StageY, 0f);

            var camGo = new GameObject("ThumbCam");
            camGo.transform.SetParent(transform, false);
            _cam = camGo.AddComponent<Camera>();
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Ground;   // post-processing drops alpha, so the plate carries its own ground
            _cam.fieldOfView = 26f;
            _cam.nearClipPlane = 0.03f;
            _cam.farClipPlane = 3.5f;             // the world proper is 400 m away, so it can never be in frame
            _cam.enabled = false;                  // rendered on demand only
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;      // the same ACES roll-off the game uses, or crystal faces clip to white
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            data.renderShadows = false;

            // a warm key and a cool rim, the lighting the pack uses on its specimen plates
            MakeLight(new Vector3(-0.5f, StageY + 0.6f, -0.55f), new Color(1f, 0.94f, 0.85f), 1.5f);
            MakeLight(new Vector3(0.55f, StageY + 0.15f, 0.5f), new Color(0.6f, 0.74f, 1f), 0.7f);
        }

        private void MakeLight(Vector3 pos, Color colour, float intensity)
        {
            var go = new GameObject("ThumbLight");
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = colour;
            l.intensity = intensity;
            l.range = 2.5f;
            l.shadows = LightShadows.None;
        }

        private bool _warm;

        /// <summary>
        /// Render one throw-away plate now. The first time this camera runs, URP compiles the post-processing
        /// variants it needs, which measured 3.9 seconds — and it used to land on the first rock the player ever
        /// opened. Paying it during load makes it invisible; every plate after the first cost 3-7 ms.
        /// </summary>
        public void Prewarm()
        {
            if (_warm) return;
            _warm = true;
            var lib = GameSession.Instance != null ? GameSession.Instance.Library : null;
            if (lib == null) { _warm = false; return; }
            var rt = Render("warmup", SeedFor(MineralCatalog.All[0].Id), true);
            if (rt != null) { _cache.Remove("warmup"); rt.Release(); Destroy(rt); }
        }

        /// <summary>A deterministic seed whose rock belongs to <paramref name="mineral"/>, or 0 if none is found.</summary>
        public ulong SeedFor(MineralId mineral)
        {
            if (_familySeeds.TryGetValue((int)mineral, out var cached)) return cached;
            ulong seed = 0x9E3779B97F4A7C15UL ^ ((ulong)mineral * 0x51_5A_31_D3UL);
            for (int i = 0; i < 600; i++)
            {
                var g = SpecimenGenerator.Generate(seed);
                if (g.Mineral == mineral) { _familySeeds[(int)mineral] = seed; return seed; }
                seed = seed * 6364136223846793005UL + 1442695040888963407UL;
            }
            _familySeeds[(int)mineral] = 0UL;
            return 0UL;
        }

        /// <summary>Put a mineral family's representative rock behind an element, as soon as it can be rendered.</summary>
        public void Family(VisualElement element, MineralId mineral, Color fallback)
        {
            ulong seed = SeedFor(mineral);
            if (seed == 0UL) { element.style.backgroundColor = fallback; return; }
            Want(element, "fam:" + (int)mineral, seed, true, fallback);
        }

        /// <summary>Put one real specimen from the save behind an element.</summary>
        public void Specimen(VisualElement element, SpecimenRecord r, Color fallback)
        {
            if (r == null) { element.style.backgroundColor = fallback; return; }
            Want(element, "rec:" + r.Id, r.Seed, r.IsOpened, fallback);
        }

        private void Want(VisualElement element, string key, ulong seed, bool opened, Color fallback)
        {
            if (_cache.TryGetValue(key, out var have) && have != null && have.IsCreated()) { Apply(element, have); return; }
            element.style.backgroundColor = fallback;
            _queue.Add((element, key, seed, opened, fallback));
        }

        private void Update()
        {
            if (_queue.Count == 0) return;
            // never photograph anything while the player is watching a rock break (§10.2)
            var session = GameSession.Instance;
            if (session != null && session.PresentationHold > 0) return;
            var job = _queue[0];
            _queue.RemoveAt(0);
            if (job.el == null || job.el.panel == null) return;    // the page closed before we got to it
            RenderTexture tex;
            using (Core.PerfProbe.Measure("thumbnail-render")) tex = Render(job.key, job.seed, job.opened);
            if (tex != null) Apply(job.el, tex);
        }

        private static void Apply(VisualElement element, Texture tex)
        {
            element.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(tex as RenderTexture));
            element.style.backgroundColor = Ground;
        }

        /// <summary>How much interior detail a plate this size can actually resolve.</summary>
        public const int ThumbnailCrystals = 90;

        /// <summary>The rock already standing in the world for this key, if there is one.</summary>
        private SpecimenVisual LiveVisual(string key)
        {
            if (!key.StartsWith("rec:")) return null;
            var session = GameSession.Instance;
            if (session == null) return null;
            var e = session.GetEntity(key.Substring(4));
            return e != null && e.Visual != null && e.Visual.Geometry != null ? e.Visual : null;
        }

        private RenderTexture Render(string key, ulong seed, bool opened)
        {
            if (_cache.TryGetValue(key, out var have) && have != null && have.IsCreated()) return have;
            var lib = GameSession.Instance != null ? GameSession.Instance.Library : null;
            if (lib == null) return null;

            GameObject go;
            // §10.3: if the specimen is standing in the world, borrow its meshes instead of generating a second
            // copy of it. That build allocated megabytes a plate and the collection it provoked was the stall.
            var live = LiveVisual(key);
            if (live != null)
            {
                using (Core.PerfProbe.Measure("  thumb:proxy")) go = live.BuildPlateProxy(_stage, !opened);
            }
            else
            {
                SpecimenGeology geology;
                using (Core.PerfProbe.Measure("  thumb:geology")) geology = SpecimenGenerator.Generate(seed);
                var condition = new SpecimenCondition { Cleaned = 1f, Rinsed = true, Opened = opened };
                go = new GameObject("Thumb");
                go.hideFlags = HideFlags.DontSave;
                go.transform.SetParent(_stage, false);
                var visual = go.AddComponent<SpecimenVisual>();
                // a plate is a couple of hundred pixels: combining every crystal in a cathedral geode for it is
                // work nobody can see, and it was the largest part of the build
                visual.CrystalBudget = ThumbnailCrystals;
                using (Core.PerfProbe.Measure("  thumb:build")) visual.Build(geology, condition, lib);
                using (Core.PerfProbe.Measure("  thumb:crackstate")) visual.SetCrackState(null, null, 0f, opened ? 0.3f : 1f);
                // an opened rock is worth showing for its inside: drop the lid and tip the bowl toward the lens
                if (opened && visual.TopHalf != null) visual.TopHalf.gameObject.SetActive(false);
            }
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0f, 205f, 0f);

            // frame from what actually got built, so a 3 cm nodule and a 20 cm cathedral both fill the plate
            var bounds = new Bounds(_stage.position, Vector3.zero);
            bool any = false;
            void Take(Renderer rend)
            {
                if (rend == null || !rend.enabled || !rend.gameObject.activeInHierarchy) return;
                if (!any) { bounds = rend.bounds; any = true; } else bounds.Encapsulate(rend.bounds);
            }
            foreach (var rend in go.GetComponentsInChildren<Renderer>()) Take(rend);
            float radius = Mathf.Max(0.02f, Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z)));
            float dist = radius / Mathf.Tan(_cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.55f;
            _cam.farClipPlane = dist * 4f;
            var pivot = bounds.center;
            // an opened bowl is looked down into; a closed nodule is looked at from just above the horizon
            var dir = Quaternion.Euler(opened ? 42f : 16f, 24f, 0f) * Vector3.back;
            _cam.transform.position = pivot + dir * dist;
            _cam.transform.rotation = Quaternion.LookRotation(pivot - _cam.transform.position, Vector3.up);

            var rt = new RenderTexture(PlateW, PlateH, 24, RenderTextureFormat.ARGB32) { name = "Thumb_" + key, antiAliasing = 1 };
            rt.Create();
            _cam.targetTexture = rt;
            using (Core.PerfProbe.Measure("  thumb:camera")) _cam.Render();
            _cam.targetTexture = null;
            Destroy(go);
            _cache[key] = rt;
            return rt;
        }

    }
}
