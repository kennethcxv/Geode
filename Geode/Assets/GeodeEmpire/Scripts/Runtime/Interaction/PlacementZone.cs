using System;
using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Core;
using GeodeEmpire.Player;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Interaction
{
    public enum ZoneKind { Cradle, SellTray, Scale, DisplaySlot, Shelf, SaleSlot, Counter, Wash, Saw, SawTray, Lap }

    /// <summary>
    /// A physical spot specimens can be placed into (and taken from). Trays hold several, slots hold one.
    /// Placing is the game's sorting mechanic: no menus, just put the rock where it belongs.
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

        public readonly List<SpecimenEntity> Occupants = new List<SpecimenEntity>();
        public event Action<PlacementZone, SpecimenEntity> Placed;
        public event Action<PlacementZone, SpecimenEntity> Taken;

        public bool IsFull => Occupants.Count >= Capacity;
        public bool IsEmpty => Occupants.Count == 0;
        public SpecimenEntity First => Occupants.Count > 0 ? Occupants[0] : null;

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
            _ => SpecimenLocation.World,
        };

        /// <summary>Station-specific acceptance (the saw takes whole rough and sawn pieces, the lap takes cut faces).</summary>
        public System.Func<SpecimenEntity, string> ExtraRefusal;

        public bool Accepts(SpecimenEntity e)
        {
            if (e == null || Locked || IsFull) return false;
            bool opened = e.Record.IsOpened;
            return opened ? AcceptsOpened : AcceptsUnopened;
        }

        /// <summary>Why a held specimen cannot go here (null when it can); shown as the prompt so a full or locked spot is never silent.</summary>
        public string RefusalReason(SpecimenEntity e)
        {
            if (e == null) return null;
            if (Locked) return Kind == ZoneKind.DisplaySlot ? "Locked shelf: buy the Cabinet Shelf Expansion" : Kind == ZoneKind.SaleSlot ? "Locked: buy the Second Sales Case" : $"{DisplayLabel} is locked";
            if (IsFull) return Kind == ZoneKind.DisplaySlot || Kind == ZoneKind.SaleSlot ? "Slot taken: pick a free slot or swap it out" : $"{DisplayLabel} is full";
            bool opened = e.Record.IsOpened;
            if (opened && !AcceptsOpened) return "Unopened rocks only";
            if (!opened && !AcceptsUnopened) return Kind == ZoneKind.DisplaySlot || Kind == ZoneKind.SaleSlot ? "Crack it open first" : "Opened specimens only";
            if (Kind == ZoneKind.SaleSlot && !e.Record.Appraised) return "Appraise it first: the scale sets the price";
            if (Kind == ZoneKind.Wash && e.Visual != null && e.Visual.DirtRemaining < 0.04f) return "Already clean";
            if (ExtraRefusal != null) { string why = ExtraRefusal(e); if (why != null) return why; }
            return null;
        }

        public override bool CanInteract(PlayerInteractor player)
        {
            if (player.Held != null) return true;          // accepted, or a refusal the prompt explains
            if (Locked) return false;
            if (Occupants.Count == 0 || Occupants[Occupants.Count - 1].Locked) return false;
            // a dirty rock in the tub is scrubbed, not taken: the tub itself carries that prompt
            if (Kind == ZoneKind.Wash && Occupants[0].Visual != null && Occupants[0].Visual.DirtRemaining > 0.02f) return false;
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
                if (top.Locked) return;
                Take(top);
                player.PickUp(top);
            }
        }

        public Vector3 SlotLocalOffset(int index)
        {
            if (Capacity <= 1) return Vector3.zero;
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
            var anchor = Anchor != null ? Anchor : transform;
            e.SetPhysics(false);
            e.transform.SetParent(null, true);
            e.Zone = this;
            e.Locked = false;
            var rot = anchor.rotation * Quaternion.Euler(0f, SeededYaw(e, idx), 0f);
            var pos = anchor.TransformPoint(SlotLocalOffset(idx)) + Vector3.up * e.RestHeightOffset(Kind == ZoneKind.DisplaySlot || Kind == ZoneKind.Scale || Kind == ZoneKind.Cradle || Kind == ZoneKind.SaleSlot);
            e.SetPose(pos, rot);
            e.Record.Location = LocationFor();
            e.Record.LocationIndex = IsIndexedSlot ? SlotIndex : idx;   // which slot, not which occupant: the reload looks it up by this
            e.Record.WorldPosition = pos;
            e.Record.WorldRotation = rot;
            if (!silent) Placed?.Invoke(this, e);
        }

        private static float SeededYaw(SpecimenEntity e, int idx) => (float)((e.Record.Seed >> 8) % 360) + idx * 37f;

        public void Take(SpecimenEntity e, bool silent = false)
        {
            if (e == null || !Occupants.Contains(e)) return;
            Occupants.Remove(e);
            e.Zone = null;
            if (!silent) Taken?.Invoke(this, e);
            // re-pack remaining occupants
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
