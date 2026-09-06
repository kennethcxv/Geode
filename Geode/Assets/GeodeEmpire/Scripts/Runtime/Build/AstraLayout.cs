using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeodeEmpire.Build
{
    /// <summary>
    /// Measured Astra architecture proposal, shared by the existing scene-builder study and its eventual
    /// production apply. It does not change the live ShopPlan until scene, routing and save migration are ready.
    /// Sizes describe real floor envelopes; decorative detail cannot consume the reserved working space.
    /// </summary>
    public static class AstraLayout
    {
        public const float Height = 2.9f;
        public static readonly Rect Starter = Rect.MinMaxRect(-6.4f, -2.7f, -0.4f, 1.3f);
        public static readonly Rect Processing = Rect.MinMaxRect(-6.4f, 1.3f, 1.4f, 6f);
        public static readonly Rect Showroom = Rect.MinMaxRect(1.4f, -2.7f, 7f, 6f);
        public static readonly Rect Office = Rect.MinMaxRect(-0.4f, -2.7f, 1.4f, 1.3f);

        [Serializable]
        public sealed class Space
        {
            public string Id, Zone;
            public Vector2 Centre, Size;
            public float Height, Yaw;
            public Vector2 Operator, OperatorSize;
            public Rect Body => new Rect(Centre - Size * 0.5f, Size);
            public Rect Work => new Rect(Operator - OperatorSize * 0.5f, OperatorSize);
            public bool HasWork => OperatorSize.x > 0f && OperatorSize.y > 0f;
        }

        private static Space S(string id, string zone, float x, float z, float w, float d, float height,
            float yaw = 0f, float ox = 0f, float oz = 0f, float ow = 0f, float od = 0f)
            => new Space { Id = id, Zone = zone, Centre = new Vector2(x, z), Size = new Vector2(w, d),
                Height = height, Yaw = yaw, Operator = new Vector2(ox, oz), OperatorSize = new Vector2(ow, od) };

        // Quarter-turn machine envelopes are already expressed in world X/Z. Yaw is for the authored visual root.
        public static readonly Space[] Spaces =
        {
            S("starter_checkout", "Starter", -4.45f, -0.15f, 2.60f, 0.85f, 0.95f, 0f, -4.45f, 0.75f, 0.90f, 0.90f),
            S("cracking_bench", "Starter", -0.90f, 0.25f, 0.749f, 1.80f, 0.90f, 90f, -1.95f, 0.25f, 0.95f, 1.60f),
            S("starter_receiving_1", "Starter", -1.20f, -2.05f, 1.20f, 0.80f, 0.45f),
            S("starter_receiving_2", "Starter", -2.55f, -2.05f, 1.20f, 0.80f, 0.45f),
            S("wash_station", "Processing", -5.95f, 3.10f, 0.80f, 1.15f, 0.92f, 270f, -4.95f, 3.10f, 1.0f, 1.15f),
            S("inspection_station", "Processing", -4.90f, 1.82f, 1.35f, 0.64f, 0.90f, 180f, -4.90f, 2.77f, 1.20f, 0.90f),
            S("trim_saw", "Processing", 0.15f, 5.45f, 1.78f, 0.86f, 1.30f, 0f, 0.15f, 4.47f, 1.50f, 0.95f),
            S("geode_cracker", "Processing", -1.63f, 5.48f, 0.72f, 0.66f, 1.65f, 0f, -1.63f, 4.50f, 0.90f, 0.95f),
            S("flat_lap", "Processing", -1.18f, 1.83f, 0.68f, 0.78f, 1.0f, 270f, -1.18f, 2.87f, 0.90f, 0.95f),
            S("bay_1", "Processing", -4.70f, 4.47f, 1.20f, 0.80f, 0.45f),
            S("bay_2", "Processing", -3.35f, 4.47f, 1.20f, 0.80f, 0.45f),
            S("bay_3", "Processing", -4.70f, 5.47f, 1.20f, 0.80f, 0.45f),
            S("bay_4", "Processing", -3.35f, 5.47f, 1.20f, 0.80f, 0.45f),
            S("showroom_checkout", "Showroom", 3.00f, -0.65f, 0.85f, 2.60f, 0.95f, 270f, 2.03f, -0.65f, 0.90f, 0.90f),
            S("shop_island", "Showroom", 4.45f, 2.70f, 1.50f, 1.0f, 1.05f),
            S("display_wall_a", "Showroom", 6.70f, 1.20f, 0.44f, 1.55f, 1.65f, 90f),
            S("display_wall_b", "Showroom", 6.70f, 4.25f, 0.44f, 1.55f, 1.65f, 90f),
            S("display_wall_c", "Showroom", 3.20f, 5.68f, 1.55f, 0.44f, 1.65f, 180f),
            S("display_cabinet", "Office", 0.50f, -2.30f, 1.30f, 0.44f, 1.75f, 0f, 0.50f, -1.46f, 1.10f, 0.90f),
        };

        public static readonly Vector2 StarterDoor = new Vector2(-5.60f, -2.70f);
        public static readonly Vector2 ShowroomDoor = new Vector2(5.60f, -2.70f);
        public static readonly Vector2 StarterCustomer = new Vector2(-4.45f, -0.90f);
        public static readonly Vector2[] StarterQueue = { new Vector2(-4.45f, -1.65f), new Vector2(-4.45f, -2.40f) };
        public static readonly Vector2[] StarterArrival =
            { new Vector2(-5.60f, -3.60f), new Vector2(-5.60f, -2.10f), new Vector2(-5.55f, -1.05f), StarterCustomer };
        public static readonly Vector2[] StarterStaff =
            { new Vector2(-1.95f, 0.25f), new Vector2(-2.75f, 0.30f), new Vector2(-2.75f, 0.75f), new Vector2(-4.45f, 0.75f) };

        public static Space Get(string id)
        {
            foreach (var space in Spaces) if (space.Id == id) return space;
            throw new ArgumentException("Unknown Astra layout space: " + id, nameof(id));
        }

        public static Rect Bounds(string zone) => zone switch
        {
            "Starter" => Starter, "Processing" => Processing, "Showroom" => Showroom, "Office" => Office,
            _ => throw new ArgumentException("Unknown Astra zone: " + zone, nameof(zone))
        };

        /// <summary>Conservative floor-envelope check for the study. Actual colliders, paths and Play remain gates.</summary>
        public static List<string> Audit()
        {
            var errors = new List<string>();
            for (int i = 0; i < Spaces.Length; i++)
            {
                var a = Spaces[i]; var zone = Bounds(a.Zone); var body = a.Body;
                if (body.xMin < zone.xMin || body.xMax > zone.xMax || body.yMin < zone.yMin || body.yMax > zone.yMax)
                    errors.Add(a.Id + ": body outside " + a.Zone);
                if (a.HasWork && (a.Work.xMin < zone.xMin || a.Work.xMax > zone.xMax || a.Work.yMin < zone.yMin || a.Work.yMax > zone.yMax))
                    errors.Add(a.Id + ": operator outside " + a.Zone);
                if (a.HasWork && Mathf.Min(a.OperatorSize.x, a.OperatorSize.y) < 0.9f)
                    errors.Add(a.Id + ": operator area narrower than 0.9 m");
                for (int j = 0; j < Spaces.Length; j++)
                {
                    if (j == i) continue;
                    var b = Spaces[j];
                    if (j > i && body.Overlaps(b.Body)) errors.Add(a.Id + " overlaps " + b.Id);
                    if (a.HasWork && a.Work.Overlaps(b.Body)) errors.Add(a.Id + ": operator blocked by " + b.Id);
                }
            }
            return errors;
        }
    }
}
