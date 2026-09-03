using System;
using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Core;
using GeodeEmpire.Player;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Interaction
{
    public enum ZoneKind { Cradle, SellTray, Scale, DisplaySlot, Shelf, SaleSlot, Counter, Wash, Saw, SawTray, Lap, Rack, Cracker }

    /// <summary>
    /// A physical spot specimens can be placed into (and taken from). Trays hold several, slots hold one.
    /// Placing is the game's sorting mechanic: no menus, just put the rock where it belongs.
    ///
    /// V5: every zone knows the real supporting surface it offers per slot (SupportHalfSize) and validates the
    /// specimen's full footprint against it, not just its pivot: an opened geode is posed side by side when the shelf
    /// is wide enough, propped clamshell-style when it is not, and refused with a reason when it physically does not
    /// fit. Trays pack their occupants by real size along rows instead of a fixed grid.
    /// </summary>
    public sealed class PlacementZone : InteractableBehaviour
    {
        public ZoneKind Kind;
        public string DisplayLabel = "Tray";
        public int SlotIndex;
        /// <summary>Single-piece slots addressed by SlotIndex across a save (cabinet and sale fixtures).</summary>
        public bool IsIndexedSlot => Kind == ZoneKind.DisplaySlot || Kind == ZoneKind.SaleSlot;
        public int Capacity = 1;
        public bool AcceptsOpened = true;
        public bool AcceptsUnopened = true;
        public Vector2 GridSpacing = new Vector2(0.11f, 0.1f);
        public int GridColumns = 4;
        public Transform Anchor;
        public bool Locked;
        /// <summary>Half-extents (local x, z) of the surface each slot really offers, measured from the slot centre.</summary>
        public Vector2 SupportHalfSize = new Vector2(0.15f, 0.15f);
        /// <summary>Trays that pack occupants by their real size in rows along local x (dealer outbox, rack shelves, saw tray).</summary>
        public bool Packed;
        /// <summary>Yaw (about the anchor) a displayed geode faces: 0 keeps the clamshell open toward local -Z, 180 toward +Z.</summary>
        public float PoseYaw;
        public const float PackGap = 0.02f;
        public const float FitMargin = 0.008f;

        public readonly List<SpecimenEntity> Occupants = new List<SpecimenEntity>();
        public event Action<PlacementZone, SpecimenEntity> Placed;
        public event Action<PlacementZone, SpecimenEntity> Taken;

        public bool IsFull => Occupants.Count >= Capacity;
        public bool IsEmpty => Occupants.Count == 0;
        public SpecimenEntity First => Occupants.Count > 0 ? Occupants[0] : null;
        public bool IsDisplayKind => Kind == ZoneKind.DisplaySlot || Kind == ZoneKind.SaleSlot;

        public SpecimenLocation LocationFor() => Kind switch
        {
            ZoneKind.Cradle => SpecimenLocation.Bench,
            ZoneKind.SellTray => SpecimenLocation.SellTray,
            ZoneKind.Scale => SpecimenLocation.AppraisalStation,
            ZoneKind.DisplaySlot => SpecimenLocation.DisplaySlot,
            ZoneKind.SaleSlot => SpecimenLocation.SaleSlot,
            ZoneKind.Wash => SpecimenLocation.WashTub,
            ZoneKind.Saw => SpecimenLocation.Saw,
            ZoneKind.Lap => SpecimenLocation.Lap,
            ZoneKind.Rack => SpecimenLocation.Rack,
            ZoneKind.Cracker => SpecimenLocation.Cracker,
            _ => SpecimenLocation.World,
        };

        /// <summary>Station-specific acceptance (the saw takes whole rough and sawn pieces, the lap takes cut faces).</summary>
        public System.Func<SpecimenEntity, string> ExtraRefusal;
        /// <summary>A station may claim the empty-handed press on its occupant (resume a committed cut) instead of a take.</summary>
        public System.Func<SpecimenEntity, string> ResumePrompt;
        public System.Action<SpecimenEntity> ResumeAction;

        public bool Accepts(SpecimenEntity e)
        {
            if (e == null || Locked || IsFull) return false;
            bool opened = e.Record.IsOpened;
            return opened ? AcceptsOpened : AcceptsUnopened;
        }

        // ------------------------------------------------------------------------------------------------
        // Footprint / pose
        // ------------------------------------------------------------------------------------------------
        /// <summary>The pose an opened geode takes here: closed up on storage trays, side by side where the shelf is wide enough, clamshell otherwise.</summary>
        public DisplayPose PoseFor(SpecimenEntity e)
        {
            if (e == null || !e.IsOpened || e.IsPiece) return DisplayPose.Natural;
            switch (Kind)
            {
                case ZoneKind.Rack:
                case ZoneKind.SellTray:
                case ZoneKind.SawTray:
                    return DisplayPose.Closed;
                case ZoneKind.DisplaySlot:
                case ZoneKind.SaleSlot:
                    return Fits(e, DisplayPose.Natural) ? DisplayPose.Natural : DisplayPose.Clamshell;
                case ZoneKind.Scale:
                    // the whole specimen is weighed: opened flat if the platform takes it, propped if not, closed up for the big ones
                    return Fits(e, DisplayPose.Natural) ? DisplayPose.Natural : Fits(e, DisplayPose.Clamshell) ? DisplayPose.Clamshell : DisplayPose.Closed;
                case ZoneKind.Wash:
                    // an opened rock is dunked for a rinse: propped in the sink, or closed up if it is a big one
                    return Fits(e, DisplayPose.Clamshell) ? DisplayPose.Clamshell : DisplayPose.Closed;
                default:
                    return DisplayPose.Natural;
            }
        }

        /// <summary>Does the specimen's footprint in this pose fit inside one slot's support rectangle?</summary>
        public bool Fits(SpecimenEntity e, DisplayPose pose)
        {
            var b = e.FootprintFor(pose);
            return b.extents.x <= SupportHalfSize.x + FitMargin && b.extents.z <= SupportHalfSize.y + FitMargin;
        }

        /// <summary>Radius a closed-up rock (or piece) takes on a packed tray.</summary>
        private static float PackRadius(SpecimenEntity e)
        {
            var b = e.FootprintFor(DisplayPose.Closed);
            return Mathf.Max(Mathf.Abs(b.min.x), Mathf.Abs(b.max.x), Mathf.Abs(b.min.z), Mathf.Abs(b.max.z)) + 0.004f;
        }

        /// <summary>Row packing of closed rocks along local x within the support rectangle; false when they do not all fit.</summary>
        private bool TryPack(List<SpecimenEntity> items, List<Vector3> outLocal)
        {
            float hx = SupportHalfSize.x, hz = SupportHalfSize.y;
            float x = -hx, rowZ = -hz, rowH = 0f;
            outLocal.Clear();
            foreach (var e in items)
            {
                float r = PackRadius(e);
                if (r > hx + FitMargin || r > hz + FitMargin) return false;
                if (x > -hx + 1e-4f && x + 2f * r > hx + FitMargin) { rowZ += rowH + PackGap; x = -hx; rowH = 0f; }
                if (rowZ + 2f * r > hz + FitMargin) return false;
                outLocal.Add(new Vector3(x + r, 0f, rowZ + r));
                x += 2f * r + PackGap;
                rowH = Mathf.Max(rowH, 2f * r);
            }
            return true;
        }

        private readonly List<Vector3> _packScratch = new List<Vector3>();
        private readonly List<SpecimenEntity> _packItems = new List<SpecimenEntity>();

        /// <summary>Why a held specimen physically cannot go here (null when it fits).</summary>
        public string FitRefusal(SpecimenEntity e)
        {
            if (e == null) return null;
            if (Packed)
            {
                _packItems.Clear(); _packItems.AddRange(Occupants); _packItems.Add(e);
                _packItems.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
                if (TryPack(_packItems, _packScratch)) return null;
                return Occupants.Count == 0 ? $"Too big for {DisplayLabel}" : $"No room in {DisplayLabel} for a rock this size";
            }
            if (Fits(e, PoseFor(e))) return null;
            return Kind switch
            {
                ZoneKind.DisplaySlot => "Too big for this shelf: the trophy wall takes the big pieces",
                ZoneKind.SaleSlot => "Too big for this spot: try the island table",
                _ => $"Too big for {DisplayLabel}",
            };
        }

        /// <summary>Why a held specimen cannot go here (null when it can); shown as the prompt so a full or locked spot is never silent.</summary>
        public string RefusalReason(SpecimenEntity e)
        {
            if (e == null) return null;
            if (Locked) return Kind == ZoneKind.DisplaySlot ? "Locked shelf: buy the Cabinet Shelf Expansion" : Kind == ZoneKind.SaleSlot ? "Locked: buy the Showroom Island Table" : $"{DisplayLabel} is locked";
            if (IsFull) return Kind == ZoneKind.DisplaySlot || Kind == ZoneKind.SaleSlot ? "Slot taken: pick a free slot or swap it out" : $"{DisplayLabel} is full";
            bool opened = e.Record.IsOpened;
            if (opened && !AcceptsOpened) return "Unopened rocks only";
            if (!opened && !AcceptsUnopened) return Kind == ZoneKind.DisplaySlot || Kind == ZoneKind.SaleSlot ? "Crack it open first" : "Opened specimens only";
            if (Kind == ZoneKind.SaleSlot && !e.Record.Appraised) return "Appraise it first: the scale sets the price";
            if ((Kind == ZoneKind.SaleSlot || Kind == ZoneKind.SellTray) && e.Record.Favorite) return "A favourite: take the star off it on the tablet before selling";
            if (Kind == ZoneKind.Wash && e.Visual != null && e.Visual.DirtRemaining < 0.04f && !(opened && !e.IsPiece && e.Record.Condition != null && !e.Record.Condition.Rinsed)) return "Already clean";
            if (ExtraRefusal != null) { string why = ExtraRefusal(e); if (why != null) return why; }
            string fit = FitRefusal(e);
            if (fit != null) return fit;
            return null;
        }

        public override bool CanInteract(PlayerInteractor player)
        {
            if (player.Held != null) return true;          // accepted, or a refusal the prompt explains
            if (Locked) return false;
            if (Occupants.Count == 0) return false;
            // a rock with a committed cut stays in the clamp (locked): the press resumes the cut instead
            if (Kind == ZoneKind.Saw && Occupants[0].Record.CutCommitted) return ResumePrompt != null && ResumePrompt(Occupants[0]) != null;
            if (Occupants[Occupants.Count - 1].Locked) return false;
            // a station may claim the press on its occupant (scrub, polish): hold works it, a tap takes it
            if (ResumePrompt != null && ResumePrompt(Occupants[Occupants.Count - 1]) != null) return true;
            return true;
        }

        public override string GetPrompt(PlayerInteractor player)
        {
            if (player.Held != null)
            {
                string why = RefusalReason(player.Held);
                if (why != null) return why;
                string verb = Kind == ZoneKind.Cradle ? "Set on" : Kind == ZoneKind.Scale ? "Weigh on" : Kind == ZoneKind.SaleSlot ? "Put up for sale on" : Kind == ZoneKind.Wash ? "Dunk in" : Kind == ZoneKind.Saw ? "Clamp in" : Kind == ZoneKind.Lap ? "Set face-down on" : "Place in";
                return $"{verb} {DisplayLabel}";
            }
            var top = Occupants.Count > 0 ? Occupants[Occupants.Count - 1] : null;
            if (top != null && ResumePrompt != null) { string resume = ResumePrompt(top); if (resume != null) return resume; }
            return top != null ? $"Take {top.ShortName}" : "";
        }

        public override void Interact(PlayerInteractor player)
        {
            if (player.Held != null)
            {
                var e = player.Held;
                string why = RefusalReason(e);
                if (why != null)
                {
                    GameSession.Instance?.Notify(why, NotificationKind.Warning);
                    Audio.WorkshopAudio.Play2D("ui_error", 0.4f);
                    return;
                }
                player.ReleaseHeld();
                Place(e);
            }
            else if (Occupants.Count > 0)
            {
                var top = Occupants[Occupants.Count - 1];
                if (ResumePrompt != null && ResumePrompt(top) != null && ResumeAction != null) { ResumeAction(top); return; }
                if (top.Locked) return;
                Take(top);
                player.PickUp(top);
            }
        }

        public Vector3 SlotLocalOffset(int index)
        {
            if (Capacity <= 1 || Packed) return Vector3.zero;
            int cols = Mathf.Max(1, GridColumns);
            int rows = Mathf.CeilToInt(Capacity / (float)cols);
            int cx = index % cols, cz = index / cols;
            float x = (cx - (cols - 1) * 0.5f) * GridSpacing.x;
            float z = (cz - (rows - 1) * 0.5f) * GridSpacing.y;
            return new Vector3(x, 0f, z);
        }

        /// <summary>Snap a specimen into this zone (used both by player action and by save restore).</summary>
        public void Place(SpecimenEntity e, bool silent = false)
        {
            if (e == null) return;
            if (e.Zone != null && e.Zone != this) e.Zone.Take(e, true);
            if (!Occupants.Contains(e)) Occupants.Add(e);
            int idx = Occupants.IndexOf(e);
            e.SetPhysics(false);
            e.transform.SetParent(null, true);
            e.Zone = this;
            e.Locked = false;
            var pose = PoseFor(e);
            e.ApplyPose(pose);
            if (Packed) Repack();
            else Seat(e, idx, pose);
            e.Record.Location = LocationFor();
            e.Record.LocationIndex = IsIndexedSlot ? SlotIndex : idx;   // which slot, not which occupant: the reload looks it up by this
            e.Record.WorldPosition = e.transform.position;
            e.Record.WorldRotation = e.transform.rotation;
            if (!silent) Placed?.Invoke(this, e);
        }

        /// <summary>Stand one occupant on its slot: the footprint's centre lands on the slot centre, its lowest hull point on the surface.</summary>
        private void Seat(SpecimenEntity e, int idx, DisplayPose pose)
        {
            var anchor = Anchor != null ? Anchor : transform;
            float yaw = IsDisplayKind && e.IsOpened && !e.IsPiece ? PoseYaw : SeededYaw(e, idx);
            var rot = anchor.rotation * Quaternion.Euler(0f, yaw, 0f);
            var fp = e.FootprintFor(pose);
            var shift = IsDisplayKind ? -new Vector3(fp.center.x, 0f, fp.center.z) : Vector3.zero;
            var pos = anchor.TransformPoint(SlotLocalOffset(idx)) + rot * shift + Vector3.up * e.RestHeightOffset(Kind == ZoneKind.DisplaySlot || Kind == ZoneKind.Scale || Kind == ZoneKind.Cradle || Kind == ZoneKind.SaleSlot);
            e.SetPose(pos, rot);
        }

        /// <summary>Lay every occupant of a packed tray out again by real size (a tray never overlaps or overhangs).</summary>
        private readonly List<SpecimenEntity> _packOrder = new List<SpecimenEntity>();

        private void Repack()
        {
            var anchor = Anchor != null ? Anchor : transform;
            _packOrder.Clear(); _packOrder.AddRange(Occupants);
            _packOrder.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            bool ok = TryPack(_packOrder, _packScratch);
            for (int i = 0; i < _packOrder.Count; i++)
            {
                var o = _packOrder[i];
                Vector3 local = ok ? _packScratch[i] : new Vector3((i % 4 - 1.5f) * 0.17f, 0f, (i / 4 - 1f) * 0.15f);   // overflow (restore of an old save): grid fallback
                var rot = anchor.rotation * Quaternion.Euler(0f, SeededYaw(o, i), 0f);
                var pos = anchor.TransformPoint(local) + Vector3.up * o.RestHeightOffset(false);
                o.SetPose(pos, rot);
                o.Record.WorldPosition = pos;
                o.Record.WorldRotation = rot;
                if (!IsIndexedSlot) o.Record.LocationIndex = Occupants.IndexOf(o);
            }
        }

        private static float SeededYaw(SpecimenEntity e, int idx) => (float)((e.Record.Seed >> 8) % 360) + idx * 37f;

        public void Take(SpecimenEntity e, bool silent = false)
        {
            if (e == null || !Occupants.Contains(e)) return;
            Occupants.Remove(e);
            e.Zone = null;
            if (PoseFor(e) != DisplayPose.Natural) e.ApplyPose(DisplayPose.Natural);
            if (!silent) Taken?.Invoke(this, e);
            if (Packed) { Repack(); return; }
            // re-pack remaining occupants of a grid tray
            for (int i = 0; i < Occupants.Count; i++)
            {
                var o = Occupants[i];
                var anchor = Anchor != null ? Anchor : transform;
                var pos = anchor.TransformPoint(SlotLocalOffset(i)) + Vector3.up * o.RestHeightOffset(false);
                o.SetPose(pos, o.transform.rotation);
                o.Record.LocationIndex = IsIndexedSlot ? SlotIndex : i;
                o.Record.WorldPosition = pos;
            }
        }
    }
}
