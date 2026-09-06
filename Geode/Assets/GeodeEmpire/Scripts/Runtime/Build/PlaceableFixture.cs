using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Core;
using GeodeEmpire.Economy;
using GeodeEmpire.Save;

namespace GeodeEmpire.Build
{
    /// <summary>
    /// A fixture the player owns and positions: a machine, a display case, a rack, a lamp. The scene authors one of
    /// these on each station root and on each purchasable fixture; build mode moves the root and the save records
    /// where it ended up. Nothing here spawns geometry — the object already exists, hidden until it is owned — so a
    /// machine keeps its wiring, its zones and its camera anchors across a placement.
    /// </summary>
    public sealed class PlaceableFixture : MonoBehaviour
    {
        /// <summary>Save key. Stable across builds; never reuse one for a different fixture.</summary>
        public string Id;
        public string DisplayName = "Fixture";
        /// <summary>Build-mode tab: MACHINES, DISPLAYS, STORAGE, DECOR, LIGHTING.</summary>
        public string Category = "MACHINES";
        [TextArea] public string Description;
        /// <summary>The physical body, local x/z metres, centred on <see cref="BodyOffset"/>.</summary>
        public Vector2 Footprint = new Vector2(1.0f, 0.8f);
        public Vector2 BodyOffset;
        public float Height = 1.4f;
        /// <summary>Working space the operator needs, in metres. 0 for a fixture nobody stands at.</summary>
        public float Clearance;
        /// <summary>Which local direction the operator stands in (unit x/z). The saw is worked from its -Z, the lap from its +X.</summary>
        public Vector2 ClearanceDir = new Vector2(1f, 0f);
        /// <summary>How wide the standing zone is. 0 takes the whole face, which is right for a machine and too much for a long case.</summary>
        public float ClearanceWidth;
        /// <summary>The upgrade that grants it. Empty for fixtures the player starts with.</summary>
        public string RequiresUpgrade;
        /// <summary>Rooms it may stand in.</summary>
        public Room[] AllowedRooms = { Room.Workshop };
        /// <summary>Must back onto a wall (its local -Z face against it).</summary>
        public bool WallBacked;
        /// <summary>Fixed furniture: shown in build mode, never picked up (the checkout counter, the shutter).</summary>
        public bool Movable = true;
        /// <summary>Turned on once the fixture is both owned and placed.</summary>
        public GameObject Body;
        /// <summary>Number of display or sale slots it adds, for the overview panel.</summary>
        public int Slots;
        public float Price;

        /// <summary>Appears at its authored spot the moment it is owned, with no placement step (fittings, not machines).</summary>
        public bool SitedByDefault;

        [System.NonSerialized] public Vector3 DefaultPosition;
        [System.NonSerialized] public float DefaultYaw;

        private static readonly List<PlaceableFixture> _all = new List<PlaceableFixture>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _all.Clear();

        /// <summary>
        /// Every fixture in the scene, including the ones inside an expansion root that has not been switched on yet:
        /// a Stage-2 machine has to appear in the catalogue the moment the stage is bought, and it never had an Awake.
        /// </summary>
        public static List<PlaceableFixture> All
        {
            get { if (_all.Count == 0) Rescan(); return _all; }
        }

        public static void Rescan()
        {
            _all.Clear();
            foreach (var f in Object.FindObjectsByType<PlaceableFixture>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (f == null) continue;
                f.CaptureDefault();
                _all.Add(f);
            }
        }

        private bool _defaultCaptured;

        /// <summary>Remember where the scene put it, once: build mode falls back to this until the player sites it.</summary>
        public void CaptureDefault(bool force = false)
        {
            if (_defaultCaptured && !force) return;
            _defaultCaptured = true;
            DefaultPosition = transform.position;
            DefaultYaw = transform.eulerAngles.y;
        }

        private void Awake()
        {
            CaptureDefault();
            if (!_all.Contains(this)) _all.Add(this);
        }

        private void OnDestroy() => _all.Remove(this);

        public bool Owned
        {
            get
            {
                if (string.IsNullOrEmpty(RequiresUpgrade)) return true;
                var st = GameSession.Instance != null ? GameSession.Instance.State : null;
                return st != null && UpgradeCatalog.Has(st, RequiresUpgrade);
            }
        }

        /// <summary>Where the save says it stands, or the authored default the first time.</summary>
        public FixturePose Pose
        {
            get
            {
                var st = GameSession.Instance != null ? GameSession.Instance.State : null;
                var p = st != null ? st.Fixture(Id) : null;
                return p ?? new FixturePose { Id = Id, Position = DefaultPosition, Yaw = DefaultYaw, Placed = false };
            }
        }

        /// <summary>Owned and standing somewhere: only then does the machine exist in the room.</summary>
        public bool Sited
        {
            get
            {
                if (!Owned) return false;
                if (SitedByDefault) return true;
                var p = Pose;
                return p.Placed;
            }
        }

        /// <summary>
        /// Is anything bought but still in its crate? The tutorial's build-mode step only appears once the player
        /// has a machine waiting to be sited, so it never asks them to place something they do not own.
        /// </summary>
        public static bool AnyCratedFor(GameState st)
        {
            if (st == null) return false;
            foreach (var f in All) if (f != null && f.Owned && !f.SitedByDefault && !f.Sited) return true;
            return false;
        }

        /// <summary>Sited state for a station root; true when the object carries no fixture at all.</summary>
        public static bool SitedFor(Component c)
        {
            if (c == null) return true;
            var f = c.GetComponent<PlaceableFixture>();
            return f == null || f.Sited;
        }

        /// <summary>The body's world-space box for a candidate pose.</summary>
        public void BodyBox(Vector3 pos, float yaw, out Vector3 centre, out Vector3 halfExtents, out Quaternion rot)
        {
            rot = Quaternion.Euler(0f, yaw, 0f);
            centre = pos + rot * new Vector3(BodyOffset.x, Height * 0.5f, BodyOffset.y);
            halfExtents = new Vector3(Footprint.x * 0.5f, Height * 0.5f, Footprint.y * 0.5f);
        }

        /// <summary>The operator's standing space, on the side <see cref="ClearanceDir"/> names. Empty when nobody stands at it.</summary>
        public void ClearanceBox(Vector3 pos, float yaw, out Vector3 centre, out Vector3 halfExtents, out Quaternion rot)
        {
            float d = Mathf.Max(0f, Clearance);
            var dir = ClearanceDir.sqrMagnitude < 0.01f ? new Vector2(1f, 0f) : ClearanceDir.normalized;
            // the box hangs off whichever face the operator uses, and is as wide as that face
            bool alongX = Mathf.Abs(dir.x) > Mathf.Abs(dir.y);
            float faceHalf = alongX ? Footprint.x * 0.5f : Footprint.y * 0.5f;
            float sideHalf = ClearanceWidth > 0.01f ? ClearanceWidth * 0.5f : (alongX ? Footprint.y * 0.5f : Footprint.x * 0.5f);
            var localCentre = new Vector3(BodyOffset.x, 0.9f, BodyOffset.y) + new Vector3(dir.x, 0f, dir.y) * (faceHalf + d * 0.5f);
            rot = Quaternion.Euler(0f, yaw, 0f);
            centre = pos + rot * localCentre;
            halfExtents = alongX ? new Vector3(d * 0.5f, 0.9f, sideHalf) : new Vector3(sideHalf, 0.9f, d * 0.5f);
        }

        public void ApplyPose(FixturePose p)
        {
            transform.SetPositionAndRotation(p.Position, Quaternion.Euler(0f, p.Yaw, 0f));
        }
    }
}
