using System;
using GeodeEmpire.Economy;
using GeodeEmpire.Save;
using UnityEngine;

namespace GeodeEmpire.Build
{
    /// <summary>Data-only, repeat-safe recovery rules. Native fixture placement is validated separately.</summary>
    public static class AstraLayoutMigration
    {
        public static void InitializeNew(GameState state)
        {
            Grant(state, UpgradeCatalog.CounterTable);
            state.LayoutRevision = AstraWorkshop.Revision;
        }

        public static void PreserveLegacyRights(GameState state)
        {
            if (state.LayoutRevision >= AstraWorkshop.Revision) return;
            Grant(state, UpgradeCatalog.CounterTable);
            Grant(state, UpgradeCatalog.WashStation);
            Grant(state, UpgradeCatalog.AppraisalStation);
            Grant(state, UpgradeCatalog.StorageShelf);
            GrantRoom(state, UpgradeCatalog.BackRoom);
            // The old Stage-2/3 bundles exposed collection and sale fixtures even outside the leased showroom.
            if (state.WorkshopStage >= 2 || state.HasUpgrade(UpgradeCatalog.Stage2) || state.HasUpgrade(UpgradeCatalog.Stage3))
                GrantRoom(state, UpgradeCatalog.ShopFront);
        }

        public static void GrantRoom(GameState state, string room)
        {
            if (state.HasUpgrade(room)) return;
            Grant(state, room);
            state.LayoutRentCredit += room == UpgradeCatalog.BackRoom ? Ledger.BackRoomRent : Ledger.ShopFrontRent;
        }

        public static void Grant(GameState state, string id)
        {
            if (!string.IsNullOrEmpty(id) && !state.HasUpgrade(id)) state.Upgrades.Add(id);
        }

        /// <summary>Pack one real owned identity; retain its business/collection designation until actual pickup.</summary>
        public static CrateRecord Pack(GameState state, SpecimenRecord specimen)
        {
            if (specimen == null || specimen.Location == SpecimenLocation.Sold || specimen.Location == SpecimenLocation.Discarded
                || specimen.Location == SpecimenLocation.Cut || specimen.Location == SpecimenLocation.Consigned)
                throw new ArgumentException("Only physically owned stock can be recovered.");
            if (specimen.InRecovery)
            {
                var existing = state.FindCrate(specimen.RecoveryCrateId);
                if (existing == null || !existing.Recovery || !existing.SpecimenIds.Contains(specimen.Id))
                    throw new InvalidOperationException("Recovery parcel does not match its owned specimen: " + specimen.Id);
                return existing;
            }
            string id = "REC-A1-" + specimen.Id;
            int suffix = 1;
            while (state.FindCrate(id) != null) id = "REC-A1-" + specimen.Id + "-" + suffix++;
            var crate = new CrateRecord { Id = id, Seed = specimen.Seed, SupplierId = specimen.SupplierId,
                Recovery = true, Locality = "Recovered stock", Rotation = Quaternion.identity };
            crate.SpecimenIds.Add(specimen.Id);
            state.Crates.Add(crate);
            specimen.RecoveryCrateId = id;
            specimen.WorldPosition = Vector3.zero;
            return crate;
        }

        /// <summary>Old crate coordinates cannot be reused under new walls. Contents repack when their parcel arrives.</summary>
        public static void QueueOldCrates(GameState state)
        {
            foreach (var crate in state.Crates)
            {
                crate.Delivered = false;
                crate.Position = Vector3.zero;
                crate.Rotation = Quaternion.identity;
                foreach (string id in crate.SpecimenIds)
                {
                    var specimen = state.FindSpecimen(id);
                    if (specimen != null && specimen.IsInside(crate)) specimen.WorldPosition = Vector3.zero;
                }
            }
        }
    }
}
