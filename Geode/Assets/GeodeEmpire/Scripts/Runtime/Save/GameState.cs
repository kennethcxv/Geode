using System;
using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Save
{
    public enum SpecimenLocation
    {
        InCrate = 0, World = 1, Held = 2, Bench = 3, SellTray = 4, AppraisalStation = 5, DisplaySlot = 6, Sold = 7, Discarded = 8,
    }

    /// <summary>Career state of one specimen. Geology is regenerated from Seed; nothing here rerolls it.</summary>
    [Serializable]
    public sealed class SpecimenRecord
    {
        public string Id;
        public ulong Seed;
        public string SupplierId;
        public string CrateId;
        public SpecimenCondition Condition = new SpecimenCondition();
        public SpecimenLocation Location;
        public int LocationIndex;
        public Vector3 WorldPosition;
        public Quaternion WorldRotation = Quaternion.identity;
        public bool Appraised;
        public float AppraisedValue;
        public string CustomName;
        public long OpenedAtTicks;
        public long DiscoveredAtTicks;
        // processing commit state
        public bool ProcessingStarted;
        public float[] SectorStress = Array.Empty<float>();
        public int StrikeCount;
        public int DamageEvents;
        public float ShellDamage;

        [NonSerialized] private SpecimenGeology _geology;
        public SpecimenGeology Geology => _geology ??= SpecimenGenerator.Generate(Seed);

        public string DisplayName => !string.IsNullOrEmpty(CustomName) ? CustomName : Valuation.DescriptiveName(Geology);
        public bool IsOpened => Condition != null && Condition.Opened;
    }

    [Serializable]
    public sealed class CrateRecord
    {
        public string Id;
        public string SupplierId;
        public ulong Seed;
        public bool Opened;
        public bool Delivered;
        public Vector3 Position;
        public Quaternion Rotation = Quaternion.identity;
        public List<string> SpecimenIds = new List<string>();
        public float PricePaid;
    }

    [Serializable]
    public sealed class EncyclopediaEntry
    {
        public MineralId Mineral;
        public int Found;
        public float BestValue;
        public string BestSpecimenId;
        public float LargestMassKg;
        public long FirstFoundTicks;
        public List<string> TraitsSeen = new List<string>();
        public List<string> CavitiesSeen = new List<string>();
    }

    [Serializable]
    public sealed class Statistics
    {
        public int CratesPurchased;
        public int RocksProcessed;
        public int SpecimensOpened;
        public float MoneySpent;
        public float MoneyEarned;
        public float BiggestSale;
        public string BiggestSaleName;
        public float BiggestCrateLoss;
        public float HighestValueKept;
        public string HighestValueKeptName;
        public float LargestSpecimenKg;
        public string LargestSpecimenName;
        public float MostDamagedFraction;
        public string MostDamagedName;
        public int SpecimensKept;
        public int SpecimensSold;
        public int SpecimensDamaged;
        public int TotalStrikes;
        public int CleanOpens;
        public float PlayTimeSeconds;
    }

    /// <summary>Whole career save. Versioned; new fields get sensible defaults on load.</summary>
    [Serializable]
    public sealed class GameState
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public string SaveId;
        public ulong WorldSeed;
        public long CreatedTicks;
        public long LastSavedTicks;
        public float Cash;
        public int CrateCounter;
        public int SpecimenCounter;
        public List<SpecimenRecord> Specimens = new List<SpecimenRecord>();
        public List<CrateRecord> Crates = new List<CrateRecord>();
        public List<string> Upgrades = new List<string>();
        public List<string> UnlockedSuppliers = new List<string>();
        public List<string> TutorialSteps = new List<string>();
        public List<EncyclopediaEntry> Encyclopedia = new List<EncyclopediaEntry>();
        public Statistics Stats = new Statistics();
        public int DisplayCapacity = 8;
        public bool PremiumInviteShown;
        public bool SliceTeaseShown;
        public int Prestige;

        public SpecimenRecord FindSpecimen(string id)
        {
            for (int i = 0; i < Specimens.Count; i++) if (Specimens[i].Id == id) return Specimens[i];
            return null;
        }

        public CrateRecord FindCrate(string id)
        {
            for (int i = 0; i < Crates.Count; i++) if (Crates[i].Id == id) return Crates[i];
            return null;
        }

        public bool HasUpgrade(string id) => Upgrades.Contains(id);
        public bool HasSupplier(string id) => UnlockedSuppliers.Contains(id);
        public bool TutorialDone(string step) => TutorialSteps.Contains(step);

        public EncyclopediaEntry GetOrCreateEntry(MineralId mineral)
        {
            foreach (var e in Encyclopedia) if (e.Mineral == mineral) return e;
            var n = new EncyclopediaEntry { Mineral = mineral };
            Encyclopedia.Add(n);
            return n;
        }

        public int DisplayedCount()
        {
            int c = 0;
            foreach (var s in Specimens) if (s.Location == SpecimenLocation.DisplaySlot) c++;
            return c;
        }

        public float CollectionValue()
        {
            float v = 0f;
            foreach (var s in Specimens) if (s.Location == SpecimenLocation.DisplaySlot) v += s.AppraisedValue > 0 ? s.AppraisedValue : s.Geology.BaseValue;
            return v;
        }
    }
}
