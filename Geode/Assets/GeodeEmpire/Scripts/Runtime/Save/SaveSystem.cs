using System;
using System.IO;
using UnityEngine;

namespace GeodeEmpire.Save
{
    /// <summary>
    /// Atomic JSON persistence with a rolling backup. Write to .tmp, keep previous as .bak, then move into place,
    /// so a crash mid-write never destroys a career.
    /// </summary>
    public static class SaveSystem
    {
        public const string FileName = "geode_career.json";

        /// <summary>Tests point this at a scratch folder so they never touch the player's real career.</summary>
        public static string DirectoryOverride;
        public static string Directory => string.IsNullOrEmpty(DirectoryOverride) ? Application.persistentDataPath : DirectoryOverride;
        public static string MainPath => Path.Combine(Directory, FileName);
        public static string BackupPath => MainPath + ".bak";
        public static string TempPath => MainPath + ".tmp";

        /// <summary>Optional editor automation boundary, invoked before any career/settings filesystem mutation.</summary>
        public static Action<string> BeforeDirectoryWrite;

        public static event Action<GameState> Saved;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Saved = null; }

        public static bool Exists() => File.Exists(MainPath) || File.Exists(BackupPath);

        public static void Save(GameState state)
        {
            if (state == null) return;
            state.LastSavedTicks = DateTime.UtcNow.Ticks;
            state.Version = GameState.CurrentVersion;
            string json = JsonUtility.ToJson(state, false);
            try
            {
                BeforeDirectoryWrite?.Invoke(Directory);
                System.IO.Directory.CreateDirectory(Directory);
                File.WriteAllText(TempPath, json);
                // sanity: re-read what we wrote before replacing the live save
                var check = JsonUtility.FromJson<GameState>(File.ReadAllText(TempPath));
                if (check == null || check.SaveId != state.SaveId) throw new IOException("save verification failed");
                if (File.Exists(MainPath))
                {
                    if (File.Exists(BackupPath)) File.Delete(BackupPath);
                    File.Move(MainPath, BackupPath);
                }
                File.Move(TempPath, MainPath);
                Saved?.Invoke(state);
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveSystem] save failed: " + e.Message);
            }
        }

        public static GameState Load()
        {
            // The swap is two renames; a crash between them leaves a verified, newer .tmp beside an older .bak.
            // Take whichever readable file is newest, and promote it if it is not the main file.
            var main = TryLoad(MainPath);
            var temp = TryLoad(TempPath);
            var back = TryLoad(BackupPath);
            GameState best = main; string bestPath = MainPath;
            if (temp != null && (best == null || temp.LastSavedTicks > best.LastSavedTicks)) { best = temp; bestPath = TempPath; }
            if (back != null && (best == null || back.LastSavedTicks > best.LastSavedTicks)) { best = back; bestPath = BackupPath; }
            if (best == null) return null;
            if (bestPath != MainPath)
            {
                Debug.LogWarning($"[SaveSystem] main save {(main == null ? "unreadable" : "older")}, restored from {Path.GetFileName(bestPath)}");
                try { BeforeDirectoryWrite?.Invoke(Directory); File.Copy(bestPath, MainPath, true); } catch (Exception e) { Debug.LogWarning("[SaveSystem] could not promote recovered save: " + e.Message); }
            }
            return best;
        }

        private static GameState TryLoad(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                return Parse(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] could not read {path}: {e.Message}");
                return null;
            }
        }

        /// <summary>A save file's text to a migrated state (null when it is not a save at all). The loader and the migration tests share it.</summary>
        public static GameState Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var s = JsonUtility.FromJson<GameState>(json);
            if (s == null || string.IsNullOrEmpty(s.SaveId)) return null;
            Migrate(s);
            return s;
        }

        /// <summary>
        /// Older files come forward one version at a time. Version 1 is the V4 career (pieces, cut lineage, Stage 2);
        /// version 2 (V5) adds provenance, calls, history, favourites, certification, the market, auctions and the exhibition:
        /// every new field gets its quiet default so a V4 save keeps playing without a seam.
        /// </summary>
        private static void Migrate(GameState s)
        {
            if (s.Version < 1) s.Version = 1;
            if (s.Version < 3 && s.DisplayCapacity <= 0) s.DisplayCapacity = 8;
            s.Upgrades ??= new System.Collections.Generic.List<string>();
            s.UnlockedSuppliers ??= new System.Collections.Generic.List<string>();
            s.TutorialSteps ??= new System.Collections.Generic.List<string>();
            s.Encyclopedia ??= new System.Collections.Generic.List<EncyclopediaEntry>();
            s.Crates ??= new System.Collections.Generic.List<CrateRecord>();
            s.Fixtures ??= new System.Collections.Generic.List<FixturePose>();
            s.Specimens ??= new System.Collections.Generic.List<SpecimenRecord>();
            s.Stats ??= new Statistics();
            // V5 (version 2): market, auctions, letters, exhibition
            s.OfferedLots ??= new System.Collections.Generic.List<string>();
            s.Commissions ??= new System.Collections.Generic.List<Commission>();
            s.AuctionLots ??= new System.Collections.Generic.List<AuctionLot>();
            s.PendingLetters ??= new System.Collections.Generic.List<LetterRecord>();
            s.ExhibitedIds ??= new System.Collections.Generic.List<string>();
            foreach (var r in s.Specimens)
            {
                r.Condition ??= new GeodeEmpire.Specimens.SpecimenCondition();
                r.SectorStress ??= Array.Empty<float>();
                r.Impacts ??= new System.Collections.Generic.List<Vector4>();
                r.History ??= new System.Collections.Generic.List<SpecimenEvent>();
                r.ProcessedBy ??= "";
                if (s.Version < 2)
                {
                    // a V4 rock came from somewhere: its source's first named locality
                    if (string.IsNullOrEmpty(r.Locality) && !string.IsNullOrEmpty(r.SupplierId)) r.Locality = GeodeEmpire.Economy.CrateGenerator.DefaultLocalities(r.SupplierId)[0];
                    if (r.AcquiredAtTicks == 0) r.AcquiredAtTicks = r.DiscoveredAtTicks > 0 ? r.DiscoveredAtTicks : s.CreatedTicks;
                    if (r.OriginalMassKg <= 0f && r.Geology != null) r.OriginalMassKg = r.Geology.MassKg;
                }
            }
            foreach (var c in s.Crates) if (c != null && string.IsNullOrEmpty(c.Locality) && !string.IsNullOrEmpty(c.SupplierId)) c.Locality = GeodeEmpire.Economy.CrateGenerator.DefaultLocalities(c.SupplierId)[0];
            // version 3: operating costs, spatial cleaning and surface observations. §23: an existing career must
            // load owing nothing, with a sensible next due date, and with its rocks exactly as clean as they were.
            s.Bills ??= new BillingState();
            s.Bills.LastLines ??= new System.Collections.Generic.List<string>();
            if (s.Version < 3)
            {
                s.Bills.Outstanding = 0f;
                s.Bills.LateFees = 0f;
                s.Bills.MissedPayments = 0;
                s.Bills.ElectricityUnits = 0f;
                s.Bills.WaterLitres = 0f;
                // the first bill lands a full period from wherever the career has got to, never immediately
                int today = 1 + (int)(s.Stats.PlayTimeSeconds / 1200f);
                s.Bills.NextBillDay = Mathf.Max(today + GeodeEmpire.Economy.Ledger.PeriodDays,
                                                GeodeEmpire.Economy.Ledger.FirstBillDay);
                s.Bills.DueDay = s.Bills.NextBillDay + 1;
            }
            foreach (var r in s.Specimens)
            {
                if (r.Condition == null) continue;
                r.Condition.RegionClean ??= Array.Empty<byte>();
                r.Condition.ClueState ??= Array.Empty<byte>();
                // a migrated rock keeps its old whole-rock cleanliness: SpecimenCondition.CleanAt falls back to
                // Cleaned while RegionClean is empty, so nothing suddenly becomes filthy or spotless
            }
            // Version 4 adds player-controlled opening hours. Old careers resume closed so the owner can
            // inspect their shop before inviting new customers; existing stock, money and fixtures are retained.
            if (s.Version < 4) s.ShopOpen = false;
            s.Version = GameState.CurrentVersion;
        }

        public static void Delete()
        {
            try
            {
                BeforeDirectoryWrite?.Invoke(Directory);
                if (File.Exists(MainPath)) File.Delete(MainPath);
                if (File.Exists(BackupPath)) File.Delete(BackupPath);
                if (File.Exists(TempPath)) File.Delete(TempPath);
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveSystem] delete failed: " + e.Message);
            }
        }
    }
}
