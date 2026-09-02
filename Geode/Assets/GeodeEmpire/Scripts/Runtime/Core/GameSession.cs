using System;
using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Interaction;
using GeodeEmpire.Player;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;
using GeodeEmpire.Workshop;

namespace GeodeEmpire.Core
{
    public enum SessionStartMode { Auto, NewGame, Continue }

    /// <summary>
    /// Owns the career state for the workshop scene: spawning specimens/crates, cash, autosave, world rebuild.
    /// Stations talk to it; UI listens to it.
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }
        public static SessionStartMode PendingStart = SessionStartMode.Auto;

        public const float StartingCash = 120f;

        public GameState State { get; private set; }
        public bool IsLoaded { get; private set; }
        public SpecimenAssetLibrary Library { get; private set; }
        public PlayerInteractor Player { get; private set; }
        public FirstPersonController Controller { get; private set; }

        public event Action<float, float> CashChanged;       // (newCash, delta)
        public event Action StateChanged;
        public event Action<string, NotificationKind> Notified;
        public event Action<SpecimenEntity> SpecimenSpawned;
        public event Action<SpecimenEntity> SpecimenDespawned;
        public event Action Loaded;

        private readonly Dictionary<string, SpecimenEntity> _entities = new Dictionary<string, SpecimenEntity>();
        private readonly Dictionary<string, CrateEntity> _crates = new Dictionary<string, CrateEntity>();
        private float _lastSaveTime = -10f;
        private bool _saveQueued;
        private string _saveReason;

        public IReadOnlyDictionary<string, SpecimenEntity> Entities => _entities;
        public IReadOnlyDictionary<string, CrateEntity> Crates => _crates;
        public float Cash => State != null ? State.Cash : 0f;

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            Library = SpecimenAssetLibrary.Load();
            if (Library == null) Debug.LogError("[GameSession] SpecimenAssetLibrary missing from Resources.");
            GameInput.Ensure();
            GameSettings.Current.Apply();
            Player = FindAnyObjectByType<PlayerInteractor>();
            Controller = FindAnyObjectByType<FirstPersonController>();
        }

        private void Start()
        {
            var mode = PendingStart;
            PendingStart = SessionStartMode.Auto;
            if (mode == SessionStartMode.NewGame || (mode == SessionStartMode.Auto && !SaveSystem.Exists()))
                NewGame();
            else
                ContinueGame();
        }

        private void Update()
        {
            if (State == null) return;
            State.Stats.PlayTimeSeconds += Time.deltaTime;
            if (_saveQueued && Time.unscaledTime - _lastSaveTime > 0.75f) FlushSave();
        }

        private void OnApplicationQuit()
        {
            if (State != null) FlushSave("quit");
        }

        // ------------------------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------------------------
        public void NewGame()
        {
            ClearWorld();
            State = new GameState
            {
                SaveId = Guid.NewGuid().ToString("N"),
                WorldSeed = (ulong)DateTime.UtcNow.Ticks ^ 0x5DEECE66DUL,
                CreatedTicks = DateTime.UtcNow.Ticks,
                Cash = StartingCash,
            };
            State.UnlockedSuppliers.Add("local");
            IsLoaded = true;
            PlacePlayerAtStart();
            CursorController.Reset();
            Loaded?.Invoke();
            StateChanged?.Invoke();
            FlushSave("new-game");
        }

        public void ContinueGame()
        {
            ClearWorld();
            State = SaveSystem.Load();
            if (State == null)
            {
                Debug.LogWarning("[GameSession] no readable save; starting new game");
                NewGame();
                return;
            }
            IsLoaded = true;
            RebuildWorld();
            PlacePlayerAtStart();
            CursorController.Reset();
            Loaded?.Invoke();
            StateChanged?.Invoke();
        }

        private void PlacePlayerAtStart()
        {
            var start = FindAnyObjectByType<PlayerStart>();
            if (start != null && Controller != null)
            {
                Controller.Teleport(start.transform.position + Vector3.up * 0.08f, start.transform.eulerAngles.y);
                Controller.SpawnPoint = start.transform.position + Vector3.up * 0.08f;
                Controller.SpawnYaw = start.transform.eulerAngles.y;
            }
        }

        private void ClearWorld()
        {
            foreach (var e in _entities.Values) if (e != null) Destroy(e.gameObject);
            _entities.Clear();
            foreach (var c in _crates.Values) if (c != null) Destroy(c.gameObject);
            _crates.Clear();
            foreach (var z in FindObjectsByType<PlacementZone>(FindObjectsInactive.Include)) z.Occupants.Clear();
        }

        /// <summary>Recreate every physical object from the save. Never rerolls anything.</summary>
        private void RebuildWorld()
        {
            var zones = FindObjectsByType<PlacementZone>(FindObjectsInactive.Include);
            foreach (var cr in State.Crates)
            {
                if (cr.Delivered && !cr.Opened || (cr.Opened && HasRemainingRocks(cr)))
                {
                    var ce = CrateEntity.Create(cr, this);
                    ce.transform.SetPositionAndRotation(cr.Position, cr.Rotation);
                    _crates[cr.Id] = ce;
                }
            }
            foreach (var r in State.Specimens)
            {
                switch (r.Location)
                {
                    case SpecimenLocation.Sold:
                    case SpecimenLocation.Discarded:
                        continue;
                    case SpecimenLocation.InCrate:
                    {
                        if (_crates.TryGetValue(r.CrateId ?? "", out var ce)) ce.RestoreRock(r);
                        else Spawn(r, r.WorldPosition, r.WorldRotation, true);
                        break;
                    }
                    case SpecimenLocation.Held:
                    case SpecimenLocation.World:
                    {
                        var pos = r.WorldPosition;
                        if (r.Location == SpecimenLocation.Held && Controller != null) pos = Controller.transform.position + Controller.transform.forward * 0.6f + Vector3.up * 1.0f;
                        Spawn(r, pos, r.WorldRotation, true);
                        break;
                    }
                    default:
                    {
                        var e = Spawn(r, r.WorldPosition, r.WorldRotation, false);
                        var zone = FindZone(zones, r.Location, r.LocationIndex);
                        if (zone != null) zone.Place(e, true);
                        else { e.SetPhysics(true); r.Location = SpecimenLocation.World; }
                        break;
                    }
                }
            }
        }

        private static bool HasRemainingRocks(CrateRecord cr)
        {
            var s = Instance.State;
            foreach (var id in cr.SpecimenIds)
            {
                var r = s.FindSpecimen(id);
                if (r != null && r.Location == SpecimenLocation.InCrate) return true;
            }
            return false;
        }

        private static PlacementZone FindZone(PlacementZone[] zones, SpecimenLocation loc, int index)
        {
            PlacementZone best = null;
            foreach (var z in zones)
            {
                if (z.LocationFor() != loc) continue;
                if (loc == SpecimenLocation.DisplaySlot) { if (z.SlotIndex == index) return z; continue; }
                if (best == null || (!z.IsFull && best.IsFull)) best = z;
            }
            return best;
        }

        // ------------------------------------------------------------------------------------
        // Specimens & crates
        // ------------------------------------------------------------------------------------
        public SpecimenRecord CreateSpecimenRecord(ulong seed, string supplierId, string crateId)
        {
            State.SpecimenCounter++;
            var r = new SpecimenRecord
            {
                Id = $"S{State.SpecimenCounter:D4}-{(seed & 0xFFFF):X4}",
                Seed = seed,
                SupplierId = supplierId,
                CrateId = crateId,
                Location = SpecimenLocation.InCrate,
                Condition = new SpecimenCondition(),
                DiscoveredAtTicks = DateTime.UtcNow.Ticks,
            };
            State.Specimens.Add(r);
            return r;
        }

        public SpecimenEntity Spawn(SpecimenRecord r, Vector3 position, Quaternion rotation, bool physics)
        {
            if (_entities.TryGetValue(r.Id, out var existing) && existing != null) return existing;
            var e = SpecimenEntity.Create(r, Library);
            e.SetPose(position, rotation);
            e.SetPhysics(physics);
            _entities[r.Id] = e;
            SpecimenSpawned?.Invoke(e);
            return e;
        }

        public void Despawn(SpecimenEntity e)
        {
            if (e == null) return;
            _entities.Remove(e.Id);
            if (e.Zone != null) e.Zone.Take(e, true);
            SpecimenDespawned?.Invoke(e);
            Destroy(e.gameObject);
        }

        public SpecimenEntity GetEntity(string id) => _entities.TryGetValue(id, out var e) ? e : null;

        public void RegisterCrate(CrateEntity c) => _crates[c.Record.Id] = c;
        public void UnregisterCrate(CrateEntity c) => _crates.Remove(c.Record.Id);

        // ------------------------------------------------------------------------------------
        // Economy
        // ------------------------------------------------------------------------------------
        public bool CanAfford(float amount) => State != null && State.Cash + 0.001f >= amount;

        public bool TrySpend(float amount, string reason)
        {
            if (!CanAfford(amount)) return false;
            State.Cash -= amount;
            State.Stats.MoneySpent += amount;
            CashChanged?.Invoke(State.Cash, -amount);
            QueueSave(reason);
            return true;
        }

        public void AddCash(float amount, string reason)
        {
            State.Cash += amount;
            State.Stats.MoneyEarned += amount;
            CashChanged?.Invoke(State.Cash, amount);
            QueueSave(reason);
        }

        public void Notify(string text, NotificationKind kind = NotificationKind.Info)
        {
            Notified?.Invoke(text, kind);
        }

        public void RaiseStateChanged() => StateChanged?.Invoke();

        /// <summary>Encyclopedia + statistics + callouts when a specimen is opened.</summary>
        public void RecordDiscovery(SpecimenRecord r, float damageFraction)
        {
            var g = r.Geology;
            var fam = g.Family;
            var entry = State.GetOrCreateEntry(g.Mineral);
            bool firstOfFamily = entry.Found == 0;
            entry.Found++;
            if (firstOfFamily) entry.FirstFoundTicks = DateTime.UtcNow.Ticks;
            float value = Valuation.DamagedValue(g, damageFraction, r.ShellDamage);
            bool record = value > entry.BestValue;
            if (record) { entry.BestValue = value; entry.BestSpecimenId = r.Id; }
            bool largest = g.MassKg > entry.LargestMassKg;
            if (largest) entry.LargestMassKg = g.MassKg;
            foreach (var t in g.Traits) { string n = t.ToString(); if (!entry.TraitsSeen.Contains(n)) entry.TraitsSeen.Add(n); }
            string cav = g.Cavity.ToString();
            if (!entry.CavitiesSeen.Contains(cav)) entry.CavitiesSeen.Add(cav);

            var st = State.Stats;
            st.SpecimensOpened++;
            if (damageFraction > 0.001f) { if (damageFraction > st.MostDamagedFraction) { st.MostDamagedFraction = damageFraction; st.MostDamagedName = r.DisplayName; } }
            else st.CleanOpens++;
            if (g.MassKg > st.LargestSpecimenKg) { st.LargestSpecimenKg = g.MassKg; st.LargestSpecimenName = r.DisplayName; }

            if (firstOfFamily) Notify($"New mineral discovered: {fam.Name}", NotificationKind.Discovery);
            if (g.Tier >= QualityTier.Exceptional) Notify($"{Valuation.TierLabel(g.Tier)} find: {r.DisplayName}", NotificationKind.Discovery);
            else if (record && entry.Found > 1) Notify($"Best {fam.Name} so far", NotificationKind.Info);
            StateChanged?.Invoke();
        }

        // ------------------------------------------------------------------------------------
        // Purchases
        // ------------------------------------------------------------------------------------
        public bool BuyCrate(string supplierId, out string error)
        {
            error = null;
            var sup = Economy.SupplierCatalog.Get(supplierId);
            if (sup == null) { error = "Unknown supplier"; return false; }
            if (!State.HasSupplier(supplierId)) { error = "Supplier not unlocked"; return false; }
            if (!CanAfford(sup.Price)) { error = "Not enough cash"; return false; }
            var receiving = FindAnyObjectByType<ReceivingArea>();
            if (receiving == null) { error = "No receiving area"; return false; }
            if (_crates.Count >= 4) { error = "The receiving pallet is full. Open or break down a crate first."; return false; }
            TrySpend(sup.Price, "crate");
            var crate = Economy.CrateGenerator.Generate(State, sup, CreateSpecimenRecord);
            State.Stats.CratesPurchased++;
            receiving.Deliver(crate);
            Audio.WorkshopAudio.Play2D("ui_buy", 0.7f);
            Notify($"{sup.Name} ordered. Delivery at the pallet.", NotificationKind.Success);
            Tutorial.Notify("crate_bought");
            if (State.CrateCounter >= 2) Tutorial.Notify("upgrade_or_crate");
            StateChanged?.Invoke();
            FlushSave("crate-bought");
            return true;
        }

        public bool BuyUpgrade(string upgradeId, out string error)
        {
            error = null;
            var up = Economy.UpgradeCatalog.Get(upgradeId);
            if (up == null) { error = "Unknown upgrade"; return false; }
            if (State.HasUpgrade(upgradeId)) { error = "Already owned"; return false; }
            if (!CanAfford(up.Price)) { error = "Not enough cash"; return false; }
            TrySpend(up.Price, "upgrade");
            State.Upgrades.Add(upgradeId);
            if (upgradeId == Economy.UpgradeCatalog.DisplayExpansion) State.DisplayCapacity = 12;
            Audio.WorkshopAudio.Play2D("ui_buy", 0.7f);
            Notify($"{up.Name} installed.", NotificationKind.Success);
            Tutorial.Notify("upgrade_or_crate");
            StateChanged?.Invoke();
            FlushSave("upgrade");
            return true;
        }

        // ------------------------------------------------------------------------------------
        // Persistence
        // ------------------------------------------------------------------------------------
        /// <summary>Coalesced autosave; use FlushSave for commits that must hit disk now.</summary>
        public void QueueSave(string reason)
        {
            _saveQueued = true;
            _saveReason = reason;
        }

        public void FlushSave(string reason = null)
        {
            if (State == null) return;
            _saveQueued = false;
            _lastSaveTime = Time.unscaledTime;
            foreach (var e in _entities.Values)
                if (e != null && e.Record.Location == SpecimenLocation.World) e.SyncRecordTransform();
            SaveSystem.Save(State);
        }
    }

    public enum NotificationKind { Info, Success, Warning, Discovery }
}
