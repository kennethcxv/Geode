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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; PendingStart = SessionStartMode.Auto; }

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
        /// <summary>A piece worth stopping for came out of a rock: a first of its family, or an exceptional grade.</summary>
        public event Action<SpecimenRecord, string> Discovered;

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

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
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
            DisplayCabinet.RecomputePrestige(State);
            PlacePlayerAtStart();
            CursorController.Reset();
            Loaded?.Invoke();
            StateChanged?.Invoke();
            CheckSolvency();
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
            // every delivered crate comes back, opened or not; an emptied crate stays until the player breaks it down
            State.Crates.RemoveAll(cr => !cr.Delivered);
            foreach (var cr in State.Crates)
            {
                var ce = CrateEntity.Create(cr, this);
                ce.transform.SetPositionAndRotation(cr.Position, cr.Rotation);
                _crates[cr.Id] = ce;
            }
            foreach (var r in State.Specimens)
            {
                switch (r.Location)
                {
                    case SpecimenLocation.Sold:
                    case SpecimenLocation.Discarded:
                    case SpecimenLocation.Cut:
                    case SpecimenLocation.Consigned:   // with the auction house: nothing in the world until the hammer
                        continue;
                    case SpecimenLocation.InCrate:
                    {
                        // rocks of a closed crate have no pose yet: they are laid out when the crate is opened
                        if (_crates.TryGetValue(r.CrateId ?? "", out var ce)) { if (ce.IsOpened) ce.RestoreRock(r); }
                        else { r.Location = SpecimenLocation.World; Spawn(r, r.WorldPosition, r.WorldRotation, true); }
                        break;
                    }
                    case SpecimenLocation.Held:
                    case SpecimenLocation.World:
                    {
                        var pos = r.WorldPosition;
                        if (r.Location == SpecimenLocation.Held)
                        {
                            // it was in the player's hands: set it down in front of them as a normal loose rock
                            if (Controller != null) pos = Controller.transform.position + Controller.transform.forward * 0.6f + Vector3.up * 1.0f;
                            r.Location = SpecimenLocation.World;
                            r.WorldPosition = pos;
                        }
                        Spawn(r, pos, r.WorldRotation, true);
                        break;
                    }
                    default:
                    {
                        var e = Spawn(r, r.WorldPosition, r.WorldRotation, false);
                        var zone = FindZone(zones, r.Location, r.LocationIndex);
                        if (zone != null) zone.Place(e, true);
                        else { e.SetPhysics(true); r.Location = SpecimenLocation.World; r.AskingPrice = 0f; }
                        if (r.Location == SpecimenLocation.SaleSlot && r.AskingPrice <= 0f) r.AskingPrice = Retail.RetailShop.AskingPrice(r);
                        break;
                    }
                }
            }
            foreach (var ce in _crates.Values) ce.FinishRestore();
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
            PlacementZone spare = null;
            foreach (var z in zones)
            {
                if (z.LocationFor() != loc) continue;
                if (loc == SpecimenLocation.DisplaySlot || loc == SpecimenLocation.SaleSlot)
                {
                    if (z.SlotIndex == index && !z.IsFull) return z;
                    if (spare == null && !z.IsFull && !z.Locked) spare = z;   // the slot is taken (older save): any free one of the same kind
                    continue;
                }
                if (best == null || (!z.IsFull && best.IsFull)) best = z;
            }
            return best ?? spare;
        }

        // ------------------------------------------------------------------------------------
        // Specimens & crates
        // ------------------------------------------------------------------------------------
        public SpecimenRecord CreateSpecimenRecord(ulong seed, string supplierId, string crateId)
        {
            State.SpecimenCounter++;
            // a dealer who scrubs the rough before shipping sends it part-cleaned; quarry crates arrive caked
            var sup = Economy.SupplierCatalog.Get(supplierId);
            float cleaned = sup != null ? Mathf.Clamp01(1f - sup.DirtScale) : 0f;
            var r = new SpecimenRecord
            {
                Id = $"S{State.SpecimenCounter:D4}-{(seed & 0xFFFF):X4}",
                Seed = seed,
                SupplierId = supplierId,
                CrateId = crateId,
                Location = SpecimenLocation.InCrate,
                Condition = new SpecimenCondition { Cleaned = cleaned },
                DiscoveredAtTicks = DateTime.UtcNow.Ticks,
            };
            State.Specimens.Add(r);
            return r;
        }

        /// <summary>
        /// The saw's commit: the parent record becomes the lineage's root (Location Cut, never spawned again) and two
        /// piece records are born from the same seed. Nothing is rerolled and nothing is duplicated: damage indices are
        /// shared with the parent, a piece's own damage lives on its own copy from here on.
        /// </summary>
        public (SpecimenRecord a, SpecimenRecord b) CutSpecimen(SpecimenRecord parent, PieceShape shapeA, PieceShape shapeB, string tool)
        {
            parent.CutCommitted = false;
            parent.CutProgress = 0f;
            SpecimenRecord Make(PieceShape shape, string suffix)
            {
                var geo = GeodeMeshBuilder.BuildPiece(parent.Geology, shape);
                var r = new SpecimenRecord
                {
                    Id = parent.Id + suffix,
                    Seed = parent.Seed,
                    SupplierId = parent.SupplierId,
                    CrateId = parent.CrateId,
                    Condition = parent.Condition.Clone(),
                    Location = SpecimenLocation.World,
                    DiscoveredAtTicks = parent.DiscoveredAtTicks,
                    OpenedAtTicks = DateTime.UtcNow.Ticks,
                    IsPiece = true,
                    Piece = shape,
                    ParentId = parent.Id,
                    CutIndex = parent.CutIndex + 1,
                    PieceRetained = geo.RetainedCrystalFraction,
                    PieceOpening = geo.CavityOpening,
                    PieceSymmetry = geo.CutSymmetry,
                    PieceFaceArea = geo.FaceAreaFraction,
                    Polish = 0f,
                    ProcessedBy = tool,
                    ShellDamage = parent.ShellDamage,
                    StrikeCount = parent.StrikeCount,
                    SectorStress = (float[])(parent.SectorStress ?? Array.Empty<float>()).Clone(),
                    Impacts = new List<Vector4>(parent.Impacts ?? new List<Vector4>()),
                    CustomName = null,
                };
                r.Condition.Opened = true;
                r.Condition.Cleaned = 1f;   // the slurry washes the piece
                r.Condition.Rinsed = true;
                // damage as the appraisal sees it, from the piece's own crystals
                float total = 0f, lost = 0f;
                foreach (var c in geo.Crystals)
                {
                    float w = c.Height * c.Height * (c.Centerpiece ? 4f : 1f);
                    total += w;
                    byte d = r.Condition.DamageAt(c.Index);
                    lost += w * (d == CrystalDamage.Chipped ? 0.3f : d == CrystalDamage.Broken ? 0.7f : d >= CrystalDamage.Missing ? 1f : 0f);
                }
                r.DamageFraction = total > 0f ? lost / total : 0f;
                State.Specimens.Add(r);
                return r;
            }
            var a = Make(shapeA, parent.IsPiece ? "a" : "-A");
            var b = Make(shapeB, parent.IsPiece ? "b" : "-B");
            bool firstOpen = !parent.IsOpened;
            parent.Location = SpecimenLocation.Cut;
            parent.Condition.Opened = true;
            var st = State.Stats;
            st.SawCuts++;
            if (shapeA.IsSlab || shapeB.IsSlab) st.SlabsCut++;
            if (firstOpen)
            {
                st.RocksProcessed++;
                // the discovery is the better piece's
                var best = a.PristineForSale() >= b.PristineForSale() ? a : b;
                RecordDiscovery(best, best.DamageFraction);
            }
            foreach (var r in new[] { a, b })
            {
                float v = r.PristineForSale();
                if (v > st.HighestValueSawResult) { st.HighestValueSawResult = v; st.HighestValueSawResultName = r.DisplayName; }
                float face = r.PieceFaceArea * Mathf.PI * parent.Geology.Size * parent.Geology.Size * 10000f;
                if (r.Piece.IsSlab && face > st.LargestSlabFaceCm2) { st.LargestSlabFaceCm2 = face; st.LargestSlabName = r.DisplayName; }
            }
            var entity = GetEntity(parent.Id);
            if (entity != null) Despawn(entity);
            StateChanged?.Invoke();
            FlushSave("cut");
            return (a, b);
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
        /// <summary>The call made in the hand meets the rock: mastery counters, a line in the history, a note for the reveal.</summary>
        public string ScoreCall(SpecimenRecord r)
        {
            if (r == null || !r.Predicted) return "";
            var g = r.Geology;
            bool hollow = g.Cavity != CavityArchetype.Nodule;
            bool hollowRight = r.PredictedHollow == hollow;
            bool tierRight = r.PredictedTier >= 0 && Mathf.Abs(r.PredictedTier - (int)g.Tier) <= 1;
            if (hollowRight) State.Stats.HollowCallsRight++;
            if (tierRight) State.Stats.TierCallsRight++;
            string line = UI.AppraisalUI.PredictionLine(r);
            GameState.Log(r, "called", 0f, line);
            return line;
        }

        public void RecordDiscovery(SpecimenRecord r, float damageFraction)
        {
            GameState.Log(r, "opened", 0f, (r.ProcessedBy == "cracker" ? "geode cracker" : r.ProcessedBy == "saw" ? "trim saw" : "hammer and chisel") + (damageFraction > 0.005f ? $", {damageFraction * 100f:F0}% crystal damage" : ", clean"));
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

            if (string.IsNullOrEmpty(r.ProcessedBy)) r.ProcessedBy = "hammer";
            var st = State.Stats;
            st.SpecimensOpened++;
            if (damageFraction > 0.001f) { if (damageFraction > st.MostDamagedFraction) { st.MostDamagedFraction = damageFraction; st.MostDamagedName = r.DisplayName; } }
            else st.CleanOpens++;
            if (g.MassKg > st.LargestSpecimenKg) { st.LargestSpecimenKg = g.MassKg; st.LargestSpecimenName = r.DisplayName; }
            if (value > st.HighestValueHammerResult) { st.HighestValueHammerResult = value; st.HighestValueHammerResultName = r.DisplayName; }

            if (firstOfFamily) Discovered?.Invoke(r, "First " + fam.Name);
            else if (g.Tier >= QualityTier.Exceptional) Discovered?.Invoke(r, Valuation.TierLabel(g.Tier) + " find");
            else if (record && entry.Found > 1) Notify($"Best {fam.Name} so far", NotificationKind.Info);
            StateChanged?.Invoke();
        }

        // ------------------------------------------------------------------------------------
        // Purchases
        // ------------------------------------------------------------------------------------
        public bool BuyCrate(string supplierId, out string error)
        {
            var supDef = Economy.SupplierCatalog.Get(supplierId);
            if (supDef != null && supDef.Occasional && !State.OfferedLots.Contains(supplierId)) { error = "That lot is not on offer right now"; return false; }
            error = null;
            var sup = Economy.SupplierCatalog.Get(supplierId);
            if (sup == null) { error = "Unknown supplier"; return false; }
            if (!State.HasSupplier(supplierId)) { error = "Supplier not unlocked"; return false; }
            if (!CanAfford(sup.Price)) { error = "Not enough cash"; return false; }
            var receiving = FindAnyObjectByType<ReceivingArea>();
            if (receiving == null) { error = "No receiving area"; return false; }
            int crateCap = State.WorkshopStage >= 3 ? 6 : 4;
            if (_crates.Count >= crateCap) { error = "The receiving pallet is full. Open or break down a crate first."; return false; }
            TrySpend(sup.Price, "crate");
            var crate = Economy.CrateGenerator.Generate(State, sup, CreateSpecimenRecord);
            State.Stats.CratesPurchased++;
            receiving.Deliver(crate);
            Economy.Auction.OnDelivery(this);   // the courier collects consigned pieces and brings back the hammer
            Audio.WorkshopAudio.Play2D("ui_buy", 0.7f);
            Notify($"{sup.Name} ordered. Delivery at the pallet.", NotificationKind.Success);
            Tutorial.Notify("crate_bought");
            if (State.CrateCounter >= 2) Tutorial.Notify("upgrade_or_crate");
            StateChanged?.Invoke();
            // sources gated on crates bought unlock here too, not only after a sale
            foreach (var id in Economy.SupplierCatalog.EvaluateUnlocks(State))
                Notify($"New supplier available: {Economy.SupplierCatalog.Get(id).Name}", NotificationKind.Discovery);
            // occasional lots come and go with the crates; an occasional lot bought leaves the offer list
            Economy.Market.ConsumeOffer(State, supplierId);
            string offer = Economy.Market.RefreshOffers(State);
            if (offer != null) Notify($"On offer now: {Economy.SupplierCatalog.Get(offer).Name}", NotificationKind.Discovery);
            FlushSave("crate-bought");
            return true;
        }

        /// <summary>Anything left to turn into cash: unopened rocks anywhere, or opened pieces not yet sold or displayed.</summary>
        public bool HasProcessableMaterial()
        {
            if (State == null) return false;
            foreach (var s in State.Specimens)
            {
                if (s.Location == SpecimenLocation.Sold || s.Location == SpecimenLocation.Discarded || s.Location == SpecimenLocation.Cut || s.Location == SpecimenLocation.DisplaySlot || s.Location == SpecimenLocation.Consigned) continue;
                return true;   // includes stock on the sales fixtures: it can always be taken back to the dealer
            }
            return false;
        }

        /// <summary>
        /// An upgrade must never strand the career: with nothing left to process, the player keeps enough cash for the
        /// cheapest crate. Explains why in a short label the tablet can put on the button.
        /// </summary>
        public bool CanBuyUpgrade(string upgradeId, out string reason)
        {
            reason = null;
            var up = Economy.UpgradeCatalog.Get(upgradeId);
            if (up == null) { reason = "Unknown upgrade"; return false; }
            if (!up.Consumable && State.HasUpgrade(upgradeId)) { reason = "Installed"; return false; }
            if (!string.IsNullOrEmpty(up.Requires) && !State.HasUpgrade(up.Requires)) { reason = "Needs the " + Economy.UpgradeCatalog.Get(up.Requires).Name; return false; }
            if (up.Consumable && upgradeId == Economy.UpgradeCatalog.SawBlade && State.BladeWear < 0.2f) { reason = "Blade still sharp"; return false; }
            if (upgradeId == Economy.UpgradeCatalog.Stage3 && Economy.Reputation.Tier(State) < 3) { reason = $"Needs a respected name ({Economy.Reputation.Word(State).ToLowerInvariant()} now)"; return false; }
            if (!CanAfford(up.Price)) { reason = $"{UI.UiKit.Money(up.Price - State.Cash)} more"; return false; }
            float cheapest = Economy.SupplierCatalog.Get(Economy.SupplierCatalog.Local).Price;
            if (State.Cash - up.Price < cheapest && !HasProcessableMaterial()) { reason = $"Keep {UI.UiKit.Money(cheapest)} for a crate"; return false; }
            return true;
        }

        /// <summary>
        /// Bad luck cannot brick a career either: broke, with nothing left to crack or sell, the dealer fronts the
        /// price of a Local crate. Rare by design (early crates are tuned to pay for themselves) and recorded in the stats.
        /// </summary>
        public void CheckSolvency()
        {
            if (State == null) return;
            float cheapest = Economy.SupplierCatalog.Get(Economy.SupplierCatalog.Local).Price;
            if (State.Cash + 0.001f >= cheapest || HasProcessableMaterial()) return;
            float advance = Mathf.Ceil(cheapest - State.Cash);
            State.Stats.DealerAdvances++;
            AddCash(advance, "advance");
            Notify($"The dealer fronts you {UI.UiKit.Money(advance)} against your next crate. Rough week.", NotificationKind.Warning);
            StateChanged?.Invoke();
        }

        public bool BuyUpgrade(string upgradeId, out string error)
        {
            error = null;
            var up = Economy.UpgradeCatalog.Get(upgradeId);
            if (up == null) { error = "Unknown upgrade"; return false; }
            if (!up.Consumable && State.HasUpgrade(upgradeId)) { error = "Already owned"; return false; }
            if (!CanBuyUpgrade(upgradeId, out string why)) { error = why; return false; }
            TrySpend(up.Price, "upgrade");
            if (up.Consumable)
            {
                if (upgradeId == Economy.UpgradeCatalog.SawBlade) State.BladeWear = 0f;
                Audio.WorkshopAudio.Play2D("ui_buy", 0.7f);
                Notify($"{up.Name} fitted.", NotificationKind.Success);
                StateChanged?.Invoke();
                FlushSave("supplies");
                return true;
            }
            State.Upgrades.Add(upgradeId);
            if (upgradeId == Economy.UpgradeCatalog.CollectionCabinet) State.DisplayCapacity += 8;
            if (upgradeId == Economy.UpgradeCatalog.DisplayExpansion) State.DisplayCapacity += 4;
            if (upgradeId == Economy.UpgradeCatalog.SalesTable) State.SaleCapacity += 12;     // the island: four on the glass, two risers, six behind it
            if (upgradeId == Economy.UpgradeCatalog.ShopShelving) State.SaleCapacity += 18;    // two nine-slot runs
            if (upgradeId == Economy.UpgradeCatalog.Stage2)
            {
                State.WorkshopStage = 2;
                State.DisplayCapacity += Workshop.WorkshopExpansion.Stage2DisplaySlots;
                State.SaleCapacity += Workshop.WorkshopExpansion.Stage2SaleSlots;
            }
            if (upgradeId == Economy.UpgradeCatalog.Stage3)
            {
                State.WorkshopStage = 3;
                State.DisplayCapacity += Workshop.WorkshopExpansion.Stage3DisplaySlots;
                State.SaleCapacity += Workshop.WorkshopExpansion.Stage3SaleSlots;
            }
            Audio.WorkshopAudio.Play2D("ui_buy", 0.7f);
            if (upgradeId == Economy.UpgradeCatalog.Stage2)
            {
                Audio.WorkshopAudio.Play2D("thud", 0.8f);
                Notify("Workshop expanded: saw bay, polishing corner, rock rack, trophy wall and showroom shelf are in.", NotificationKind.Discovery);
                foreach (var id in Economy.SupplierCatalog.EvaluateUnlocks(State))
                    Notify($"New supplier available: {Economy.SupplierCatalog.Get(id).Name}", NotificationKind.Discovery);
            }
            else if (upgradeId == Economy.UpgradeCatalog.Stage3)
            {
                Audio.WorkshopAudio.Play2D("thud", 0.8f);
                Notify("Stage 3: the slab saw is in the bay, the UV lamp is at the scale, the gallery plinths and the second case are in the showroom, the receiving bay is bigger.", NotificationKind.Discovery);
                foreach (var id in Economy.SupplierCatalog.EvaluateUnlocks(State))
                    Notify($"New supplier available: {Economy.SupplierCatalog.Get(id).Name}", NotificationKind.Discovery);
            }
            else Notify($"{up.Name} installed.", NotificationKind.Success);
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
