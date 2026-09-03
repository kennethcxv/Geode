using System;
using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Save
{
    public enum SpecimenLocation
    {
        InCrate = 0, World = 1, Held = 2, Bench = 3, SellTray = 4, AppraisalStation = 5, DisplaySlot = 6, Sold = 7, Discarded = 8,
        /// <summary>On a retail sales fixture; customers may buy it. LocationIndex is the sale slot.</summary>
        SaleSlot = 9,
        /// <summary>In the wash tub being scrubbed.</summary>
        WashTub = 10,
        /// <summary>Sawn into pieces: the record stays as the lineage's parent, nothing spawns for it.</summary>
        Cut = 11,
        /// <summary>Clamped in the saw (LocationIndex unused).</summary>
        Saw = 12,
        /// <summary>On the polishing lap.</summary>
        Lap = 13,
        /// <summary>On the Stage-2 rock rack (material storage). LocationIndex is the occupant index.</summary>
        Rack = 14,
        /// <summary>On the geode cracker's rails (Stage 2).</summary>
        Cracker = 15,
    }

    /// <summary>Career state of one specimen. Geology is regenerated from Seed; nothing here rerolls it.</summary>
    /// <summary>One line of a specimen's life: what happened, when, and a number where one applies (a price, a value).</summary>
    [Serializable]
    public sealed class SpecimenEvent
    {
        public string Kind;      // acquired, washed, opened, cut, polished, appraised, displayed, listed, sold, dealer, rinsed, predicted
        public long Ticks;
        public float Value;
        public string Note;
    }

    [Serializable]
    public sealed class SpecimenRecord
    {
        public string Id;
        // V5 provenance: where and when it came in, what it weighed whole, what the player called before opening it
        public string Locality = "";
        public long AcquiredAtTicks;
        public float AcquisitionCost;
        public float OriginalMassKg;
        public bool Predicted;
        public bool PredictedHollow;
        public int PredictedTier = -1;
        public List<SpecimenEvent> History = new List<SpecimenEvent>();
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
        /// <summary>Retail asking price while on a sale slot (0 = not for sale).</summary>
        public float AskingPrice;
        public string CustomName;
        public long OpenedAtTicks;
        public long DiscoveredAtTicks;
        // processing commit state
        public bool ProcessingStarted;
        public float[] SectorStress = Array.Empty<float>();
        /// <summary>Chisel marks on the shell (longitude fraction, signed latitude fraction, radius m, strength), newest last.</summary>
        public List<Vector4> Impacts = new List<Vector4>();
        public int StrikeCount;
        public int DamageEvents;
        public float ShellDamage;
        /// <summary>Crystal damage fraction as the appraisal sees it, kept current by the bench so value can be estimated without geometry.</summary>
        public float DamageFraction;
        // opened layout: where the flipped top half rests relative to the bottom half (set when the specimen leaves the bench)
        public bool HasOpenPose;
        public Vector3 OpenTopLocalPos;
        public Quaternion OpenTopLocalRot = Quaternion.identity;
        // saw lineage: a piece shares its parent's seed (the geology) and carries the planes that bound it
        public bool IsPiece;
        public PieceShape Piece;
        public string ParentId;
        public int CutIndex;
        /// <summary>What the cut exposed, captured when the piece was made so value never needs the geometry.</summary>
        public float PieceRetained = 1f, PieceOpening, PieceSymmetry = 1f, PieceFaceArea;
        /// <summary>0..1 finish on the cut face (only pieces can be polished).</summary>
        public float Polish;
        /// <summary>Which tool opened it, for the records ("hammer", "saw").</summary>
        public string ProcessedBy = "";
        // a cut in progress: the committed plane and how far the blade has gone (0..1), so a reload resumes, never rerolls
        public bool CutCommitted;
        public Vector3 CutNormal;
        public float CutHeight, CutProgress;
        public float CutYaw, CutRoll, CutOffset;
        // V5 saw: which blade profile was on for the cut, and any step the face picked up (the rock shifting in the jaws)
        public bool CutThin;
        public float CutFaceStep;

        [NonSerialized] private SpecimenGeology _geology;
        public SpecimenGeology Geology => _geology ??= SpecimenGenerator.Generate(Seed);

        public string DisplayName => !string.IsNullOrEmpty(CustomName) ? CustomName : IsPiece ? Valuation.PieceName(Geology, Piece, Polish, PieceOpening) : Valuation.DescriptiveName(Geology);
        public bool IsOpened => Condition != null && Condition.Opened;

        /// <summary>Appraised value when known, otherwise the damaged value the dealer would see (never the pristine base value).</summary>
        public float EstimatedValue() => Appraised && AppraisedValue > 0f ? AppraisedValue : PristineForSale();

        /// <summary>The value a fresh appraisal would give right now.</summary>
        public float PristineForSale() => IsPiece
            ? Valuation.PieceValue(Geology, Piece, PieceRetained, PieceOpening, PieceSymmetry, PieceFaceArea, Polish, DamageFraction, ShellDamage)
            : Valuation.DamagedValue(Geology, DamageFraction, ShellDamage);
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
        public string Locality = "";
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
        public int DealerAdvances;
        // retail
        public int RetailSales;
        public float RetailRevenue;
        public float BiggestRetailSale;
        public string BiggestRetailSaleName;
        public int CustomersServed;
        public int CustomersLeftEmptyHanded;
        // processing (V4)
        public int SawCuts;
        public int SlabsCut;
        public int PiecesPolished;
        public float BladeWearSpent;
        public float HighestValueSawResult;
        public string HighestValueSawResultName;
        public float HighestValueHammerResult;
        public string HighestValueHammerResultName;
        public float BestPolishedValue;
        public string BestPolishedName;
        public float LargestSlabFaceCm2;
        public string LargestSlabName;
        public int RocksWashed;
        // V5 mastery: calls made before opening, and how many were right
        public int PredictionsMade, HollowCallsRight, TierCallsRight;
        public int RocksCracked;
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
        public int SaleCapacity = 6;
        public bool PremiumInviteShown;
        public bool SliceTeaseShown;
        public int Prestige;
        /// <summary>Workshop stage: 0 = the V3 garage, 1 = the Stage-2 lapidary expansion.</summary>
        public int WorkshopStage;
        /// <summary>Diamond blade wear 0..1; a worn blade cuts slowly and chips, a new one is bought on the tablet.</summary>
        public float BladeWear;

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

        /// <summary>Append a line to a specimen's history (kept short: the last 40 events).</summary>
        public static void Log(SpecimenRecord r, string kind, float value = 0f, string note = null)
        {
            if (r == null) return;
            r.History ??= new List<SpecimenEvent>();
            r.History.Add(new SpecimenEvent { Kind = kind, Ticks = DateTime.UtcNow.Ticks, Value = value, Note = note ?? "" });
            while (r.History.Count > 40) r.History.RemoveAt(0);
        }
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

        public int ForSaleCount()
        {
            int c = 0;
            foreach (var s in Specimens) if (s.Location == SpecimenLocation.SaleSlot) c++;
            return c;
        }

        public float CollectionValue()
        {
            float v = 0f;
            foreach (var s in Specimens) if (s.Location == SpecimenLocation.DisplaySlot) v += s.EstimatedValue();
            return v;
        }
    }
}
