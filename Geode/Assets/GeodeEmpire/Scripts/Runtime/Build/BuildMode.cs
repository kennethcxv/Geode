using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Core;
using GeodeEmpire.Player;
using GeodeEmpire.Save;
using GeodeEmpire.UI;

namespace GeodeEmpire.Build
{
    /// <summary>
    /// Shop layout editing, in first person. The player keeps walking and looking; the fixture being placed follows
    /// the view on the floor, snapped to a quarter metre and fifteen degrees, wrapped in a footprint volume that is
    /// green while <see cref="PlacementValidator"/> accepts the pose and red when it does not, with the reason
    /// written under it.
    ///
    /// The preview is the real fixture, moved. Nothing is cloned and nothing is approximated, so a placement that
    /// looks valid cannot turn out otherwise once it is confirmed.
    /// </summary>
    public sealed class BuildMode : MonoBehaviour
    {
        public static BuildMode Instance { get; private set; }

        public Material GhostOk, GhostBad;

        public bool Active { get; private set; }
        public PlaceableFixture Holding { get; private set; }
        public bool CurrentValid { get; private set; }
        public string CurrentReason { get; private set; } = "";
        /// <summary>Everything the player could place or move right now, in catalogue order.</summary>
        public readonly List<PlaceableFixture> Available = new List<PlaceableFixture>();
        public int Index { get; private set; }
        public System.Action Changed;

        private GameObject _ghost;
        private MeshRenderer _ghostR, _padR;
        private Vector3 _pos;
        private float _yaw;
        private Vector3 _restorePos;
        private float _restoreYaw;
        private bool _wasPlaced;
        private float _lastRoute;
        private bool _routeOk = true;
        private string _routeReason = "";

        private void Awake()
        {
            Instance = this;
            BuildGhost();
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void BuildGhost()
        {
            _ghost = new GameObject("PlacementGhost");
            _ghost.transform.SetParent(transform, false);
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Volume"; box.transform.SetParent(_ghost.transform, false);
            Destroy(box.GetComponent<Collider>());
            _ghostR = box.GetComponent<MeshRenderer>();
            _ghostR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "Footprint"; pad.transform.SetParent(_ghost.transform, false);
            Destroy(pad.GetComponent<Collider>());
            _padR = pad.GetComponent<MeshRenderer>();
            _padR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ghost.SetActive(false);
        }

        // -----------------------------------------------------------------------------------------
        public void Toggle() { if (Active) Exit(); else Enter(); }

        public void Enter()
        {
            if (Active) return;
            RefreshAvailable();
            if (Available.Count == 0)
            {
                GameSession.Instance?.Notify("Nothing to place yet — buy equipment or fittings on the tablet.", NotificationKind.Info);
                return;
            }
            Active = true;
            // build mode draws its own chrome: the goal card, the control rail, the crosshair and the tutorial step
            // belong to walking round the shop
            HudController.Instance?.SetFreeRoamVisible(false);
            Index = Mathf.Clamp(Index, 0, Available.Count - 1);
            Pick(Index);
            Changed?.Invoke();
        }

        public void Exit()
        {
            if (!Active) return;
            Release(restore: true);
            Active = false;
            _ghost.SetActive(false);
            HudController.Instance?.SetFreeRoamVisible(true);
            Changed?.Invoke();
        }

        /// <summary>Enter build mode with one particular fixture in hand (a delivery crate being opened).</summary>
        public void EnterHolding(PlaceableFixture f)
        {
            if (f == null) return;
            if (!Active) Enter();
            if (!Active) return;
            int i = Available.IndexOf(f);
            if (i >= 0) Select(i);
        }

        public void RefreshAvailable()
        {
            Available.Clear();
            var st = GameSession.Instance != null ? GameSession.Instance.State : null;
            foreach (var f in PlaceableFixture.All)
            {
                if (f == null || !f.Movable || !f.Owned) continue;
                Available.Add(f);
            }
            Available.Sort((a, b) =>
            {
                var ap = a.Pose; var bp = b.Pose;
                if (ap.Placed != bp.Placed) return ap.Placed ? 1 : -1;   // things still to site come first
                int c = string.CompareOrdinal(a.Category, b.Category);
                return c != 0 ? c : string.CompareOrdinal(a.DisplayName, b.DisplayName);
            });
            if (Index >= Available.Count) Index = Mathf.Max(0, Available.Count - 1);
        }

        /// <summary>Pick a specific entry (the catalogue strip clicking through).</summary>
        public void Select(int i)
        {
            if (!Active || i < 0 || i >= Available.Count) return;
            Release(restore: true);
            Index = i;
            Pick(Index);
            Changed?.Invoke();
        }

        public void Cycle(int delta)
        {
            if (!Active || Available.Count == 0) return;
            Release(restore: true);
            Index = (Index + delta + Available.Count) % Available.Count;
            Pick(Index);
            Changed?.Invoke();
        }

        private void Pick(int i)
        {
            if (i < 0 || i >= Available.Count) { Holding = null; return; }
            Holding = Available[i];
            var pose = Holding.Pose;
            _restorePos = Holding.transform.position;
            _restoreYaw = Holding.transform.eulerAngles.y;
            _wasPlaced = pose.Placed;
            _pos = _restorePos; _yaw = _restoreYaw;
            _ghost.SetActive(true);
            _lastRoute = -99f;
        }

        private void Release(bool restore)
        {
            if (Holding == null) return;
            if (restore) Holding.transform.SetPositionAndRotation(_restorePos, Quaternion.Euler(0f, _restoreYaw, 0f));
            Holding = null;
        }

        // -----------------------------------------------------------------------------------------
        private void Update()
        {
            // B toggles the mode from free roam; Esc leaves it
            bool freeRoam = HudController.Instance == null || HudController.Instance.FreeRoam || Active;
            if (!CursorController.InMenu && freeRoam && GameInput.BuildPressed) { Toggle(); return; }
            if (Active && GameInput.BackPressed && !CursorController.InputConsumedThisFrame) { CursorController.MarkInputConsumed(); Exit(); return; }
            if (!Active) return;
            if (Holding == null) { Exit(); return; }
            var cam = Camera.main;
            if (cam == null) return;

            // where the player is looking on the floor, an arm's length to four metres out
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            var plane = new Plane(Vector3.up, Vector3.zero);
            Vector3 target;
            if (plane.Raycast(ray, out float t) && t > 0.1f)
                target = ray.GetPoint(Mathf.Clamp(t, 1.1f, 4.5f));
            else
                target = cam.transform.position + Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized * 2.2f;
            target.y = 0f;
            _pos = new Vector3(Mathf.Round(target.x / 0.25f) * 0.25f, 0f, Mathf.Round(target.z / 0.25f) * 0.25f);

            float rot = GameInput.Rotate;
            if (Mathf.Abs(rot) > 0.5f && Time.time - _lastRotate > 0.16f) { _yaw = Mathf.Repeat(_yaw + Mathf.Sign(rot) * 15f, 360f); _lastRotate = Time.time; }
            var scroll = GameInput.Scroll;
            if (Mathf.Abs(scroll.y) > 0.5f && Time.time - _lastCycle > 0.18f) { _lastCycle = Time.time; Cycle(scroll.y > 0f ? 1 : -1); return; }

            Holding.transform.SetPositionAndRotation(_pos, Quaternion.Euler(0f, _yaw, 0f));

            // the cheap rules every frame; the route flood fill five times a second
            var quick = PlacementValidator.Check(Holding, _pos, _yaw, routeCheck: false);
            if (quick.Valid && Time.time - _lastRoute > 0.2f)
            {
                _lastRoute = Time.time;
                var full = PlacementValidator.Check(Holding, _pos, _yaw, routeCheck: true);
                _routeOk = full.Valid; _routeReason = full.Reason;
            }
            else if (!quick.Valid) { _routeOk = true; _routeReason = ""; }
            CurrentValid = quick.Valid && _routeOk;
            CurrentReason = quick.Valid ? _routeReason : quick.Reason;
            DrawGhost();

            if (GameInput.InteractPressed && CurrentValid) Confirm();
            else if (GameInput.InteractPressed) GameSession.Instance?.Notify(CurrentReason, NotificationKind.Warning);
        }

        private float _lastRotate, _lastCycle;

        private void DrawGhost()
        {
            Holding.BodyBox(_pos, _yaw, out var centre, out var half, out var rot);
            var vol = _ghost.transform.GetChild(0);
            vol.SetPositionAndRotation(centre, rot);
            vol.localScale = half * 2f;
            var pad = _ghost.transform.GetChild(1);
            pad.SetPositionAndRotation(new Vector3(centre.x, 0.006f, centre.z), rot);
            float grow = Holding.Clearance > 0.01f ? Holding.Clearance : 0.12f;
            pad.localScale = new Vector3(half.x * 2f + grow, 0.008f, half.z * 2f + 0.24f);
            var m = CurrentValid ? GhostOk : GhostBad;
            if (m != null) { _ghostR.sharedMaterial = m; _padR.sharedMaterial = m; }
        }

        // -----------------------------------------------------------------------------------------
        /// <summary>
        /// Commit a fixture to a pose. Validates first and refuses if the pose is not legal, so nothing outside build
        /// mode (a delivery flow, a test, a "site it for me" affordance) can write an impossible layout to the save.
        /// </summary>
        public bool TryPlace(PlaceableFixture f, Vector3 pos, float yaw, out string reason)
        {
            reason = "";
            if (f == null) { reason = "nothing to place"; return false; }
            var prevPos = f.transform.position; var prevRot = f.transform.rotation;
            f.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var r = PlacementValidator.Check(f, pos, yaw, routeCheck: true);
            if (!r.Valid)
            {
                f.transform.SetPositionAndRotation(prevPos, prevRot);
                reason = r.Reason;
                return false;
            }
            var session = GameSession.Instance;
            if (session?.State != null)
            {
                session.State.SetFixture(f.Id, pos, yaw, true);
                session.RaiseStateChanged();
            }
            if (f.Body != null && !f.Body.activeSelf) f.Body.SetActive(true);
            PlacementValidator.InvalidateMask();
            Workshop.Tutorial.Notify("fixture_placed");
            return true;
        }

        public void Confirm()
        {
            if (Holding == null || !CurrentValid) return;
            if (!TryPlace(Holding, _pos, _yaw, out string why))
            {
                GameSession.Instance?.Notify(why, NotificationKind.Warning);
                return;
            }
            GameSession.Instance?.Notify(Holding.DisplayName + " placed.", NotificationKind.Success);
            var done = Holding;
            Release(restore: false);
            RefreshAvailable();
            // move on to the next thing still waiting to be sited, else stay on what was just placed
            int next = Available.FindIndex(f => !f.Pose.Placed);
            Index = next >= 0 ? next : Mathf.Max(0, Available.IndexOf(done));
            Pick(Index);
            Changed?.Invoke();
        }
    }
}
