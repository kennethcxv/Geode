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
        private float _rescan;

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

        /// <summary>Which object each step id points at. One place, so a renamed prop breaks loudly rather than silently.</summary>
        private static Transform Resolve(string key)
        {
            switch (key)
            {
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
                    return d.transform;
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
                _rescan = 0.4f;
            }
            if (_target == null) { _ring.style.display = DisplayStyle.None; return; }

            var world = _target.position + Vector3.up * 0.35f;
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
