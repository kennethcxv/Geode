using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using GeodeEmpire.Core;
using GeodeEmpire.Workshop;

namespace GeodeEmpire.UI
{
    /// <summary>
    /// Points at the thing the current tutorial step is about (V6 §57: "highlight relevant world object").
    /// A ring sits over the object while it is on screen; when it is behind the player the ring becomes a chevron
    /// pinned to the edge of the screen, pointing the way to turn. Both carry the distance, because "the tablet"
    /// means nothing until you know it is four metres behind you.
    ///
    /// The marker is drawn on the HUD panel rather than in the world: no new shader, no new material, nothing to
    /// occlude, and it reads the same in a dark corner as under a lamp.
    /// </summary>
    public sealed class TutorialBeacon : MonoBehaviour
    {
        private VisualElement _ring, _chevron;
        private Label _label;
        private Transform _target;
        private string _targetKey = "";
        private float _rescan, _lift = 0.12f;

        private void Start()
        {
            var hud = HudController.Instance;
            if (hud == null) { enabled = false; return; }
            var root = hud.GetComponent<UIDocument>().rootVisualElement;
            _ring = UiKit.Box(root, "beacon");
            _ring.pickingMode = PickingMode.Ignore;
            UiKit.Box(_ring, "beacon-dot");
            _chevron = UiKit.Box(_ring, "beacon-chevron");
            _label = UiKit.Label(_ring, "", "beacon-label");
            _ring.style.display = DisplayStyle.None;
        }

        /// <summary>The live placement zone of a kind, or null. Zones move with the fixture that owns them.</summary>
        private static Transform Zone(Interaction.ZoneKind kind)
        {
            foreach (var z in Object.FindObjectsByType<Interaction.PlacementZone>(FindObjectsSortMode.None))
                if (z.Kind == kind && z.gameObject.activeInHierarchy)
                    return z.Anchor != null ? z.Anchor : z.transform;
            return null;
        }

        /// <summary>The rock the player is being told to pick up: the topmost one in an open crate.</summary>
        private static Transform TopRockInCrate()
        {
            Transform best = null; float bestY = float.MinValue;
            foreach (var e in Object.FindObjectsByType<Specimens.SpecimenEntity>(FindObjectsSortMode.None))
            {
                if (e.Record == null || e.Record.Condition.Opened || e.Zone != null) continue;
                if (e.Record.Location != Save.SpecimenLocation.InCrate) continue;
                if (e.transform.position.y > bestY) { bestY = e.transform.position.y; best = e.transform; }
            }
            return best;
        }

        /// <summary>
        /// Which object each step id points at (§13.2). Semantic, and resolved live every few frames, so a step
        /// points at the thing the player has to touch rather than at the station's root transform — the cradle
        /// rather than the bench, the chisel rather than the bench, the rock rather than the crate it is in.
        /// Because everything here comes from the running scene, it survives the player moving a machine, a
        /// save/load, and the room growing (§13.3).
        /// </summary>
        public static Transform Resolve(string key)
        {
            switch (key)
            {
                // ---- exact affordances, not station roots ----------------------------------------
                case "rock":   return TopRockInCrate() ?? Resolve("crate");
                case "cradle": return Zone(Interaction.ZoneKind.Cradle) ?? Find<Cracking.CrackingBench>();
                case "chisel":
                {
                    var b = Object.FindAnyObjectByType<Cracking.CrackingBench>();
                    if (b == null) return null;
                    // once a rock is on the cradle the seam is what the step is about, not the tool on the rack
                    var onCradle = Zone(Interaction.ZoneKind.Cradle);
                    if (b.Active && onCradle != null) return onCradle;
                    return b.ChiselVisual != null ? b.ChiselVisual : b.transform;
                }
                case "basin":
                {
                    var w = Object.FindAnyObjectByType<WashStation>();
                    if (w == null) return null;
                    return w.WaterSurface != null ? w.WaterSurface : w.transform;
                }
                case "brush":
                {
                    var w = Object.FindAnyObjectByType<WashStation>();
                    if (w == null) return null;
                    return w.Brush != null ? w.Brush : w.WaterSurface != null ? w.WaterSurface : w.transform;
                }
                case "pan":    return Zone(Interaction.ZoneKind.Scale) ?? Find<AppraisalStation>();
                case "outbox_tray": return Zone(Interaction.ZoneKind.SellTray) ?? Find<SellOutbox>();
                case "register":
                {
                    var c = Object.FindAnyObjectByType<Checkout.CheckoutStation>();
                    if (c == null) return null;
                    return c.ScannedPoint != null ? c.ScannedPoint : c.Counter != null ? c.Counter : c.transform;
                }
                case "vise":
                {
                    var s = Object.FindAnyObjectByType<Lapidary.SawStation>();
                    if (s == null) return null;
                    return s.Vise != null ? s.Vise : s.transform;
                }
                case "platen":
                {
                    var l = Object.FindAnyObjectByType<Lapidary.PolishStation>();
                    if (l == null) return null;
                    return l.Platen != null ? l.Platen : l.transform;
                }

                case "tablet":
                {
                    // the workshop's own tablet, not the office laptop: the laptop opens the same screen but it is a
                    // room away, and a first-run player sent to the back of house on step two learns the wrong room
                    Transform fallback = null;
                    foreach (var t in Object.FindObjectsByType<OrderTablet>(FindObjectsSortMode.None))
                    {
                        if (t.Prompt == "Use tablet") return t.transform;
                        fallback = t.transform;
                    }
                    return fallback;
                }
                case "crate":
                {
                    Transform best = null;
                    foreach (var c in Object.FindObjectsByType<CrateEntity>(FindObjectsSortMode.None)) best = c.transform;
                    return best;
                }
                case "washtub": return Find<WashStation>();
                case "bench": return Find<Cracking.CrackingBench>();
                case "scale": return Find<AppraisalStation>();
                case "cabinet": return Find<DisplayCabinet>();
                case "intercom": return Find<DealerIntercom>();
                case "shelf":
                {
                    var shop = Retail.RetailShop.Instance;
                    if (shop == null) return null;
                    foreach (var z in shop.SaleSlots) if (z != null && z.gameObject.activeInHierarchy && !z.Locked && z.IsEmpty) return z.transform;
                    return shop.SaleSlots.Count > 0 ? shop.SaleSlots[0].transform : null;
                }
                case "counter": return Find<Checkout.CheckoutStation>();
                case "saw": return Find<Lapidary.SawStation>();
                case "lap": return Find<Lapidary.PolishStation>();
                case "delivery":
                {
                    var d = Object.FindAnyObjectByType<Build.FixtureDelivery>();
                    if (d == null) return null;
                    foreach (var slot in d.Slots) if (slot.Root != null && slot.Root.activeSelf) return slot.Root.transform;
                    // no crate waiting: point at nothing rather than at the delivery component's own transform,
                    // which sits at the world origin and would put the beacon on the floor in the middle of the room
                    return null;
                }
                case "outbox": return Find<SellOutbox>();
                default: return null;
            }
        }

        private static Transform Find<T>() where T : Component
        {
            var c = Object.FindAnyObjectByType<T>();
            return c != null ? c.transform : null;
        }

        /// <summary>How far above a target's own origin the ring should sit, from what the target actually is.</summary>
        private static float LiftFor(Transform target)
        {
            var rends = target.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return 0.12f;                       // an empty anchor: a hand's width above it
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            float above = (b.max.y - target.position.y) + 0.05f;
            return Mathf.Clamp(above, 0.06f, 0.35f);
        }

        private void LateUpdate()
        {
            if (_ring == null) return;
            var hud = HudController.Instance;
            var step = Tutorial.Current;
            var cam = Camera.main;
            if (step == null || cam == null || hud == null || !hud.FreeRoam || CursorController.InMenu || string.IsNullOrEmpty(step.Target))
            {
                _ring.style.display = DisplayStyle.None;
                return;
            }
            // objects come and go (a crate is delivered, a machine is sited): re-resolve a few times a second
            _rescan -= Time.unscaledDeltaTime;
            if (_target == null || _targetKey != step.Target || _rescan <= 0f)
            {
                _targetKey = step.Target;
                _target = Resolve(step.Target);
                // GetComponentsInChildren allocates, so measure the target when it changes, not every frame
                _lift = _target != null ? LiftFor(_target) : 0.12f;
                _rescan = 0.4f;
            }
            if (_target == null) { _ring.style.display = DisplayStyle.None; return; }

            // sit just above the thing, not a fixed third of a metre above it: 0.35 m is right for a station on
            // the floor and wrong for a rock in a crate, where it floats clear of what it is pointing at (§13.1)
            var world = _target.position + Vector3.up * _lift;
            var panel = _ring.panel;
            float dist = Vector3.Distance(cam.transform.position, world);
            var vp = cam.WorldToViewportPoint(world);
            bool behind = vp.z <= 0f;
            // a point behind the camera projects mirrored: flip it so the chevron points the shorter way round
            if (behind) { vp.x = 1f - vp.x; vp.y = 1f - vp.y; }
            bool offScreen = behind || vp.x < 0.04f || vp.x > 0.96f || vp.y < 0.06f || vp.y > 0.94f;

            Vector2 p;
            if (offScreen)
            {
                var dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
                if (dir.sqrMagnitude < 1e-6f) dir = new Vector2(0f, -1f);
                dir.Normalize();
                // push out to an inset ellipse so the chevron never sits under the crosshair or off the edge
                p = new Vector2(0.5f + dir.x * 0.40f, 0.5f + dir.y * 0.36f);
                _chevron.style.display = DisplayStyle.Flex;
                _chevron.style.rotate = new Rotate(new Angle(Mathf.Atan2(-dir.y, dir.x) * Mathf.Rad2Deg + 90f, AngleUnit.Degree));
            }
            else
            {
                p = new Vector2(vp.x, vp.y);
                _chevron.style.display = DisplayStyle.None;
            }
            var screen = RuntimePanelUtils.CameraTransformWorldToPanel(panel, world, cam);
            float w = _ring.panel.visualTree.layout.width, h = _ring.panel.visualTree.layout.height;
            float px = offScreen ? p.x * w : screen.x;
            float py = offScreen ? (1f - p.y) * h : screen.y;
            _ring.style.left = px;
            _ring.style.top = py;
            _ring.style.display = DisplayStyle.Flex;
            _ring.EnableInClassList("beacon-off", offScreen);
            _label.text = dist < 1.6f ? "here" : $"{dist:0.#} m";
            // a slow pulse so it reads as a marker rather than a lens flare
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 3.1f);
            _ring.style.opacity = 0.55f + 0.35f * pulse;
        }
    }
}
