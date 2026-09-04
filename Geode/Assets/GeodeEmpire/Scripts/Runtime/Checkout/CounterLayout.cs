using System;
using UnityEngine;

namespace GeodeEmpire.Checkout
{
    /// <summary>
    /// Every pose and rect on the checkout counter, in the COUNTER'S OWN LOCAL FRAME — never world coordinates, so
    /// moving or rotating the counter cannot leave a device, a workspace or a camera looking at where the counter used
    /// to be. The Geode analogue of Golf's REGISTER datum block (src/data/shopLayout.js).
    ///
    /// Frame: local +X runs along the counter (the register block sits at +X, over the counter's closed cabinet), local
    /// +Z is the STAFF side (the cashier, behind the open shelves), local -Z is the customer side (the slatted panel and
    /// the LED strip), and y is measured from the counter's base. Metres — measured off the imported kit, not assumed.
    /// The kit's screens and keypads face their own local +Z, so a device at yaw 0 already looks at the cashier and the
    /// customer display is the one that carries yaw 180.
    /// </summary>
    [CreateAssetMenu(fileName = "CounterLayout", menuName = "Geode Empire/Counter Layout")]
    public sealed class CounterLayout : ScriptableObject
    {
        [Serializable] public struct Pose { public float X, Z, Yaw; public Pose(float x, float z, float yaw = 0f) { X = x; Z = z; Yaw = yaw; } }
        [Serializable] public struct Rect { public float MinX, MaxX, MinZ, MaxZ;
            public Rect(float minX, float maxX, float minZ, float maxZ) { MinX = minX; MaxX = maxX; MinZ = minZ; MaxZ = maxZ; }
            public float CentreX => (MinX + MaxX) * 0.5f; public float CentreZ => (MinZ + MaxZ) * 0.5f;
            public float Width => MaxX - MinX; public float Depth => MaxZ - MinZ;
            public bool Contains(float x, float z) => x >= MinX && x <= MaxX && z >= MinZ && z <= MaxZ; }

        [Header("Counter body")]
        public float Length = 2.6f;
        public float Depth = 0.85f;
        public float TopY = 0.95f;

        [Header("Devices (authored mounts on checkout_counter)")]
        public Pose Monitor = new Pose(0.86f, -0.16f, 0f);
        public Pose Terminal = new Pose(0.38f, 0.30f, 0f);
        public Pose CustomerDisplay = new Pose(1.14f, -0.33f, 0f);   // its head is authored already turned, so zero faces the queue
        public Pose BagStation = new Pose(-1.02f, 0.08f, 0f);
        public Vector3 Drawer = new Vector3(0.86f, 0.72f, -0.035f);
        /// <summary>Travel is longer than the model's own 0.32: at 0.32 the note row sits half under the counter slab and reads as the coin row (Golf playtest).</summary>
        public float DrawerTravel = 0.42f;

        [Header("Workspaces (counter-local; the counter runs x +-1.30 and is 0.85 deep, so z +-0.425)")]
        /// <summary>Where the customer sets the piece down: their half of the counter, just behind their own tender.</summary>
        public Rect Staging = new Rect(-0.29f, 0.33f, -0.20f, 0.10f);
        /// <summary>Rung-up goods stay visible here until the sale completes (the counter's authored scanned area).</summary>
        public Rect ScannedStaging = new Rect(-0.92f, -0.32f, -0.27f, 0.28f);
        /// <summary>The laid carrier's footprint; a rung-up piece slides sideways along the counter into its mouth.</summary>
        public Rect Bagging = new Rect(-1.24f, -0.80f, -0.10f, 0.26f);
        /// <summary>Counted change piles FLAT on the bare counter, clear of the terminal and the monitor.</summary>
        public Rect ChangeHandoff = new Rect(-0.30f, 0.08f, 0.14f, 0.34f);
        /// <summary>Where the customer's own cash lands: their own edge, in front of the goods, never among the change.</summary>
        public Rect CustomerTender = new Rect(-0.11f, 0.15f, -0.40f, -0.22f);

        [Header("Standing datums")]
        public Pose StaffStand = new Pose(0.60f, 0.95f);
        public Pose CustomerStand = new Pose(0.10f, -0.80f);

        [Header("Camera (Golf's derived working composition: the eye is pinned to the work, not the floor)")]
        /// <summary>Eye above the counter TOP. Golf 0.56: a standing eye sits too high to read a counter as a work surface.</summary>
        public float EyeAboveCounter = 0.56f;
        public float WorkingFov = 54f;
        public float WorkingGlanceScale = 0.34f;
        public float CashGlanceScale = 0.30f;
        public Pose WorkingEye = new Pose(0.15f, 1.05f);
        public Pose WorkingLook = new Pose(0.20f, -0.06f);
        public float WorkingLookAboveCounter = 0.10f;

        public float DrawerFov = 52f;
        public Pose DrawerEye = new Pose(0.80f, 1.00f);
        public float DrawerEyeAboveCounter = 0.74f;
        public Pose DrawerLook = new Pose(0.84f, 0.34f);
        public float DrawerLookAboveCounter = -0.06f;

        public float CardFov = 46f;
        public Pose CardEye = new Pose(0.36f, 0.92f);
        public float CardEyeAboveCounter = 0.36f;
        public Pose CardLook = new Pose(0.38f, 0.30f);
        public float CardLookAboveCounter = 0.08f;

        [Header("Timings (Golf playtest constants -- copy the number, copy the comment)")]
        public float SlideDuration = 0.55f;      // one forgiving click owns the whole ring-up gesture
        public float CardInsertTime = 0.72f;
        public float CardAuthTime = 1.15f;
        public float AutoPaymentHold = 0.38f;
        public float BagDeliverTime = 0.78f;
        public float BagCustomerHold = 1.25f;
        public float PaidBagAcceptanceHold = 1.4f;
        public float DrawerOpenSpeed = 3.2f;
        public float DrawerCloseSpeed = 2.4f;
        public float ProductPlaceSeconds = 0.58f;
        public float TerminalBusyDotHz = 3f;

        // ---- frame helpers: the counter Transform carries the rotation, so everything resolves through it ----
        public Vector3 Local(Pose p, float y) => new Vector3(p.X, y, p.Z);
        public Vector3 LocalRect(Rect r, float y) => new Vector3(r.CentreX, y, r.CentreZ);
        public Vector3 World(Transform counter, Pose p, float y) => counter.TransformPoint(Local(p, y));
        public Vector3 World(Transform counter, float x, float y, float z) => counter.TransformPoint(new Vector3(x, y, z));
        public Vector3 WorldRect(Transform counter, Rect r, float y) => counter.TransformPoint(LocalRect(r, y));
        public Quaternion WorldRot(Transform counter, float yaw) => counter.rotation * Quaternion.Euler(0f, yaw, 0f);
        /// <summary>The direction the cashier looks across the counter (counter-local -Z, toward the customer).</summary>
        public Vector3 AcrossCounter(Transform counter) => -counter.forward;
        public Vector3 AlongCounter(Transform counter) => counter.right;
    }
}

