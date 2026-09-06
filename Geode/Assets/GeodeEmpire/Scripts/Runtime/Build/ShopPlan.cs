using UnityEngine;

namespace GeodeEmpire.Build
{
    /// <summary>Which room a point is in. Fixtures are only valid in the rooms their kind belongs to.</summary>
    public enum Room { None = 0, Workshop = 1, BackOfHouse = 2, Showroom = 3 }

    /// <summary>
    /// The floor plan, in one place, so the scene builder and the placement rules can never disagree about where a
    /// wall is. Docs/VisualRebuild/PLAN.md B has the drawing.
    /// </summary>
    public static class ShopPlan
    {
        public const float XMin = -6.4f, XMax = 7.0f, ZMin = -2.7f, ZMax = 6.0f;
        public static float Height => AstraWorkshop.Active ? AstraLayout.Height : 3.2f;
        /// <summary>Workshop | showroom.</summary>
        public static float PartitionX => AstraWorkshop.Active ? AstraLayout.Showroom.xMin : 2.4f;
        /// <summary>Workshop | back of house.</summary>
        public static float BackZ => AstraWorkshop.Active ? AstraLayout.Processing.yMin : 3.2f;
        /// <summary>The two framed openings in the cross wall.</summary>
        public static float BackA0 => AstraWorkshop.Active ? -3.9f : -4.6f;
        public static float BackA1 => AstraWorkshop.Active ? -2.7f : -3.0f;
        public static float BackB0 => AstraWorkshop.Active ? -0.1f : -1.0f;
        public static float BackB1 => AstraWorkshop.Active ? 1.1f : 0.6f;
        /// <summary>The receiving-bay opening in the north wall.</summary>
        public const float BayX0 = -5.4f, BayX1 = -2.6f;
        public const float ShopDoorX = 5.6f, ShopDoorHalf = 0.5f;
        /// <summary>The workshop's own door in the south wall.</summary>
        public static float WorkDoorX => AstraWorkshop.Active ? AstraLayout.StarterDoor.x : -2.3f;
        public const float WorkDoorHalf = 0.55f;
        /// <summary>The staff doorway through the partition.</summary>
        public static float PartitionDoorZ0 => AstraWorkshop.Active ? 2.0f : 0.5f;
        public static float PartitionDoorZ1 => AstraWorkshop.Active ? 3.2f : 1.6f;
        /// <summary>
        /// Where the hoarding stands until the shop front is leased. It runs the workshop's full depth on the
        /// east jamb of the north opening, so it never cuts a doorway, and it seals the east end of the workshop
        /// and — through it — the partition's two openings, and so the showroom beyond. Day one is the workshop
        /// west of this line.
        /// </summary>
        public static float HoardX => AstraWorkshop.Active ? AstraLayout.Starter.xMax : 0.6f;

        /// <summary>Whether that room has been leased yet. A room the player has not taken on is not theirs to build in.</summary>
        public static bool Leased(Room r)
        {
            switch (r)
            {
                case Room.Workshop: return true;
                case Room.BackOfHouse: return Workshop.PremisesExpansion.BackRoomOpen;
                case Room.Showroom: return Workshop.PremisesExpansion.ShopFrontOpen;
                default: return false;
            }
        }

        /// <summary>
        /// Whether the player can stand at, walk through or build on this point today. Room membership is not
        /// enough: the east strip of the workshop and of the back room is behind the hoarding until the shop
        /// front is leased.
        /// </summary>
        public static bool Open(Vector3 p)
        {
            var r = RoomAt(p);
            if (r == Room.None || !Leased(r)) return false;
            if (!AstraWorkshop.Active && r == Room.Workshop && p.x > HoardX && !Workshop.PremisesExpansion.ShopFrontOpen) return false;
            return true;
        }

        /// <summary>The part of the floor plan the player has today, as a rectangle. Used by the route grid.</summary>
        public static void OpenBounds(out float x0, out float x1, out float z0, out float z1)
        {
            bool shop = Workshop.PremisesExpansion.ShopFrontOpen;
            bool back = Workshop.PremisesExpansion.BackRoomOpen;
            x0 = XMin; x1 = shop ? XMax : HoardX;
            z0 = ZMin; z1 = back ? ZMax : BackZ;
            if (back && !shop) x1 = Mathf.Max(x1, PartitionX);   // the back room runs the full width behind the hoarding
        }

        public static Room RoomAt(Vector3 p)
        {
            if (p.x < XMin || p.x > XMax || p.z < ZMin || p.z > ZMax) return Room.None;
            if (p.x > PartitionX) return Room.Showroom;
            if (AstraWorkshop.Active && p.x > AstraLayout.Starter.xMax) return Room.BackOfHouse; // office through processing
            return p.z > BackZ ? Room.BackOfHouse : Room.Workshop;
        }

        /// <summary>Doorways and framed openings: a fixture footprint may never touch one of these.</summary>
        public static Bounds[] Portals => AstraWorkshop.Active ? AstraPortals : LegacyPortals;

        private static readonly Bounds[] AstraPortals =
        {
            new Bounds(new Vector3(-5.6f, 1.05f, -2.2f), new Vector3(1.15f, 2.1f, 1.5f)),
            new Bounds(new Vector3(5.6f, 1.05f, -2.2f), new Vector3(1.15f, 2.1f, 1.5f)),
            new Bounds(new Vector3(-3.3f, 1.05f, 1.3f), new Vector3(1.2f, 2.1f, 1.1f)),
            new Bounds(new Vector3(0.5f, 1.05f, 1.3f), new Vector3(1.2f, 2.1f, 1.1f)),
            new Bounds(new Vector3(1.4f, 1.05f, 2.6f), new Vector3(1.1f, 2.1f, 1.2f)),
            new Bounds(new Vector3(-4f, 1.05f, 5.6f), new Vector3(2.8f, 2.1f, .8f)),
        };

        // Literal legacy coordinates keep loading this type in an Astra scene from changing the fallback plan.
        private static readonly Bounds[] LegacyPortals =
        {
            // south wall: the workshop door and the shop door
            new Bounds(new Vector3(-2.3f, 1.05f, ZMin + 0.55f), new Vector3(WorkDoorHalf * 2f, 2.1f, 1.5f)),
            new Bounds(new Vector3(ShopDoorX, 1.05f, ZMin + 0.55f), new Vector3(ShopDoorHalf * 2f + 0.4f, 2.1f, 1.5f)),
            // the two back-of-house openings
            new Bounds(new Vector3(-3.8f, 1.05f, 3.2f), new Vector3(1.6f, 2.1f, 1.7f)),
            new Bounds(new Vector3(-0.2f, 1.05f, 3.2f), new Vector3(1.6f, 2.1f, 1.7f)),
            // the staff doorway through the partition
            new Bounds(new Vector3(2.4f, 1.05f, 1.05f), new Vector3(1.6f, 2.1f, 1.1f)),
            // the serving opening over the checkout counter (customers must reach the counter face)
            new Bounds(new Vector3(3.15f, 1.05f, -1.1f), new Vector3(1.3f, 2.1f, 2.4f)),
            // the receiving bay, in front of the shutter
            new Bounds(new Vector3((BayX0 + BayX1) * 0.5f, 1.05f, ZMax - 0.75f), new Vector3(BayX1 - BayX0, 2.1f, 1.5f)),
        };
    }
}
