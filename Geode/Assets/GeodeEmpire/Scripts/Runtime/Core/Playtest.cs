using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering.Universal;
using GeodeEmpire.Cracking;
using GeodeEmpire.Interaction;
using GeodeEmpire.Player;
using GeodeEmpire.Save;
using GeodeEmpire.Specimens;
using GeodeEmpire.Workshop;

namespace GeodeEmpire.Core
{
    /// <summary>
    /// Scripted play sessions that use the DevDriver (virtual devices) to exercise the real loop:
    /// buy, open crate, pick rock, bench, strike until open, sort, appraise, sell, display.
    /// Results accumulate in Log so an external observer (the Editor eval) can read them.
    /// </summary>
    public sealed class Playtest : MonoBehaviour
    {
        public static Playtest Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Instance = null; }
        public readonly StringBuilder Log = new StringBuilder();
        public bool Running;
        public string Phase = "idle";
        public bool UseGamepad;

        public static Playtest Get()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("_Playtest");
            DontDestroyOnLoad(go);          // title <-> workshop flows cross scene loads
            Instance = go.AddComponent<Playtest>();
            return Instance;
        }

        private DevDriver D { get { var d = DevDriver.Get(); d.UseGamepad = UseGamepad; return d; } }
        private GameSession S => GameSession.Instance;
        private PlayerInteractor P => D.Player;

        private int _snap;
        private bool _stockedSnap;
        public string SnapDir = "Assets/Output/fresh";

        /// <summary>Contact-sheet frame of the Game view at a milestone (Editor: relative to the project root).</summary>
        private void Snap(string name)
        {
            System.IO.Directory.CreateDirectory(SnapDir);
            string path = $"{SnapDir}/{_snap++:D2}_{name}.png";
            ScreenCapture.CaptureScreenshot(path, 2);   // the Editor's Game view is small; twice its size keeps screen text legible in review
            L($"snap {path}");
        }

        private void L(string msg)
        {
            Log.AppendLine($"[{Time.time:F1}] {msg}");
        }

        public string Dump() => Log.ToString();

        // ---- primitives ---------------------------------------------------------------------
        private IEnumerator Interact()
        {
            if (UseGamepad) yield return D.PadTap(GamepadButton.South, 0.1f);
            else yield return D.Tap(Key.E, 0.1f);
            yield return new WaitForSeconds(0.15f);
        }

        private IEnumerator LookAndInteract(Vector3 point, string expectPromptContains, float settle = 0.25f)
        {
            yield return DismissLetters();
            if (D.LastWalkRemaining > 0.6f) L($"  walk ended {D.LastWalkRemaining:F2} m short of its target (player at {D.Controller.transform.position:F2})");
            D.LookAt(point);
            yield return new WaitForSeconds(settle);
            string prompt = P != null ? P.Prompt : "";
            if (!prompt.Contains(expectPromptContains))
            {
                Probe("miss");
                // second chance: re-aim and wait a little longer
                D.LookAt(point);
                yield return new WaitForSeconds(0.4f);
                prompt = P != null ? P.Prompt : "";
                Probe("retry");
            }
            L($"prompt='{prompt}' (want '{expectPromptContains}')");
            yield return Interact();
        }

        /// <summary>Every collider along the crosshair ray, with what it belongs to and whether it would take a press.</summary>
        private void ProbeAll(string tag)
        {
            var p = P;
            var cam = p.Cam;
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            var hits = Physics.RaycastAll(ray, 3f, p.Mask, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            var sb = new StringBuilder($"  probeAll[{tag}] {hits.Length} hits:");
            foreach (var h in hits)
            {
                var inter = h.collider.GetComponentInParent<IInteractable>();
                sb.Append($"\n     {h.collider.transform.root.name}/{h.collider.name}@{h.distance:F2} trigger={h.collider.isTrigger} inter={(inter != null ? inter.GetType().Name : "-")} can={(inter != null && inter.CanInteract(p))}");
            }
            L(sb.ToString());
        }

        private void Probe(string tag)
        {
            var p = P;
            var c = D.Controller;
            var cam = p.Cam;
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            string hit = Physics.Raycast(ray, out var h, 3f, p.Mask, QueryTriggerInteraction.Collide) ? $"{h.collider.name}@{h.distance:F2} inter={(h.collider.GetComponentInParent<IInteractable>() != null)}" : "none";
            L($"  probe[{tag}] pos={c.transform.position:F2} fwd={cam.transform.forward:F2} yaw={c.Yaw:F0} hit={hit} target={(p.Target != null)} prompt='{p.Prompt}' locked={p.InputLocked} gameplay={GameInput.GameplayEnabled} held={(p.Held != null)} inspecting={p.Inspecting}");
        }

        private IEnumerator Strike(float holdSeconds)
        {
            var bench = Find<CrackingBench>();
            if (bench != null && bench.Active && bench.ClampOwned && !bench.ClampClosed) { yield return Interact(); yield return new WaitForSeconds(0.5f); L($"  clamp closed: seat={bench.Stability:F2}"); }
            if (UseGamepad)
            {
                D.PadState(Vector2.zero, Vector2.zero, 0f, 1f);
                yield return new WaitForSeconds(holdSeconds);
                D.PadState(Vector2.zero, Vector2.zero, 0f, 0f);
            }
            else yield return D.ClickHold(0, holdSeconds);
            yield return new WaitForSeconds(0.32f);
        }

        private IEnumerator Rotate(float seconds, int dir)
        {
            if (UseGamepad)
            {
                D.PadState(Vector2.zero, Vector2.zero, 0f, 0f, dir > 0 ? GamepadButton.RightShoulder : GamepadButton.LeftShoulder);
                yield return new WaitForSeconds(seconds);
                D.PadState(Vector2.zero, Vector2.zero, 0f, 0f);
            }
            else
            {
                D.KeyDown(dir > 0 ? Key.R : Key.Q);
                yield return new WaitForSeconds(seconds);
                D.KeyUp();
            }
            yield return null;
        }

        private IEnumerator MoveCursor(float dx, float dy)
        {
            if (UseGamepad)
            {
                D.PadState(Vector2.zero, new Vector2(Mathf.Sign(dx), Mathf.Sign(dy)) * 0.8f, 0f, 0f);
                yield return new WaitForSeconds(Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) / 0.85f / 0.8f);
                D.PadState(Vector2.zero, Vector2.zero, 0f, 0f);
            }
            else
            {
                D.MouseDelta(dx / 0.0011f, dy / 0.0011f);
            }
            yield return null;
        }

        /// <summary>Move the bench cursor to a viewport target through the mouse/right-stick path.</summary>
        private IEnumerator AimCursor(CrackingBench bench, Vector2 target)
        {
            for (int i = 0; i < 90; i++)
            {
                Vector2 delta = target - bench.Cursor;
                if (delta.magnitude < 0.012f) break;
                if (UseGamepad)
                {
                    D.PadState(Vector2.zero, Vector2.ClampMagnitude(delta * 12f, 1f), 0f, 0f);
                    yield return null;
                }
                else
                {
                    D.MouseDelta(delta.x / 0.0011f, delta.y / 0.0011f);
                    yield return null;
                }
            }
            if (UseGamepad) D.PadState(Vector2.zero, Vector2.zero, 0f, 0f);
            yield return null;
        }

        /// <summary>Run F: snapshot the career, reload it in place through the real save path, and diff.</summary>
        public string RunSaveReloadCheck()
        {
            var s = S;
            s.FlushSave("test");
            var before = new Dictionary<string, string>();
            foreach (var r in s.State.Specimens) before[r.Id] = Describe(r);
            float cash = s.State.Cash;
            int crates = s.State.Crates.Count;
            var upgrades = string.Join(",", s.State.Upgrades);
            s.ContinueGame();
            var sb = new StringBuilder();
            int mismatches = 0;
            foreach (var r in s.State.Specimens)
            {
                if (!before.TryGetValue(r.Id, out var d)) { sb.AppendLine("  new after reload?! " + r.Id); mismatches++; continue; }
                var now = Describe(r);
                if (now != d) { sb.AppendLine($"  MISMATCH {r.Id}\n    before {d}\n    after  {now}"); mismatches++; }
            }
            int entities = s.Entities.Count;
            int expectedEntities = 0;
            foreach (var r in s.State.Specimens) if (r.Location != SpecimenLocation.Sold && r.Location != SpecimenLocation.Discarded && r.Location != SpecimenLocation.Cut) expectedEntities++;
            sb.Insert(0, $"save/reload: specimens={s.State.Specimens.Count} cashBefore={cash} cashAfter={s.State.Cash} crates={crates}->{s.State.Crates.Count} upgrades='{upgrades}' entities={entities}/{expectedEntities} mismatches={mismatches}\n");
            foreach (var e in s.Entities.Values)
            {
                float dist = (e.transform.position - e.Record.WorldPosition).magnitude;
                if (dist > 0.3f && e.Record.Location != SpecimenLocation.Held) sb.AppendLine($"  {e.Id} {e.Record.Location} spawned {dist:F2} m from saved position");
            }
            L(sb.ToString());
            return sb.ToString();
        }

        private static string Describe(SpecimenRecord r)
        {
            var g = r.Geology;
            return $"seed={r.Seed:X} {g.Mineral} {g.Cavity} {g.Tier} ${g.BaseValue} loc={r.Location}/{r.LocationIndex} opened={r.IsOpened} appraised={r.Appraised}:{r.AppraisedValue} strikes={r.StrikeCount} dmg=[{string.Join("", r.Condition.CrystalDamage ?? new byte[0])}] stress=[{string.Join(",", System.Array.ConvertAll(r.SectorStress ?? new float[0], f => f.ToString("F2")))}] shell={r.ShellDamage:F2} name={r.CustomName}";
        }

        private static Vector3 ZonePos(ZoneKind kind, int index = 0)
        {
            foreach (var z in FindObjectsByType<PlacementZone>(FindObjectsInactive.Exclude))
                if (z.Kind == kind && (kind != ZoneKind.DisplaySlot || z.SlotIndex == index)) return z.transform.position;
            return Vector3.zero;
        }

        private static T Find<T>() where T : Object => FindAnyObjectByType<T>();

        // the partition between workshop and showroom is only open by the north wall
        private const float PartitionX = 2.4f;
        private static readonly Vector3 OpeningWest = new Vector3(2.2f, 0f, 1.8f), OpeningEast = new Vector3(2.95f, 0f, 1.8f);

        private int _walkRescues;

        /// <summary>
        /// Walk with a rescue: when the controller ends up wedged well short of its target, list what surrounds it and
        /// teleport it there so the run keeps going. The count is reported at the end, so a wedge is still a finding.
        /// </summary>
        private IEnumerator Walk(Vector3 target, float tolerance = 0.3f, float timeout = 12f)
        {
            yield return DismissLetters();
            yield return D.WalkTo(target, tolerance, timeout);
            if (D.LastWalkRemaining <= 0.9f) yield break;
            var c = D.Controller;
            if (c == null) yield break;
            Vector3 pos = c.transform.position;
            var names = new HashSet<string>();
            foreach (var col in Physics.OverlapCapsule(pos + Vector3.up * 0.25f, pos + Vector3.up * 1.5f, 0.5f, ~0, QueryTriggerInteraction.Ignore))
                if (col.transform.root != c.transform.root) names.Add(col.transform.root.name + "/" + col.name);
            _walkRescues++;
            L($"  WALK RESCUE {_walkRescues}: wedged {D.LastWalkRemaining:F2} m short at {pos:F2} next to [{string.Join(", ", names)}]; teleporting to {target:F2}");
            D.Teleport(new Vector3(target.x, 0f, target.z), c.transform.eulerAngles.y);
            yield return null;
            D.LastWalkRemaining = 0f;
        }

        /// <summary>Walk to a point, going round through the partition opening when the target is in the other room.</summary>
        private IEnumerator RouteTo(Vector3 target, float tolerance = 0.3f)
        {
            var c = D.Controller;
            bool hereEast = c.transform.position.x > PartitionX, thereEast = target.x > PartitionX;
            if (hereEast != thereEast)
            {
                yield return Walk(hereEast ? OpeningEast : OpeningWest, 0.3f, 14f);
                yield return Walk(hereEast ? OpeningWest : OpeningEast, 0.3f, 8f);
            }
            yield return Walk(target, tolerance, 14f);
        }

        /// <summary>A standing spot near a station point, offset toward the room centre so we never walk into walls.</summary>
        private static Vector3 StandNear(Vector3 point, float dist = 0.95f)
        {
            Vector3 toCenter = new Vector3(0f, 0f, -0.3f) - point; toCenter.y = 0f;
            if (toCenter.sqrMagnitude < 0.01f) toCenter = Vector3.back;
            var p = point + toCenter.normalized * dist; p.y = 0f;
            return p;
        }

        // ---- scenarios ----------------------------------------------------------------------
        /// <summary>Run A: buy, open the crate, crack one rock with careful medium strikes, sort it.</summary>
        public void RunFirstCrate(string style = "careful") { if (!Running) StartCoroutine(FirstCrate(style)); }

        public void RunStarterAcceptance() { if (!Running) StartCoroutine(StarterAcceptance()); }

        /// <summary>
        /// §14 of the starter-rebuild spec, run against a real fresh save: the day-one business is small and does
        /// not show the mature one; nothing later is installed; the collection is empty; then buy, receive, site
        /// and reload a piece of equipment and check the room actually changed and stayed changed.
        /// </summary>
        private IEnumerator StarterAcceptance()
        {
            Running = true;
            Phase = "starter-acceptance";
            S.NewGame();
            yield return new WaitForSeconds(0.6f);
            var st = S.State;
            int pass = 0, fail = 0;
            void Check(string what, bool ok, string detail = null)
            {
                if (ok) pass++; else fail++;
                L($"  {(ok ? "ok  " : "FAIL")}  {what}{(detail == null ? "" : "  (" + detail + ")")}");
            }

            L("== StarterAcceptance: a real fresh save");

            // --- the day-one business -----------------------------------------------------------
            var premises = Find<Workshop.PremisesExpansion>();
            Check("back room is not leased", premises != null && !premises.BackRoomRoot.activeSelf);
            Check("shop front is not leased", premises != null && !premises.ShopFrontRoot.activeSelf);
            Check("the hoarding is up", premises != null && premises.ShopFrontHoarding.activeSelf);
            Check("the north openings are boarded", premises != null && premises.BackRoomHoarding.activeSelf);
            Check("no retail shop is running", Retail.RetailShop.Instance == null);

            // how much floor the player can actually reach, measured the way build mode measures it
            Build.PlacementValidator.InvalidateMask();
            int free = 0, reach = 0;
            Core.WorldIntegrityAudit.Clearance(out free, out reach);
            const float CellM2 = 0.15f * 0.15f;   // WorldIntegrityAudit.Clearance samples on a 15 cm grid
            float reachedM2 = reach * CellM2;
            L($"  reachable floor {reachedM2:F1} m^2 ({reach} cells of {free} free)");
            Check("day one is a small unit", reachedM2 < 35f, $"{reachedM2:F1} m^2");

            // --- nothing later is installed ------------------------------------------------------
            foreach (var id in new[] { "trim_saw", "geode_cracker", "flat_lap", "shop_island", "display_wall_a", "display_wall_b", "display_cabinet" })
            {
                Build.PlaceableFixture f = null;
                foreach (var x in Build.PlaceableFixture.All) if (x.Id == id) f = x;
                Check($"{id} is not installed", f != null && !f.Owned && (f.Body == null || !f.Body.activeInHierarchy));
            }

            // --- the collection is empty ----------------------------------------------------------
            Check("no display capacity yet", st.DisplayCapacity == 0, "cap=" + st.DisplayCapacity);
            Check("nothing on display", st.DisplayedCount() == 0);
            Check("no specimens owned", st.Specimens.Count == 0, st.Specimens.Count + " records");
            Check("encyclopedia empty", st.Encyclopedia.Count == 0);
            int loose = 0;
            foreach (var e in S.Entities.Values) if (e != null) loose++;
            Check("no specimen entities in the world", loose == 0, loose + " entities");

            // --- buy, receive, site, reload -------------------------------------------------------
            Phase = "starter-buy";
            st.Cash = 2000f;
            S.RaiseStateChanged();
            S.BuyUpgrade(Economy.UpgradeCatalog.BackRoom, out _);
            yield return new WaitForSeconds(0.5f);
            Check("the back room opens on purchase", premises.BackRoomRoot.activeSelf && !premises.BackRoomHoarding.activeSelf);
            bool bought = S.BuyUpgrade(Economy.UpgradeCatalog.CollectionCabinet, out string why);
            Check("the collection cabinet can be bought", bought, why);
            yield return new WaitForSeconds(0.5f);
            Check("buying it grants display slots", st.DisplayCapacity == 8, "cap=" + st.DisplayCapacity);

            var delivery = Find<Build.FixtureDelivery>();
            int crated = 0;
            if (delivery != null) foreach (var slot in delivery.Slots) if (slot?.Root != null && slot.Root.activeSelf) crated++;
            Check("it arrives crated in the workshop", crated == 1, crated + " crates on the floor");

            Build.PlaceableFixture cab = null;
            foreach (var x in Build.PlaceableFixture.All) if (x.Id == "display_cabinet") cab = x;
            var bm = Find<Build.BuildMode>();
            bool sited = false;
            Vector3 sitedAt = Vector3.zero;
            if (cab != null && bm != null)
            {
                // sweep the rooms the player has, the way a player sweeps a wall looking for a gap: the test is
                // that somewhere legal exists, not that one hard-coded metre does
                string last = "no candidate tried";
                for (float x = Build.ShopPlan.XMin + 0.4f; x <= Build.ShopPlan.XMax - 0.4f && !sited; x += 0.25f)
                    for (float z = Build.ShopPlan.ZMin + 0.4f; z <= Build.ShopPlan.ZMax - 0.4f && !sited; z += 0.25f)
                        foreach (float yaw in new[] { 0f, 90f, 180f, 270f })
                        {
                            var p = new Vector3(x, 0f, z);
                            if (!Build.ShopPlan.Open(p)) continue;
                            if (bm.TryPlace(cab, p, yaw, out last)) { sited = true; sitedAt = p; L($"    sited at ({x:F2}, {z:F2}) yaw {yaw:F0}"); break; }
                        }
                if (!sited) L("    place: " + last);
            }
            Check("the player can site it on a back-room wall", sited);
            yield return new WaitForSeconds(0.4f);
            Check("its body is in the room once sited", cab != null && cab.Body != null && cab.Body.activeInHierarchy,
                  cab == null ? "no fixture" : cab.Body == null ? "no body" : $"self={cab.Body.activeSelf} owned={cab.Owned} placed={cab.Pose.Placed} rootActive={cab.gameObject.activeInHierarchy}");

            // it must be refused where it would block the way out
            bool refused = cab != null && bm != null && !bm.TryPlace(cab, new Vector3(GeodeEmpire.Build.ShopPlan.WorkDoorX, 0f, GeodeEmpire.Build.ShopPlan.ZMin + 0.6f), 0f, out _);
            Check("placement in the doorway is refused", refused);
            // and in a room that has not been leased
            bool refusedShop = cab != null && bm != null && !bm.TryPlace(cab, new Vector3(5.0f, 0f, 2.0f), 0f, out _);
            Check("placement in the unleased showroom is refused", refusedShop);

            Phase = "starter-reload";
            var pose = st.Fixture("display_cabinet");
            Vector3 where = pose != null ? pose.Position : Vector3.zero;
            S.FlushSave("acceptance");
            S.ContinueGame();
            yield return new WaitForSeconds(0.8f);
            var pose2 = S.State.Fixture("display_cabinet");
            Check("the placement survives a reload", pose2 != null && pose2.Placed && (pose2.Position - where).sqrMagnitude < 0.0025f);
            Check("display capacity survives a reload", S.State.DisplayCapacity == 8);
            Check("the collection is still empty after a reload", S.State.DisplayedCount() == 0);

            // --- and the leases actually change the building --------------------------------------
            Phase = "starter-lease";
            S.State.Cash = 4000f;
            S.BuyUpgrade(Economy.UpgradeCatalog.ShopFront, out _);
            yield return new WaitForSeconds(0.4f);
            Check("the shop front opens on purchase", premises.ShopFrontRoot.activeSelf && !premises.ShopFrontHoarding.activeSelf);
            Check("the shop starts serving", Retail.RetailShop.Instance != null);
            Build.PlacementValidator.InvalidateMask();
            Core.WorldIntegrityAudit.Clearance(out int free2, out int reach2);
            float grownM2 = reach2 * CellM2;
            L($"  reachable floor after both leases {grownM2:F1} m^2");
            Check("the business is visibly bigger", grownM2 > reachedM2 * 1.8f, $"{reachedM2:F1} -> {grownM2:F1} m^2");

            L($"starter acceptance: pass={pass} fail={fail}");
            Running = false;
        }

        private IEnumerator FirstCrate(string style)
        {
            Running = true;
            Phase = "buy";
            L($"== FirstCrate ({style}) cash={S.State.Cash}");
            if (S.Crates.Count == 0)
            {
                S.BuyCrate("local", out string err);
                L("buy: " + (err ?? "ok") + " cash=" + S.State.Cash);
            }
            yield return new WaitForSeconds(1.4f);

            Phase = "open-crate";
            CrateEntity crate = null;
            foreach (var c in S.Crates.Values) if (!c.IsOpened || c.RemainingRocks > 0) { crate = c; break; }
            if (crate == null) { L("no crate"); Running = false; yield break; }
            Vector3 cratePos = crate.transform.position;
            Vector3 stand = cratePos + (new Vector3(-0.3f, 0f, 0.6f)).normalized * 1.1f;
            stand.y = 0f;
            yield return Walk(stand, 0.3f);
            D.LookAt(cratePos + Vector3.up * 0.2f);
            yield return null; yield return null;
            Snap("crate_delivered");
            if (!crate.IsOpened) yield return LookAndInteract(cratePos + Vector3.up * 0.2f, "Open crate");
            yield return new WaitForSeconds(0.9f);
            L("crate opened=" + crate.IsOpened + " remaining=" + crate.RemainingRocks);
            Snap("crate_opened");

            Phase = "pick-rock";
            SpecimenEntity rock = null;
            foreach (var e in S.Entities.Values) if (!e.IsOpened && e.Record.Location == SpecimenLocation.InCrate) { rock = e; break; }
            if (rock == null) { L("no rock"); Running = false; yield break; }
            yield return LookAndInteract(rock.transform.position, "Pick up rock");
            L("held=" + (P.Held != null ? P.Held.Id : "none"));
            if (P.Held == null) { Running = false; yield break; }
            yield return new WaitForSeconds(0.4f);
            Snap("rock_in_hand");

            Phase = "to-bench";
            Vector3 cradle = ZonePos(ZoneKind.Cradle);
            Vector3 benchStand = new Vector3(cradle.x, 0f, cradle.z - 0.95f);
            yield return Walk(benchStand, 0.25f);
            yield return LookAndInteract(cradle, "Set on the cradle");
            yield return new WaitForSeconds(1.2f);
            var bench = Find<CrackingBench>();
            L("bench active=" + bench.Active + " rock=" + (bench.Rock != null ? bench.Rock.Id : "none"));
            if (!bench.Active) { Running = false; yield break; }
            Snap("bench_start");

            Phase = "crack";
            int strikes = 0;
            float hold = style == "careless" ? 1.0f : style == "light" ? 0.25f : 0.5f;
            while (bench.Active && !bench.Opened && !bench.Revealing && strikes < 60)
            {
                // aim at the seam facing the camera (what a player learns to do), with a little error, using real input
                var hint = bench.SeamCursorHint() + new Vector2(Random.Range(-0.02f, 0.02f), Random.Range(-0.02f, 0.02f));
                yield return AimCursor(bench, hint);
                yield return Strike(hold);
                strikes++;
                var r = bench.LastResult;
                L($"strike {strikes}: force~{hold} sector={r.Sector} place={r.Placement:F2} added={r.StressAdded:F2} cracks={r.CracksTotal} slip={r.Slipped} dmg={r.Damaged} open={r.Opened}");
                if (strikes == 5) Snap("crack_mid");
                if (style != "careless" || strikes % 2 == 0) yield return Rotate(0.42f, 1);   // work around the ring
                while (bench.Revealing) yield return null;
            }
            yield return new WaitForSeconds(2.2f);
            L($"opened={bench.Opened} strikes={strikes} note='{bench.ResultNote}' damageEvents={bench.DamageEventsThisRock}");
            Snap("reveal");
            if (!bench.Opened) { Running = false; yield break; }

            Phase = "take";
            yield return Interact();
            yield return new WaitForSeconds(0.3f);
            L("held after take=" + (P.Held != null ? P.Held.Record.DisplayName : "none") + " value=" + (P.Held != null ? P.Held.Geology.BaseValue.ToString() : ""));

            Phase = "appraise";
            Vector3 scale = ZonePos(ZoneKind.Scale);
            yield return Walk(StandNear(scale), 0.3f);
            yield return LookAndInteract(scale, "Weigh on the scale");
            yield return new WaitForSeconds(1.6f);
            var ap = Find<AppraisalStation>();
            L("appraised=" + (ap.Current != null && ap.Current.Record.Appraised) + " value=" + (ap.Current != null ? ap.Current.Record.AppraisedValue.ToString() : ""));
            Snap("appraisal");
            yield return LookAndInteract(scale, "Take");
            L("held=" + (P.Held != null ? P.Held.Record.DisplayName : "none"));

            Phase = "sell";
            Vector3 tray = ZonePos(ZoneKind.SellTray);
            yield return Walk(StandNear(tray), 0.3f);
            yield return LookAndInteract(tray, "Place in the dealer outbox");
            var outbox = Find<SellOutbox>();
            L("outbox count=" + outbox.Count + " est=" + outbox.EstimateTotal());
            Snap("outbox");
            var intercom = Find<DealerIntercom>();
            yield return Walk(StandNear(new Vector3(intercom.transform.position.x, 0f, intercom.transform.position.z), 1.7f), 0.3f);
            yield return LookAndInteract(intercom.transform.position, "Call dealer");
            yield return new WaitForSeconds(0.5f);
            L($"cash={S.State.Cash} sold={S.State.Stats.SpecimensSold} suppliers={string.Join(",", S.State.UnlockedSuppliers)}");
            Snap("dealer_sold");
            Phase = "done";
            Running = false;
        }

        /// <summary>V4: take a caked rock from a crate, dunk it, scrub it clean holding the button, then tap-test it in the hand.</summary>
        public void RunWashRock() { if (!Running) StartCoroutine(WashRock()); }

        private IEnumerator WashRock()
        {
            Running = true;
            Phase = "wash";
            L($"== WashRock cash={S.State.Cash}");
            if (S.Crates.Count == 0) { S.BuyCrate("local", out string err); L("buy: " + (err ?? "ok")); yield return new WaitForSeconds(1.4f); }
            CrateEntity crate = null;
            foreach (var c in S.Crates.Values) if (!c.IsOpened || c.RemainingRocks > 0) { crate = c; break; }
            if (crate == null) { L("no crate"); Running = false; yield break; }
            if (!crate.IsOpened)
            {
                Vector3 cratePos = crate.transform.position;
                Vector3 stand = cratePos + (new Vector3(-0.3f, 0f, 0.6f)).normalized * 1.1f; stand.y = 0f;
                yield return Walk(stand, 0.3f);
                yield return LookAndInteract(cratePos + Vector3.up * 0.2f, "Open crate");
                yield return new WaitForSeconds(0.9f);
            }
            // the dirtiest rock in the crate
            SpecimenEntity rock = null; float dirtiest = -1f;
            foreach (var e in S.Entities.Values) if (!e.IsOpened && e.Record.Location == SpecimenLocation.InCrate && e.Visual.DirtRemaining > dirtiest) { dirtiest = e.Visual.DirtRemaining; rock = e; }
            if (rock == null) { L("no rock"); Running = false; yield break; }
            L($"rock {rock.Id} {rock.Geology.Mineral} size={rock.Geology.SizeClass} tex={rock.Geology.Texture} dirt={rock.Visual.DirtRemaining:F2} stain={rock.Geology.Stain:F2} chip={rock.Geology.HasNaturalChip} seamQ={rock.Geology.SeamQuality:F2} mass={rock.Geology.MassKg:F1}");
            yield return FetchRock(rock);
            if (P.Held == null) { L("could not pick up the rock"); Running = false; yield break; }
            if (P.Held != rock) { rock = P.Held; L($"picked a neighbour instead: {rock.Id} dirt={rock.Visual.DirtRemaining:F2}"); }
            // tap it in the hand
            D.SetMouseButton(1, true);
            yield return new WaitForSeconds(0.4f);
            yield return D.ClickHold(0, 0.08f);
            yield return new WaitForSeconds(0.3f);
            L($"inspect prompt='{P.Prompt}' hint='{P.Hint}'");
            Snap("hand_dirty");
            D.SetMouseButton(1, false);
            yield return new WaitForSeconds(0.3f);
            Vector3 tub = ZonePos(ZoneKind.Wash);
            yield return RouteTo(new Vector3(tub.x, 0f, tub.z - 0.9f), 0.25f);
            yield return LookAndInteract(tub, "Dunk in");
            yield return new WaitForSeconds(0.5f);
            L($"in tub: loc={rock.Record.Location} held={(P.Held != null)}");
            Snap("tub_before");
            var ws = Find<WashStation>();
            D.LookAt(tub + Vector3.up * 0.05f);
            yield return new WaitForSeconds(0.3f);
            L($"tub prompt='{P.Prompt}'");
            D.KeyDown(Key.E);
            float t0 = Time.time;
            while (rock.Visual.DirtRemaining > 0.02f && Time.time - t0 < 8f)
            {
                if (Time.time - t0 > 1.5f && Time.time - t0 < 1.6f) Snap("tub_scrubbing");
                yield return null;
            }
            D.KeyUp();
            L($"scrubbed in {Time.time - t0:F1}s: dirt={rock.Visual.DirtRemaining:F2} cleaned={rock.Record.Condition.Cleaned:F2} scrubbing={ws.Scrubbing}");
            yield return new WaitForSeconds(0.4f);
            Snap("tub_after");
            yield return LookAndInteract(tub, "Take");
            L($"held after wash={(P.Held != null ? P.Held.Id : "none")} loc={rock.Record.Location}");
            D.SetMouseButton(1, true);
            yield return new WaitForSeconds(0.5f);
            L($"inspect clean prompt='{P.Prompt}'");
            Snap("hand_clean");
            D.SetMouseButton(1, false);
            yield return new WaitForSeconds(0.2f);
            L(Core.CollisionAudit.Report("wash end"));
            Phase = "done";
            Running = false;
        }

        /// <summary>Test fixture: every rock still in a crate is opened and appraised on paper, put in the outbox and shipped.</summary>
        private IEnumerator SellCrateDirect()
        {
            var outbox = Find<SellOutbox>();
            int placed = 0;
            foreach (var e in new List<SpecimenEntity>(S.Entities.Values))
            {
                if (e.IsOpened || e.Record.Location != SpecimenLocation.InCrate) continue;
                var r = e.Record;
                r.Condition.Opened = true; r.OpenedAtTicks = System.DateTime.UtcNow.Ticks; r.ProcessedBy = "hammer";
                r.DamageFraction = 0.02f; r.Appraised = true; r.AppraisedValue = r.PristineForSale();
                S.RecordDiscovery(r, 0.02f);
                e.ApplyOpenPose();
                if (outbox.Tray.RefusalReason(e) == null) { outbox.Tray.Place(e, true); placed++; }
                if (placed >= 12) break;
            }
            yield return null;
            if (placed > 0) outbox.Ship();
            // whatever the tray would not take goes to the dealer on paper: no opened rock is left lying in a crate (it would sit in its open pose against the slats)
            int direct = 0;
            foreach (var e in new List<SpecimenEntity>(S.Entities.Values))
            {
                if (!e.IsOpened || e.Record.Location != SpecimenLocation.InCrate) continue;
                var r = e.Record; r.Location = SpecimenLocation.Sold; S.State.Stats.SpecimensSold++; S.AddCash(r.AppraisedValue, "test"); S.Despawn(e); direct++;
            }
            L($"  sold {placed} direct{(direct > 0 ? $" + {direct} leftovers" : "")}: cash={S.State.Cash} sold={S.State.Stats.SpecimensSold}");
            yield return BreakDownEmptyCrates();
        }

        /// <summary>V5 Stage 3 + endgame: force the career to the Stage-3 gate, buy it, check the room, verify a piece under UV, saw a tall rock on the slab saw, set three pieces on the plinths, open the exhibition.</summary>
        public void RunStage3() { if (!Running) StartCoroutine(Stage3()); }

        private IEnumerator Stage3()
        {
            Running = true;
            Phase = "stage3";
            var st = S.State;
            L($"== Stage3 cash={st.Cash} stage={st.WorkshopStage} rep={Economy.Reputation.Word(st)} ({Economy.Reputation.Score(st)})");
            S.AddCash(12000f, "test");
            string e;
            if (!st.HasUpgrade(Economy.UpgradeCatalog.TrimSaw)) S.BuyUpgrade(Economy.UpgradeCatalog.TrimSaw, out e);
            if (st.WorkshopStage < 2) { S.BuyUpgrade(Economy.UpgradeCatalog.Stage2, out e); yield return new WaitForSeconds(0.8f); }
            // a career that has earned its name: the stats a respected shop would have
            st.Stats.SpecimensSold += 60; st.Stats.CustomersServed += 20; st.Stats.CleanOpens += 20; st.Stats.SawCuts += 10; st.Stats.PiecesPolished += 5; st.Stats.CommissionsFilled += 3;
            for (int i = 0; i < 8; i++) st.GetOrCreateEntry((MineralId)i).Found = Mathf.Max(1, st.GetOrCreateEntry((MineralId)i).Found);
            SpawnTestStock(6, 0f);
            yield return DisplayKeepCore();
            L($"rep now={Economy.Reputation.Word(st)} ({Economy.Reputation.Score(st)}) tier={Economy.Reputation.Tier(st)} canBuyStage3={S.CanBuyUpgrade(Economy.UpgradeCatalog.Stage3, out string why)} ({why})");
            bool ok = S.BuyUpgrade(Economy.UpgradeCatalog.Stage3, out e);
            L($"stage 3: {(ok ? "ok" : e)} stage={st.WorkshopStage} display={st.DisplayCapacity} sale={st.SaleCapacity}");
            yield return new WaitForSeconds(1.0f);
            var exp = FindAnyObjectByType<Workshop.WorkshopExpansion>();
            L($"stage3 root active={(exp != null && exp.Stage3Root != null && exp.Stage3Root.activeSelf)}");
            var saw = Find<Lapidary.SawStation>();
            L($"saw usingLarge={saw.UsingLarge} bladeR={saw.BladeRadius} maxPass={saw.MaxPassHeight:F3}");
            L(Core.WorldIntegrityAudit.Report("stage3"));
            Snap("stage3_room");
            DevDriver.CaptureFrom(new Vector3(1.7f, 1.55f, 1.05f), new Vector3(2.0f, 1.05f, 2.25f), 42f, SnapDir + "/stage3_slab_saw.png");
            DevDriver.CaptureFrom(new Vector3(0.3f, 1.7f, 0.4f), new Vector3(1.0f, 0.1f, -0.6f), 50f, SnapDir + "/stage3_pallet.png");
            DevDriver.CaptureFrom(new Vector3(5.4f, 1.4f, -1.0f), new Vector3(6.7f, 0.85f, -1.75f), 45f, SnapDir + "/stage3_case2.png");
            // the slab saw takes a tall rock
            yield return SawCut(0f, 0f, 0.01f, false, true, false);
            Running = true;
            st = S.State;   // the saw run's save/reload check swapped the state object
            // UV verification: an exceptional piece on the scale
            SpecimenEntity best = null;
            foreach (var en in S.Entities.Values) if (en.IsOpened && en.Geology.Tier >= QualityTier.Exceptional && en.Record.Location != SpecimenLocation.Sold) { best = en; break; }
            if (best == null)
            {
                var rec = S.CreateSpecimenRecord(0x7D1UL, "premium", ""); rec.Location = SpecimenLocation.World; rec.Condition.Opened = true; rec.Condition.Rinsed = true;
                best = S.Spawn(rec, ZonePos(ZoneKind.Scale) + new Vector3(0f, 0.3f, -0.7f), Quaternion.identity, false); best.ApplyOpenPose();
                best.SetPose(new Vector3(best.transform.position.x, best.RestHeightOffset(true), best.transform.position.z), Quaternion.identity); best.SetStaticCollidable();
                rec.WorldPosition = best.transform.position; rec.WorldRotation = best.transform.rotation;
                S.RecordDiscovery(rec, 0f);
            }
            if (best.Zone != null) best.Zone.Take(best, true);
            var scale = Find<AppraisalStation>();
            scale.Scale.Place(best);
            yield return new WaitForSeconds(2.4f);
            L($"uv: certified={best.Record.Certified} fluorescence='{best.Record.Fluorescence}' value={best.Record.AppraisedValue} asking={Retail.RetailShop.AskingPrice(best.Record)}");
            Snap("stage3_uv");
            DevDriver.CaptureFrom(new Vector3(-2.25f, 1.45f, 1.1f), new Vector3(-3.0f, 0.98f, 0.7f), 40f, SnapDir + "/stage3_uv_close.png");
            scale.Scale.Take(best, true);
            // the exhibition wants a career's worth of display: the sawn half polished (on paper), curated finds in the cabinet
            // and the trophy wall for the collection goals, the best three on the plinths
            var director = UI.ExhibitionDirector.Instance;
            var zones = FindObjectsByType<PlacementZone>(FindObjectsInactive.Exclude);
            if (P.Held != null) { if (UseGamepad) yield return D.PadTap(GamepadButton.West, 0.1f); else yield return D.Tap(Key.G, 0.1f); yield return new WaitForSeconds(0.4f); }
            foreach (var en in S.Entities.Values) if (en.IsPiece && en.Record.Location != SpecimenLocation.Sold) { en.Record.Polish = 0.95f; L($"polished on paper: {en.Record.DisplayName}"); break; }
            var curated = new List<SpecimenEntity>();
            string[] sups = { Economy.SupplierCatalog.Local, Economy.SupplierCatalog.Regional, Economy.SupplierCatalog.CuttingRough, Economy.SupplierCatalog.AmethystLot };
            var seenTraits = new HashSet<RareTrait>(); var seenFams = new HashSet<MineralId>();
            var wants = new List<(string name, System.Func<SpecimenGeology, bool> ok)>
            {
                ("cathedral", g => g.Cavity == CavityArchetype.Cathedral && g.Tier >= QualityTier.Exceptional && g.MassKg < 2.5f),
                ("museum", g => g.Tier >= QualityTier.MuseumGrade && g.MassKg < 2.5f && g.Cavity != CavityArchetype.Nodule),
                ("heavy", g => g.MassKg >= 2.5f && g.Tier >= QualityTier.Exceptional && g.Cavity != CavityArchetype.Nodule),
            };
            for (int k = 0; k < 6; k++) { int fam = k; wants.Add(($"family{fam}", g => (int)g.Mineral == fam && g.Tier >= QualityTier.Exceptional && g.Cavity != CavityArchetype.Nodule && g.MassKg < 2.5f && g.Traits.Count > 0)); }
            ulong cseed = 20000UL;
            foreach (var w in wants)
            {
                SpecimenRecord rec = null;
                for (int tries = 0; tries < 60000 && rec == null; tries++, cseed++)
                {
                    var g = SpecimenGenerator.Generate(cseed);
                    if (!w.ok(g)) continue;
                    rec = S.CreateSpecimenRecord(cseed, sups[curated.Count % sups.Length], "");
                }
                if (rec == null) { L($"  curated {w.name}: no seed found"); continue; }
                rec.Location = SpecimenLocation.World; rec.Condition.Opened = true; rec.Condition.Rinsed = true; rec.Appraised = true; rec.AppraisedValue = Valuation.DamagedValue(rec.Geology, 0f, 0f);
                var en = S.Spawn(rec, new Vector3(-1.5f + (curated.Count % 4) * 0.45f, 0.3f, -0.6f + (curated.Count / 4) * 0.5f), Quaternion.identity, false);
                en.ApplyOpenPose(); S.RecordDiscovery(rec, 0f);
                curated.Add(en);
                foreach (var tr in rec.Geology.Traits) seenTraits.Add(tr); seenFams.Add(rec.Geology.Mineral);
                L($"  curated {w.name}: {rec.DisplayName} {rec.Geology.Tier} {rec.Geology.MassKg:F1} kg traits={rec.Geology.Traits.Count} sup={rec.SupplierId}");
            }
            L($"curated {curated.Count}: families={seenFams.Count} traits={seenTraits.Count}");
            foreach (var en in S.Entities.Values) if (en.IsPiece && en.Record.Polish > 0.9f && en != P.Held) { curated.Add(en); break; }
            int onPlinths = 0, inCabinet = 0;
            PlacementZone FindSlot(SpecimenEntity en, bool plinth, out string why)
            {
                why = null;
                foreach (var z in zones)
                {
                    if (z.Kind != ZoneKind.DisplaySlot || !z.IsEmpty || z.Locked || !z.gameObject.activeInHierarchy) continue;
                    if ((z.SlotIndex >= director.FirstPlinthSlot) != plinth) continue;
                    string r = z.RefusalReason(en) ?? z.FitRefusal(en);
                    if (r != null) { why = r; continue; }
                    return z;
                }
                return null;
            }
            foreach (var en in curated)
            {
                string refusal = null;
                var target = onPlinths < 3 ? FindSlot(en, true, out refusal) : null;
                if (target == null) target = FindSlot(en, false, out refusal);
                if (target == null) { L($"  no display slot for {en.Record.DisplayName}: {refusal}"); continue; }
                if (en.Zone != null) en.Zone.Take(en, true);
                target.Place(en, true);
                if (target.SlotIndex >= director.FirstPlinthSlot) { onPlinths++; L($"  plinth {target.SlotIndex}: {en.Record.DisplayName}"); } else inCabinet++;
            }
            L($"placed: plinths={onPlinths} cabinet={inCabinet} displayed={st.DisplayedCount()}");
            yield return new WaitForSeconds(0.5f);
            L(Core.CollisionAudit.Report("plinths"));
            DevDriver.CaptureFrom(new Vector3(5.2f, 1.6f, 0.2f), new Vector3(5.2f, 1.05f, 2.4f), 55f, SnapDir + "/stage3_gallery.png");
            // sourcing is a career fact: six suppliers on the books
            var supSet = new HashSet<string>(); foreach (var r in st.Specimens) if (!string.IsNullOrEmpty(r.SupplierId)) supSet.Add(r.SupplierId);
            foreach (var sd in Economy.SupplierCatalog.All) if (supSet.Count < 6 && !supSet.Contains(sd.Id)) { var rr = S.CreateSpecimenRecord(4242UL + (ulong)supSet.Count, sd.Id, ""); rr.Location = SpecimenLocation.Sold; supSet.Add(sd.Id); }
            foreach (var g in Economy.CollectionGoals.All) { var (have, need) = g.Progress(st); L($"  goal {g.Id}: {have}/{need}{(g.Done(st) ? "  done" : "")}"); }
            foreach (var ax in Economy.Exhibition.Axes(st)) L($"  axis {ax.Title}: {(ax.Met ? "met" : "NOT met")} ({ax.Detail})");
            L($"eligible={Economy.Exhibition.Eligible(st)} onPlinths={director.PlinthCount(st)}");
            S.RaiseStateChanged();
            yield return new WaitForSeconds(0.6f);
            L($"invite shown={st.ExhibitionInviteShown}");
            yield return DismissLetters();
            if (Economy.Exhibition.Eligible(st) && director.PlinthCount(st) >= 3)
            {
                // V5 §72 recovery: quit in the middle of the pass. Nothing is recorded until the room closes, so the reload
                // finds the pieces on the plinths, the invitation already read, and the exhibition still open on the tablet.
                director.Open();
                yield return new WaitForSeconds(3.0f);
                L($"mid-pass: running={director.Running} inputLocked={P.InputLocked} held={st.ExhibitionsHeld}");
                L(RunSaveReloadCheck());
                st = S.State; director = UI.ExhibitionDirector.Instance;
                L($"after mid-pass reload: held={st.ExhibitionsHeld} eligible={Economy.Exhibition.Eligible(st)} onPlinths={director.PlinthCount(st)} running={director.Running} inputLocked={P.InputLocked} inMenu={CursorController.InMenu}");
                director.Open();
                float t0 = Time.time; bool snapped = false;
                while (director.Running && Time.time - t0 < 30f)
                {
                    if (!snapped && Time.time - t0 > 3.5f) { snapped = true; Snap("exhibition_plinth"); }
                    if (CursorController.InMenu && Time.time - t0 > 12f) { Snap("exhibition_summary"); if (UseGamepad) yield return D.PadTap(GamepadButton.South, 0.1f); else yield return D.Tap(Key.Enter, 0.1f); yield return new WaitForSeconds(0.5f); }
                    yield return null;
                }
                L($"exhibition: held={st.ExhibitionsHeld} completed={st.ExhibitionCompletedTicks > 0} exhibited=[{string.Join(",", st.ExhibitedIds)}] running={director.Running} inputLocked={P.InputLocked}");
            }
            else L("exhibition not opened (eligibility or plinths)");
            L(RunSaveReloadCheck());
            L($"after reload: stage={S.State.WorkshopStage} held={S.State.ExhibitionsHeld} certified={S.State.FindSpecimen(best.Record.Id)?.Certified}");
            Phase = "done";
            Running = false;
        }

        /// <summary>V5 market: crates until an occasional lot is offered, sales until a buyer writes in, a favourite refused by the outbox, a commission filled through it.</summary>
        public void RunMarket() { if (!Running) StartCoroutine(MarketRun()); }

        private IEnumerator MarketRun()
        {
            Running = true;
            Phase = "market";
            L($"== Market cash={S.State.Cash}");
            S.AddCash(6000f, "test");
            var st = S.State;
            // crates until an offer appears (occasional lots need prestige 2 / three crates); prestige via test stock on display
            SpawnTestStock(6, 0f);
            yield return DisplayKeepCore();
            L($"prestige={st.Prestige} displayed={st.DisplayedCount()} collection={st.CollectionValue()}");
            int bought = 0; string offer = null;
            for (int i = 0; i < 12 && offer == null; i++)
            {
                string sup = st.HasSupplier(Economy.SupplierCatalog.Regional) ? Economy.SupplierCatalog.Regional : Economy.SupplierCatalog.Local;
                if (!S.BuyCrate(sup, out string err)) { L($"buy {sup} failed: {err}"); break; }
                bought++;
                yield return new WaitForSeconds(0.6f);
                if (st.OfferedLots.Count > 0) offer = st.OfferedLots[0];
                yield return BreakDownEmptyCrates();
                // open the newest crate and sell its rocks to the dealer (opened and appraised directly: this run is about the market, not the bench)
                yield return OpenNewestCrate();
                yield return SellCrateDirect();
                L($"  crate {bought}: sold={st.Stats.SpecimensSold} offers=[{string.Join(",", st.OfferedLots)}] commissions={st.Commissions.Count} unlocked=[{string.Join(",", st.UnlockedSuppliers)}]");
            }
            L($"offer after {bought} crates: {offer ?? "none"} (eligible occasional: {string.Join(",", st.UnlockedSuppliers.FindAll(id => Economy.SupplierCatalog.Get(id).Occasional))})");
            if (offer != null)
            {
                bool ok = S.BuyCrate(offer, out string err2);
                L($"bought offered lot {offer}: {(ok ? "ok" : err2)} offers now=[{string.Join(",", st.OfferedLots)}] locality='{(ok ? st.Crates[st.Crates.Count - 1].Locality : "")}'");
                bool again = S.BuyCrate(offer, out string err3);
                L($"buying it again: {(again ? "allowed (!)" : "refused: " + err3)}");
                yield return new WaitForSeconds(0.6f);
                yield return OpenNewestCrate();
                int chipped = 0; foreach (var r in st.Specimens) if (r.SupplierId == offer && r.ShellDamage > 0.01f) chipped++;
                L($"offered lot rocks: {st.Crates[st.Crates.Count - 1].SpecimenIds.Count}, pre-chipped={chipped}");
            }
            // commissions
            Commission ask = null; foreach (var c in st.Commissions) if (!c.Fulfilled) { ask = c; break; }
            L($"open commission: {(ask != null ? Economy.Market.Describe(ask) : "none")}");
            if (ask != null)
            {
                SpecimenRecord match = null;
                foreach (var r in st.Specimens) if (r.Location == SpecimenLocation.DisplaySlot && Economy.Market.Matches(ask, r)) { match = r; break; }
                if (match == null) foreach (var r in st.Specimens) if ((r.Location == SpecimenLocation.World || r.Location == SpecimenLocation.DisplaySlot) && r.IsOpened && Economy.Market.Matches(ask, r)) { match = r; break; }
                L($"matching piece on hand: {(match != null ? match.DisplayName + " (" + match.Location + ")" : "none")}");
                if (match != null)
                {
                    var ent = S.GetEntity(match.Id);
                    if (ent != null)
                    {
                        if (ent.Zone != null) ent.Zone.Take(ent, true);
                        var outbox = Find<SellOutbox>();
                        match.Favorite = true;
                        L($"favourite refusal: '{outbox.Tray.RefusalReason(ent)}'");
                        match.Favorite = false;
                        outbox.Tray.Place(ent, true);
                        float before = st.Cash;
                        outbox.Ship();
                        L($"shipped with the commission: cash {before} -> {st.Cash} filled={ask.Fulfilled} commissionsFilled={st.Stats.CommissionsFilled} revenue={st.Stats.CommissionRevenue}");
                    }
                }
            }
            L($"goals done: {Economy.CollectionGoals.DoneCount(st)}/{Economy.CollectionGoals.All.Length} nearest gap: {Economy.CollectionGoals.NearestGap(st)}");
            // a favourite is never sold by mistake: the outbox and the sales shelves refuse it
            foreach (var r in st.Specimens)
            {
                if (r.Location != SpecimenLocation.DisplaySlot) continue;
                var ent = S.GetEntity(r.Id); if (ent == null) continue;
                var outbox = Find<SellOutbox>();
                r.Favorite = true;
                L($"favourite {r.DisplayName}: outbox says '{outbox.Tray.RefusalReason(ent)}'");
                r.Favorite = false;
                break;
            }
            L(RunSaveReloadCheck());
            L($"after reload: offers=[{string.Join(",", S.State.OfferedLots)}] commissions={S.State.Commissions.Count} filled={S.State.Stats.CommissionsFilled}");
            Phase = "done";
            Running = false;
        }

        /// <summary>V5 verification: call a rock in the hand (drop key while inspecting), crack it, read the call on the result and the card, check the history.</summary>
        public void RunCallTest() { if (!Running) StartCoroutine(CallTest()); }

        private IEnumerator CallTest()
        {
            Running = true;
            Phase = "call";
            L($"== CallTest cash={S.State.Cash}");
            if (S.Crates.Count == 0) { S.BuyCrate("local", out string err); L("buy: " + (err ?? "ok")); yield return new WaitForSeconds(1.4f); }
            CrateEntity crate = null;
            foreach (var c in S.Crates.Values) if (!c.IsOpened || c.RemainingRocks > 0) { crate = c; break; }
            if (crate == null) { L("no crate"); Running = false; yield break; }
            L($"crate {crate.Record.Id} locality='{crate.Record.Locality}'");
            if (!crate.IsOpened)
            {
                Vector3 cratePos = crate.transform.position;
                Vector3 stand = cratePos + (new Vector3(-0.3f, 0f, 0.6f)).normalized * 1.1f; stand.y = 0f;
                yield return Walk(stand, 0.3f);
                yield return LookAndInteract(cratePos + Vector3.up * 0.2f, "Open crate");
                yield return new WaitForSeconds(0.9f);
            }
            SpecimenEntity rock = null; float best = -1f;
            foreach (var e in S.Entities.Values) { if (e.IsOpened || e.Record.Location != SpecimenLocation.InCrate) continue; float sc = e.Geology.CavityFraction; if (sc > best) { best = sc; rock = e; } }
            if (rock == null) { L("no rock"); Running = false; yield break; }
            yield return FetchRock(rock);
            if (P.Held == null) { L("could not pick up"); Running = false; yield break; }
            rock = P.Held;
            var r = rock.Record;
            L($"rock {r.Id} {rock.Geology.Mineral} {rock.Geology.Cavity} tier={rock.Geology.Tier} locality='{r.Locality}' acquired={r.AcquiredAtTicks > 0} cost={r.AcquisitionCost} origMass={r.OriginalMassKg:F2} history={r.History.Count}");
            // inspect and call it: two presses of the drop key while inspecting = "hollow, good"
            D.SetMouseButton(1, true); yield return new WaitForSeconds(0.4f);
            yield return D.Tap(Key.G, 0.08f); yield return new WaitForSeconds(0.25f);
            yield return D.Tap(Key.G, 0.08f); yield return new WaitForSeconds(0.25f);
            L($"called: predicted={r.Predicted} hollow={r.PredictedHollow} tier={r.PredictedTier} prompt='{P.Prompt}' held={(P.Held != null)}");
            // the tap: knock on the shell while inspecting, read the grade (a thick shell or clay can mislead it)
            D.SetMouseButton(1, true); yield return new WaitForSeconds(0.3f);
            yield return D.ClickHold(0, 0.08f); yield return new WaitForSeconds(0.4f);
            L($"tap: shell={P.Held.Geology.ShellThickness:F2} cavity={P.Held.Geology.CavityFraction:F2} dirt={(P.Held.Visual != null ? P.Held.Visual.DirtRemaining : 0f):F2} prompt='{P.Prompt}'");
            D.SetMouseButton(1, false); yield return new WaitForSeconds(0.2f);
            Snap("call_inspect");
            D.SetMouseButton(1, false); yield return new WaitForSeconds(0.3f);
            if (P.Held == null) { L("the call dropped the rock!"); Running = false; yield break; }
            // crack it
            Vector3 cradle = ZonePos(ZoneKind.Cradle);
            yield return RouteTo(new Vector3(cradle.x, 0f, cradle.z - 0.95f), 0.25f);
            yield return LookAndInteract(cradle, "Set on the cradle");
            yield return new WaitForSeconds(1.0f);
            var bench = Find<CrackingBench>();
            if (!bench.Active) { L("bench not active"); Running = false; yield break; }
            int strikes = 0;
            while (bench.Active && !bench.Opened && !bench.Revealing && strikes < 60)
            {
                yield return AimCursor(bench, bench.SeamCursorHint() + new Vector2(0f, Random.Range(-0.015f, 0.015f)));
                yield return Strike(0.5f);
                strikes++;
                var res = bench.LastResult;
                if (res.WeakBite || res.Damaged) L($"  strike {strikes}: weakBite={res.WeakBite} cause={res.BiteCause} damaged={res.Damaged} dmgCause={res.DamageCause} internal={res.InternalDamage}");
                yield return Rotate(0.42f, 1);
                while (bench.Revealing) yield return null;
            }
            yield return new WaitForSeconds(2.0f);
            L($"opened={bench.Opened} strikes={strikes} note='{bench.ResultNote}' processedBy={r.ProcessedBy}");
            Snap("call_result");
            if (!bench.Opened) { Running = false; yield break; }
            yield return Interact();
            yield return new WaitForSeconds(0.3f);
            Vector3 scale = ZonePos(ZoneKind.Scale);
            yield return RouteTo(StandNear(scale), 0.3f);
            yield return LookAndInteract(scale, "Weigh on the scale");
            yield return new WaitForSeconds(1.8f);
            L($"appraised={r.Appraised} value={r.AppraisedValue} explain=[{string.Join(" | ", Valuation.Explain(r))}]");
            L($"card call: '{UI.AppraisalUI.PredictionLine(r)}'");
            L($"provenance: {UI.TabletUI.Provenance(r)}");
            L($"history:\n{UI.TabletUI.HistoryText(r)}");
            var st = S.State.Stats;
            L($"stats: predictions={st.PredictionsMade} hollowRight={st.HollowCallsRight} tierRight={st.TierCallsRight}");
            Snap("call_appraisal");
            L(RunSaveReloadCheck());
            var r2 = S.State.FindSpecimen(r.Id);
            L($"after reload: predicted={r2.Predicted} locality='{r2.Locality}' history={r2.History.Count}");
            Phase = "done";
            Running = false;
        }

        /// <summary>V5 cracker: buy Stage 2 + the cracker if needed, set a rock on it (tilted first, so it slips), level it, tighten, squeeze until it splits.</summary>
        public void RunCracker(float tiltDeg = 22f) { if (!Running) StartCoroutine(Cracker(tiltDeg)); }

        private IEnumerator Cracker(float tiltDeg)
        {
            Running = true;
            Phase = "cracker";
            var st = FindAnyObjectByType<Cracking.CrackerStation>(FindObjectsInactive.Include);   // it lives under the Stage-2 root, inactive until bought
            if (st == null) { L("no cracker station"); Running = false; yield break; }
            if (!st.Owned)
            {
                if (S.State.Cash < 3000f) S.AddCash(3000f, "test");
                if (!Economy.UpgradeCatalog.Has(S.State, Economy.UpgradeCatalog.TrimSaw)) S.BuyUpgrade(Economy.UpgradeCatalog.TrimSaw, out _);
                if (S.State.WorkshopStage < 2) { S.BuyUpgrade(Economy.UpgradeCatalog.Stage2, out string e2); L("stage 2: " + (e2 ?? "ok")); yield return new WaitForSeconds(1f); }
                S.BuyUpgrade(Economy.UpgradeCatalog.GeodeCracker, out string err);
                L("bought cracker: " + (err ?? "ok") + " owned=" + st.Owned);
                yield return new WaitForSeconds(0.5f);
            }
            L($"== Cracker tilt={tiltDeg} cash={S.State.Cash}");
            if (S.Crates.Count == 0) { S.BuyCrate("local", out _); yield return new WaitForSeconds(1.4f); }
            CrateEntity crate = null;
            foreach (var c in S.Crates.Values) if (!c.IsOpened || c.RemainingRocks > 0) { crate = c; break; }
            if (crate != null && !crate.IsOpened)
            {
                Vector3 cratePos = crate.transform.position;
                Vector3 stand = cratePos + (new Vector3(-0.3f, 0f, 0.6f)).normalized * 1.1f; stand.y = 0f;
                yield return Walk(stand, 0.3f);
                yield return LookAndInteract(cratePos + Vector3.up * 0.2f, "Open crate");
                yield return new WaitForSeconds(0.9f);
            }
            SpecimenEntity rock = null; float best = -1f;
            foreach (var e in S.Entities.Values)
            {
                if (e.IsOpened || e.Record.Location != SpecimenLocation.InCrate || e.Radius > st.MaxRockRadius) continue;
                float score = e.Geology.CavityFraction + (e.Geology.Cavity == CavityArchetype.Nodule ? -1f : 0f);
                if (score > best) { best = score; rock = e; }
            }
            if (rock == null) { L("no rock"); Running = false; yield break; }
            yield return FetchRock(rock);
            if (P.Held == null) { L("could not pick up"); Running = false; yield break; }
            rock = P.Held;
            var g = rock.Geology;
            L($"rock {rock.Id} {g.Mineral} {g.Cavity} size={g.SizeClass} r={rock.Radius:F3} shell={g.ShellThickness:F2} seamQ={g.SeamQuality:F2} tier={g.Tier}");
            Vector3 bed = ZonePos(ZoneKind.Cracker);
            yield return RouteTo(new Vector3(bed.x, 0f, bed.z - 1.0f), 0.25f);
            yield return LookAndInteract(bed, "Set on");
            yield return new WaitForSeconds(0.8f);
            L($"cracker active={st.Active} state={st.State} split@={st.SplitPressure:F2}");
            if (!st.Active) { Running = false; yield break; }
            Snap("cracker_seated");
            // 1. deliberately off level: the chain rides up and slips under pressure
            st.DevSeat(0f, tiltDeg);
            yield return new WaitForSeconds(0.3f);
            L($"tilted {st.TiltAngle:F0} deg ({st.AlignmentWord})");
            Snap("cracker_tilted");
            yield return Interact();   // lay the chain
            D.KeyDown(Key.E);
            float t0 = Time.time;
            while (st.State == Cracking.CrackerStation.Phase.Tighten && Time.time - t0 < 6f) yield return null;
            D.KeyUp(); yield return null;
            L($"tightened: state={st.State} tighten={st.Tighten:F2}");
            D.SetMouseButton(0, true);
            t0 = Time.time;
            while (st.State == Cracking.CrackerStation.Phase.Pressure && Time.time - t0 < 12f) yield return null;
            D.SetMouseButton(0, false); yield return null;
            L($"after squeeze (tilted): state={st.State} slips={st.Slips} pressure={st.Pressure:F2} note='{st.Note}'");
            Snap("cracker_slipped");
            // 2. level it, tighten again, squeeze until it splits
            st.DevSeat(0f, 0f);
            yield return new WaitForSeconds(0.3f);
            L($"levelled {st.TiltAngle:F0} deg ({st.AlignmentWord}) state={st.State}");
            if (st.State == Cracking.CrackerStation.Phase.Seat) yield return Interact();
            D.KeyDown(Key.E);
            t0 = Time.time;
            while (st.State == Cracking.CrackerStation.Phase.Tighten && Time.time - t0 < 6f) yield return null;
            D.KeyUp(); yield return null;
            D.SetMouseButton(0, true);
            t0 = Time.time; bool snapped = false;
            while ((st.State == Cracking.CrackerStation.Phase.Pressure || st.State == Cracking.CrackerStation.Phase.Splitting) && Time.time - t0 < 20f)
            {
                if (!snapped && st.Pressure / Mathf.Max(0.01f, st.SplitPressure) > 0.75f) { snapped = true; Snap("cracker_groaning"); }
                if (st.State == Cracking.CrackerStation.Phase.Splitting) D.SetMouseButton(0, false);
                yield return null;
            }
            D.SetMouseButton(0, false);
            yield return new WaitForSeconds(0.8f);
            L($"split: state={st.State} pressure={st.Pressure:F2}/{st.SplitPressure:F2} opened={rock.IsOpened} processedBy={rock.Record.ProcessedBy} damage={rock.Record.DamageFraction:F2} note='{st.ResultNote}'");
            Snap("cracker_open");
            {
                Vector3 c = rock.transform.position;
                DevDriver.CaptureFrom(c + new Vector3(0.1f, 0.32f, -0.34f), c + Vector3.up * 0.02f, 34f, SnapDir + "/cracker_close.png");
            }
            L(Core.CollisionAudit.Report("cracker open"));
            yield return Interact();
            yield return new WaitForSeconds(0.4f);
            L($"took: held={(P.Held != null ? P.Held.Record.DisplayName : "none")} active={st.Active}");
            L(RunSaveReloadCheck());
            Phase = "done";
            Running = false;
        }

        /// <summary>
        /// V5 prep: the dirtiest rock in the crate goes to the bench caked (seam hidden, strikes land poorly), is tilted on
        /// the cradle (seat quality read from the hull), washed, cracked clean, then rinsed in the tub for the full colour.
        /// </summary>
        public void RunPrepRock(string style = "careful") { if (!Running) StartCoroutine(PrepRock(style)); }

        private IEnumerator PrepRock(string style)
        {
            Running = true;
            Phase = "prep";
            L($"== PrepRock ({style}) cash={S.State.Cash}");
            if (S.Crates.Count == 0) { S.BuyCrate("local", out string err); L("buy: " + (err ?? "ok")); yield return new WaitForSeconds(1.4f); }
            CrateEntity crate = null;
            foreach (var c in S.Crates.Values) if (!c.IsOpened || c.RemainingRocks > 0) { crate = c; break; }
            if (crate == null) { L("no crate"); Running = false; yield break; }
            if (!crate.IsOpened)
            {
                Vector3 cratePos = crate.transform.position;
                Vector3 stand = cratePos + (new Vector3(-0.3f, 0f, 0.6f)).normalized * 1.1f; stand.y = 0f;
                yield return Walk(stand, 0.3f);
                yield return LookAndInteract(cratePos + Vector3.up * 0.2f, "Open crate");
                yield return new WaitForSeconds(0.9f);
            }
            SpecimenEntity rock = null; float dirtiest = -1f;
            foreach (var e in S.Entities.Values) if (!e.IsOpened && e.Record.Location == SpecimenLocation.InCrate && e.Visual.DirtRemaining > dirtiest) { dirtiest = e.Visual.DirtRemaining; rock = e; }
            if (rock == null) { L("no rock"); Running = false; yield break; }
            yield return FetchRock(rock);
            if (P.Held == null) { L("could not pick up the rock"); Running = false; yield break; }
            rock = P.Held;
            var g = rock.Geology;
            L($"rock {rock.Id} {g.Mineral} size={g.SizeClass} ext={g.Exterior} dirt={rock.Visual.DirtRemaining:F2} chip={g.HasNaturalChip}@{g.ChipLatitude:F2} seamQ={g.SeamQuality:F2} axes={g.Axes}");
            D.SetMouseButton(1, true); yield return new WaitForSeconds(0.4f);
            L($"hand (dirty): '{P.Prompt}'");
            D.SetMouseButton(1, false); yield return new WaitForSeconds(0.2f);

            // 1. caked rock at the bench: the seam is hidden, the chisel does not find it
            Vector3 cradle = ZonePos(ZoneKind.Cradle);
            Vector3 benchStand = new Vector3(cradle.x, 0f, cradle.z - 0.95f);
            yield return RouteTo(benchStand, 0.25f);
            yield return LookAndInteract(cradle, "Set on the cradle");
            yield return new WaitForSeconds(1.0f);
            var bench = Find<CrackingBench>();
            if (!bench.Active) { L("bench not active"); Running = false; yield break; }
            L($"bench: clean={bench.Cleanliness:F2} seat={bench.Stability:F2} ({Workshop.Preparation.SeatWord(bench.Stability)}) chipSector={bench.ChipSector} tilt={bench.TiltAngle:F0}");
            Snap("prep_caked_bench");
            float placeSum = 0f; int n = 0;
            for (int i = 0; i < 3 && bench.Active && !bench.Opened; i++)
            {
                yield return AimCursor(bench, bench.SeamCursorHint() + new Vector2(0f, -0.022f));   // a finger below the (hidden) seam
                yield return Strike(0.35f);
                var r = bench.LastResult; placeSum += r.Placement; n++;
                L($"  caked strike {i + 1}: sector={r.Sector} place={r.Placement:F2} added={r.StressAdded:F2} chip={r.SurfaceChip} wobbled={r.Wobbled} slip={r.Slipped}");
                yield return Rotate(0.3f, 1);
                while (bench.Revealing) yield return null;
            }
            L($"caked placement avg={(n > 0 ? placeSum / n : 0f):F2}");
            // 2. seat it: tilt forward, read the seat, tilt back
            float seat0 = bench.Stability, tilt0 = bench.TiltAngle;
            D.KeyDown(Key.W); yield return new WaitForSeconds(0.45f); D.KeyUp(); yield return null;
            L($"tilt W: seat {seat0:F2} -> {bench.Stability:F2} tilt {tilt0:F0} -> {bench.TiltAngle:F0}");
            Snap("prep_tilted");
            D.KeyDown(Key.S); yield return new WaitForSeconds(0.45f); D.KeyUp(); yield return null;
            L($"tilt S: seat={bench.Stability:F2} tilt={bench.TiltAngle:F0}");
            D.KeyDown(Key.A); yield return new WaitForSeconds(0.3f); D.KeyUp(); yield return null;
            L($"tilt A: seat={bench.Stability:F2} tilt={bench.TiltAngle:F0}");
            // the support pad: a shim under the low side takes a rocking rock to a workable seat (forced here: this rock may already sit firm)
            float seatBefore = bench.Stability;
            bench.PlaceShim();
            yield return new WaitForSeconds(0.3f);
            L($"shim: seat {seatBefore:F2} -> {bench.Stability:F2} shimmed={bench.Shimmed} wedge={(GameObject.Find("Shim") != null && GameObject.Find("Shim").activeInHierarchy)}");
            Snap("prep_shimmed");
            L(Core.CollisionAudit.Report("tilted on cradle"));
            // 3. leave with the rock, wash it, come back
            if (bench.Active) { if (UseGamepad) yield return D.PadTap(GamepadButton.East, 0.1f); else yield return D.Tap(Key.Escape, 0.1f); }
            yield return new WaitForSeconds(0.3f);
            if (CursorController.InMenu) { if (UseGamepad) yield return D.PadTap(GamepadButton.Start, 0.1f); else yield return D.Tap(Key.Escape, 0.1f); yield return new WaitForSeconds(0.3f); }
            yield return LookAndInteract(cradle, "Pick up");
            L($"left bench: held={(P.Held != null ? P.Held.Id : "none")} active={bench.Active}");
            if (P.Held == null) { Running = false; yield break; }
            Vector3 tub = ZonePos(ZoneKind.Wash);
            yield return RouteTo(new Vector3(tub.x, 0f, tub.z - 0.9f), 0.25f);
            yield return LookAndInteract(tub, "Dunk in");
            yield return new WaitForSeconds(0.4f);
            D.LookAt(tub + Vector3.up * 0.05f); yield return new WaitForSeconds(0.2f);
            D.KeyDown(Key.E);
            float t0 = Time.time;
            while (rock.Visual.DirtRemaining > 0.02f && Time.time - t0 < 8f) yield return null;
            D.KeyUp(); yield return new WaitForSeconds(0.3f);
            L($"washed in {Time.time - t0:F1}s dirt={rock.Visual.DirtRemaining:F2}");
            yield return LookAndInteract(tub, "Take");
            D.SetMouseButton(1, true); yield return new WaitForSeconds(0.4f);
            L($"hand (clean): '{P.Prompt}'");
            D.SetMouseButton(1, false); yield return new WaitForSeconds(0.2f);
            yield return RouteTo(benchStand, 0.25f);
            yield return LookAndInteract(cradle, "Set on the cradle");
            yield return new WaitForSeconds(1.0f);
            L($"bench again: clean={bench.Cleanliness:F2} seat={bench.Stability:F2} ({Workshop.Preparation.SeatWord(bench.Stability)}) stressAtChip={(bench.ChipSector >= 0 ? bench.Model.Stress[bench.ChipSector] : -1f):F2}");
            Snap("prep_clean_bench");
            // 4. crack it
            int strikes = 0; placeSum = 0f; n = 0;
            float hold = style == "careless" ? 1.0f : 0.5f;
            while (bench.Active && !bench.Opened && !bench.Revealing && strikes < 60)
            {
                var hint = bench.SeamCursorHint() + new Vector2(0f, strikes < 3 ? -0.022f : Random.Range(-0.015f, 0.015f));   // the first few aim as low as the caked ones did
                yield return AimCursor(bench, hint);
                yield return Strike(hold);
                strikes++;
                var r = bench.LastResult; placeSum += r.Placement; n++;
                if (strikes <= 3 || r.Opened) L($"  clean strike {strikes}: sector={r.Sector} place={r.Placement:F2} added={r.StressAdded:F2} cracks={r.CracksTotal} wobbled={r.Wobbled} open={r.Opened}");
                yield return Rotate(0.42f, 1);
                while (bench.Revealing) yield return null;
            }
            yield return new WaitForSeconds(2.0f);
            L($"opened={bench.Opened} strikes={strikes} placement avg={(n > 0 ? placeSum / n : 0f):F2} note='{bench.ResultNote}' dust={rock.Visual.Dust:F0}");
            Snap("prep_opened_dusty");
            {
                Vector3 c = rock.transform.position;
                DevDriver.CaptureFrom(c + new Vector3(0.12f, 0.32f, -0.3f), c + Vector3.up * 0.03f, 34f, SnapDir + "/prep_close_dusty.png");
            }
            if (!bench.Opened) { Running = false; yield break; }
            yield return Interact();
            yield return new WaitForSeconds(0.3f);
            // 5. rinse the opened rock
            yield return RouteTo(new Vector3(tub.x, 0f, tub.z - 0.9f), 0.25f);
            yield return LookAndInteract(tub, "Dunk in");
            yield return new WaitForSeconds(0.6f);
            L($"rinsed={rock.Record.Condition.Rinsed} dust={rock.Visual.Dust:F0} wet={rock.Visual.Wetness:F2} loc={rock.Record.Location} pose ok={(rock.Zone != null)}");
            {
                Vector3 c = rock.transform.position;
                DevDriver.CaptureFrom(c + new Vector3(0.04f, 0.5f, -0.16f), c + Vector3.up * 0.03f, 34f, SnapDir + "/prep_close_rinsed.png");   // over the sink rim, looking down
            }
            D.LookAt(tub + Vector3.up * 0.05f); yield return new WaitForSeconds(0.3f);
            Snap("prep_rinsed");
            L(Core.CollisionAudit.Report("rinsed in tub"));
            yield return LookAndInteract(tub, "Take");
            L($"held after rinse={(P.Held != null ? P.Held.Record.DisplayName : "none")}");
            Phase = "done";
            Running = false;
        }

        /// <summary>
        /// V4 saw: buy the saw if needed, take an unopened rock to it, clamp, set a plan (yaw/roll/offset), commit, feed
        /// through (fast when asked), read the pieces, take the better one to the scale. Logs times, chips, wear, values.
        /// </summary>
        public void RunSawCut(float yaw = 0f, float roll = 0f, float offset = 0f, bool fast = false, bool tall = false, bool dry = false) { if (!Running) StartCoroutine(SawCut(yaw, roll, offset, fast, tall, dry)); }

        private IEnumerator SawCut(float yaw, float roll, float offset, bool fast, bool tall = false, bool dry = false)
        {
            Running = true;
            Phase = "saw";
            var saw = Find<Lapidary.SawStation>();
            if (saw == null) { L("no saw station"); Running = false; yield break; }
            if (!saw.Owned)
            {
                if (S.State.Cash < 700f) S.AddCash(700f, "test");
                S.BuyUpgrade(Economy.UpgradeCatalog.TrimSaw, out string err);
                L("bought saw: " + (err ?? "ok") + " owned=" + saw.Owned);
                yield return null;
            }
            L($"== SawCut yaw={yaw} roll={roll} offset={offset} fast={fast} tall={tall} dry={dry} cash={S.State.Cash} blade={S.State.BladeWear:F2}");
            if (S.Crates.Count == 0) { S.BuyCrate("local", out string err2); yield return new WaitForSeconds(1.4f); }
            CrateEntity crate = null;
            foreach (var c in S.Crates.Values) if (!c.IsOpened || c.RemainingRocks > 0) { crate = c; break; }
            if (crate != null && !crate.IsOpened)
            {
                Vector3 cratePos = crate.transform.position;
                Vector3 stand = cratePos + (new Vector3(-0.3f, 0f, 0.6f)).normalized * 1.1f; stand.y = 0f;
                yield return Walk(stand, 0.3f);
                yield return LookAndInteract(cratePos + Vector3.up * 0.2f, "Open crate");
                yield return new WaitForSeconds(0.9f);
            }
            // a medium or small unopened rock with a decent cavity, for a readable cut (or the tallest, for two passes)
            SpecimenEntity rock = null; float best = -1f;
            foreach (var e in S.Entities.Values)
            {
                if (e.IsOpened || e.Record.Location != SpecimenLocation.InCrate) continue;
                e.HullBoundsFor(Quaternion.identity, out var mn, out var mx);
                float height = mx.y - mn.y;
                if (tall)
                {
                    if (height <= saw.MaxPassHeight || height > 2f * saw.MaxPassHeight) continue;
                    float scoreT = height;
                    if (scoreT > best) { best = scoreT; rock = e; }
                    continue;
                }
                if (e.Geology.SizeClass == SizeClass.Oversized || e.Geology.SizeClass == SizeClass.Large || height > saw.MaxPassHeight) continue;
                float score = e.Geology.CavityFraction + (e.Geology.Tier >= QualityTier.Decent ? 0.3f : 0f);
                if (score > best) { best = score; rock = e; }
            }
            if (tall)
            {
                // a tall rock is staged on its own on the floor by the saw, clear of the crate crowd, so the pick-up is certain
                if (rock != null) { L($"tall candidate in the crate: {rock.Id} (staging a clean one instead)"); rock = null; }
                Vector3 spot = ZonePos(ZoneKind.Saw) + new Vector3(-0.5f, 0f, -1.1f); spot.y = 0f;
                for (ulong seed = 5000; seed < 9000 && rock == null; seed++)
                {
                    var g = SpecimenGenerator.Generate(seed);
                    if (g.SizeClass != SizeClass.Large || g.Cavity == CavityArchetype.Nodule) continue;
                    var rec = S.CreateSpecimenRecord(seed, "local", "");
                    rec.Location = SpecimenLocation.World;
                    var ent = S.Spawn(rec, spot + Vector3.up * 0.2f, Quaternion.identity, false);
                    ent.HullBoundsFor(Quaternion.identity, out var mn, out var mx);
                    if (mx.y - mn.y > saw.MaxPassHeight && mx.y - mn.y < 2f * saw.MaxPassHeight)
                    {
                        rock = ent;
                        ent.SetPose(spot + Vector3.up * ent.RestHeightOffset(false), Quaternion.identity);
                        ent.SetStaticCollidable();
                        rec.WorldPosition = ent.transform.position; rec.WorldRotation = ent.transform.rotation;
                        L($"staged tall rock {rec.Id} {g.Mineral} {g.Cavity} height={(mx.y - mn.y):F3}");
                    }
                    else { S.Despawn(ent); S.State.Specimens.Remove(rec); }   // a candidate that was never staged leaves no record behind (a reload would spawn it)
                }
            }
            if (rock == null) { L("no suitable rock"); Running = false; yield break; }
            yield return FetchRock(rock);
            if (P.Held != null && P.Held != rock)
            {
                // grabbed a neighbour in the crowd: put it back down and try once more
                L($"picked a neighbour {P.Held.Id}: dropping it and retrying");
                if (UseGamepad) yield return D.PadTap(GamepadButton.West, 0.1f); else yield return D.Tap(Key.G, 0.1f);
                yield return new WaitForSeconds(0.4f);
                yield return FetchRock(rock);
            }
            if (P.Held == null) { L("could not pick up"); Running = false; yield break; }
            if (P.Held != rock) L($"note: cutting {P.Held.Id} instead of {rock.Id}");
            rock = P.Held;
            L($"rock {rock.Id} {rock.Geology.Mineral} {rock.Geology.Cavity} size={rock.Geology.SizeClass} r={rock.Radius:F3} tier={rock.Geology.Tier} base=${rock.Geology.BaseValue}");
            Vector3 clamp = ZonePos(ZoneKind.Saw);
            yield return RouteTo(new Vector3(clamp.x - 0.15f, 0f, clamp.z - 0.95f), 0.25f);
            { string why = saw.Clamp.RefusalReason(rock); if (why != null) L($"clamp refuses: '{why}' (lowest pose height {Lapidary.SawStation.LowestHeightOverPoses(rock) * 100f:F1} cm)"); }
            yield return LookAndInteract(clamp, "Clamp in");
            yield return new WaitForSeconds(0.8f);
            L($"saw active={saw.Active} state={saw.State} rock={(saw.Rock != null ? saw.Rock.Id : "none")} height={saw.RockHeight:F3} maxPass={saw.MaxPassHeight:F3} fits={saw.FitsUnderArbor} grip={saw.Grip:F2} prompt='{P.Prompt}'");
            if (!saw.Active) { Probe("saw"); L("not clamped (refused?)"); Running = false; yield break; }
            Snap("saw_clamped");
            // the coolant valve: open it the way a player does (the drop key), unless this is a deliberately dry cut
            if (saw.CoolantOpen == dry) { if (UseGamepad) yield return D.PadTap(GamepadButton.West, 0.1f); else yield return D.Tap(Key.G, 0.1f); yield return new WaitForSeconds(0.2f); }
            L($"coolant={saw.CoolantWord}");
            // set the plan through the real inputs where cheap (a few yaw taps), then exactly
            yield return Rotate(0.2f, 1);
            saw.SetPlan(yaw, roll, offset);
            yield return new WaitForSeconds(0.3f);
            if (!saw.FitsUnderArbor)
            {
                // too tall as it lies: try turning it flatter, the way a player would
                foreach (float r in new[] { 30f, 60f, 90f, -30f, -60f, -90f })
                {
                    saw.SetPlan(yaw, r, offset);
                    L($"  too tall ({saw.RockHeight * 100f:F1} cm): tilt {r} -> {saw.RockHeight * 100f:F1} cm fits={saw.FitsUnderArbor}");
                    if (saw.FitsUnderArbor) break;
                }
                if (!saw.FitsUnderArbor)
                {
                    Snap("saw_too_tall");
                    yield return Interact();
                    yield return new WaitForSeconds(0.3f);
                    L($"commit refused as expected: state={saw.State} committed={saw.Committed}");
                    if (UseGamepad) yield return D.PadTap(GamepadButton.East, 0.1f); else yield return D.Tap(Key.Escape, 0.1f);
                    yield return new WaitForSeconds(0.3f);
                    if (CursorController.InMenu) { if (UseGamepad) yield return D.PadTap(GamepadButton.Start, 0.1f); else yield return D.Tap(Key.Escape, 0.1f); yield return new WaitForSeconds(0.3f); }
                    Phase = "done"; Running = false; yield break;
                }
            }
            saw.Estimate(out float secs, out float wear, out float cost);
            saw.PlanInRockFrame(out var n, out float h);
            L($"plan yaw={saw.Yaw} roll={saw.Roll} offset={saw.Offset * 1000f:F0}mm normal={n:F2} h={h * 1000f:F1}mm estimate {secs:F0}s wear={wear:F3} ${cost:F1}");
            Snap("saw_plan");
            yield return Interact();
            yield return new WaitForSeconds(0.5f);
            L($"committed={saw.Committed} state={saw.State} saved cut={rock.Record.CutCommitted}");
            saw.DevFeed = true; saw.DevFast = fast;
            float t0 = Time.time; float maxLoad = 0f; bool snapped = false;
            int probeAt = 1;
            while (saw.State == Lapidary.SawStation.Phase.Cutting && Time.time - t0 < 120f)
            {
                maxLoad = Mathf.Max(maxLoad, saw.Load);
                if (saw.Progress > probeAt * 0.25f && probeAt <= 3) { L($"  probe {saw.Probe()}"); probeAt++; }
                if (!snapped && saw.Progress > 0.45f)
                {
                    snapped = true; Snap("saw_cutting");
                    Vector3 c = saw.Rock.transform.position;
                    DevDriver.CaptureFrom(c + new Vector3(0.05f, 0.28f, -0.42f), c + Vector3.up * 0.02f, 32f, SnapDir + "/saw_close_cut.png");
                    DevDriver.CaptureFrom(c + new Vector3(0.02f, 0.5f, -0.06f), c, 30f, SnapDir + "/saw_close_top.png");
                }
                yield return null;
            }
            saw.DevFeed = false; saw.DevFast = false;
            float tw = Time.time;
            while (string.IsNullOrEmpty(saw.ResultNote) && Time.time - tw < 4f) yield return null;
            yield return new WaitForSeconds(0.3f);
            L($"cut done in {Time.time - t0:F1}s state={saw.State} maxLoad={maxLoad:F2} chips={saw.ChipsThisCut} wear={saw.WearThisCut:F3} blade={S.State.BladeWear:F2} faceStep={saw.FaceStep * 1000f:F1}mm note='{saw.ResultNote}'");
            Snap("saw_result");
            var a = saw.PieceA; var b = saw.PieceB;
            if (a != null) L($"piece A {a.Id} {a.Record.DisplayName} value=${a.Record.PristineForSale()} retained={a.Record.PieceRetained:F2} opening={a.Record.PieceOpening:F2} sym={a.Record.PieceSymmetry:F2} face={a.Record.PieceFaceArea:F2} loc={a.Record.Location}");
            if (b != null) L($"piece B {b.Id} {b.Record.DisplayName} value=${b.Record.PristineForSale()} retained={b.Record.PieceRetained:F2} opening={b.Record.PieceOpening:F2} loc={b.Record.Location}");
            var parentRec = S.State.FindSpecimen(rock.Record.Id);
            L($"parent loc={(parentRec != null ? parentRec.Location.ToString() : "missing")} entity={(S.GetEntity(rock.Record.Id) != null)}");
            L(Core.CollisionAudit.Report("saw pieces"));
            yield return Interact();
            yield return new WaitForSeconds(0.5f);
            L($"took piece: held={(P.Held != null ? P.Held.Id : "none")} sawActive={saw.Active}");
            if (P.Held != null)
            {
                Vector3 scale = ZonePos(ZoneKind.Scale);
                yield return RouteTo(StandNear(scale), 0.3f);
                yield return LookAndInteract(scale, "Weigh on the scale");
                yield return new WaitForSeconds(1.6f);
                var ap = Find<AppraisalStation>();
                L("appraised piece=" + (ap.Current != null && ap.Current.Record.Appraised) + " value=" + (ap.Current != null ? ap.Current.Record.AppraisedValue.ToString() : "") + " name=" + (ap.Current != null ? ap.Current.Record.DisplayName : ""));
                Snap("saw_piece_appraisal");
                // clear the scale so the next run can use it: the piece goes to the dealer outbox
                yield return LookAndInteract(scale, "Take");
                if (P.Held != null)
                {
                    Vector3 tray = ZonePos(ZoneKind.SellTray);
                    yield return RouteTo(StandNear(tray), 0.3f);
                    yield return LookAndInteract(tray, "Place in the dealer outbox");
                }
            }
            L(RunSaveReloadCheck());
            Phase = "done";
            Running = false;
        }

        /// <summary>
        /// V4 saw persistence and pad: clamp a rock through the real inputs (pad if UseGamepad), turn/tilt/slide it with
        /// the sticks and bumpers, commit, feed to ~40%, save and reload mid-cut, check the cut resumes at the same
        /// plane and depth, finish it, then reload again and check both pieces exist once and the parent is gone.
        /// </summary>
        public void RunSawPersistence() { if (!Running) StartCoroutine(SawPersistence()); }

        private IEnumerator SawPersistence()
        {
            Running = true;
            Phase = "saw-persist";
            var saw = Find<Lapidary.SawStation>();
            if (!saw.Owned) { if (S.State.Cash < 700f) S.AddCash(700f, "test"); S.BuyUpgrade(Economy.UpgradeCatalog.TrimSaw, out _); yield return null; }
            L($"== SawPersistence pad={UseGamepad} cash={S.State.Cash}");
            if (S.Crates.Count == 0) { S.BuyCrate("local", out _); yield return new WaitForSeconds(1.4f); }
            CrateEntity crate = null;
            foreach (var c in S.Crates.Values) if (!c.IsOpened || c.RemainingRocks > 0) { crate = c; break; }
            if (crate != null && !crate.IsOpened)
            {
                Vector3 cratePos = crate.transform.position;
                Vector3 stand = cratePos + (new Vector3(-0.3f, 0f, 0.6f)).normalized * 1.1f; stand.y = 0f;
                yield return Walk(stand, 0.3f);
                yield return LookAndInteract(cratePos + Vector3.up * 0.2f, "Open crate");
                yield return new WaitForSeconds(0.9f);
            }
            SpecimenEntity rock = null;
            foreach (var e in S.Entities.Values) if (!e.IsOpened && e.Record.Location == SpecimenLocation.InCrate && (int)e.Geology.SizeClass <= 1) { rock = e; break; }
            if (rock == null) { L("no suitable rock"); Running = false; yield break; }
            yield return FetchRock(rock);
            if (P.Held == null) { L("could not pick up"); Running = false; yield break; }
            rock = P.Held;
            Vector3 clamp = ZonePos(ZoneKind.Saw);
            yield return RouteTo(new Vector3(clamp.x - 0.15f, 0f, clamp.z - 0.95f), 0.25f);
            yield return LookAndInteract(clamp, "Clamp in");
            yield return new WaitForSeconds(1.2f);
            if (!saw.Active) { L("saw did not take the rock"); Probe("saw"); Running = false; yield break; }
            L($"in saw: active={saw.Active} state={saw.State} canRotate={saw.CanRotate}");
            // orient through the real inputs: yaw (bumpers / Q,R), tilt (right stick / mouse), across (left stick / A,D)
            float y0 = saw.Yaw, r0 = saw.Roll, o0 = saw.Offset;
            if (UseGamepad)
            {
                D.PadState(Vector2.zero, Vector2.zero, 0f, 0f, GamepadButton.RightShoulder); yield return new WaitForSeconds(0.35f);
                D.PadState(Vector2.zero, new Vector2(0f, 1f), 0f, 0f); yield return new WaitForSeconds(0.35f);
                D.PadState(new Vector2(1f, 0f), Vector2.zero, 0f, 0f); yield return new WaitForSeconds(0.35f);
                D.PadState(Vector2.zero, Vector2.zero, 0f, 0f);
            }
            else
            {
                D.KeyDown(Key.R); yield return new WaitForSeconds(0.3f); D.KeyUp();
                yield return new WaitForSeconds(0.15f);
                for (int i = 0; i < 14; i++) { D.MouseDelta(0f, 40f); yield return null; }
                yield return new WaitForSeconds(0.15f);
                D.KeyDown(Key.D); yield return new WaitForSeconds(0.3f); D.KeyUp();
            }
            yield return new WaitForSeconds(0.2f);
            L($"inputs moved the plan: yaw {y0}->{saw.Yaw}  roll {r0}->{saw.Roll}  offset {o0 * 1000f:F0}->{saw.Offset * 1000f:F0}mm");
            Snap("sawp_plan");
            yield return Interact();
            yield return new WaitForSeconds(0.5f);
            saw.PlanInRockFrame(out var n1, out float h1);
            L($"committed={saw.Committed} plan n={n1:F3} h={h1 * 1000f:F1}mm saved={rock.Record.CutCommitted}");
            // feed to 40% through the real button
            if (UseGamepad) D.PadState(Vector2.zero, Vector2.zero, 0f, 1f); else D.SetMouseButton(0, true);
            float t0 = Time.time;
            while (saw.Progress < 0.4f && Time.time - t0 < 40f) yield return null;
            if (UseGamepad) D.PadState(Vector2.zero, Vector2.zero, 0f, 0f); else D.SetMouseButton(0, false);
            yield return new WaitForSeconds(0.4f);
            float pBefore = saw.Progress;
            L($"fed to {pBefore:P0} in {Time.time - t0:F1}s feeding={saw.Feeding} load={saw.Load:F2}");
            Snap("sawp_midcut");
            // step away (Back), save, reload, come back
            if (UseGamepad) yield return D.PadTap(GamepadButton.East, 0.1f); else yield return D.Tap(Key.Escape, 0.1f);
            yield return new WaitForSeconds(0.4f);
            L($"stepped away: active={saw.Active} rockLoc={rock.Record.Location} progress saved={rock.Record.CutProgress:P0}");
            S.FlushSave("test");
            S.ContinueGame();
            yield return new WaitForSeconds(0.8f);
            var back = S.GetEntity(rock.Id);
            L($"after reload: entity={(back != null)} loc={(back != null ? back.Record.Location.ToString() : "-")} committed={(back != null && back.Record.CutCommitted)} progress={(back != null ? back.Record.CutProgress : 0f):P0} clampHas={(saw.Clamp.First != null ? saw.Clamp.First.Id : "none")}");
            // re-enter and finish
            yield return RouteTo(new Vector3(clamp.x - 0.15f, 0f, clamp.z - 0.95f), 0.25f);
            D.LookAt(clamp);
            yield return new WaitForSeconds(0.3f);
            L($"prompt at clamp='{P.Prompt}' sawCan={saw.CanInteract(P)} occ={(saw.Clamp.First != null ? saw.Clamp.First.Id : "none")} occLocked={(saw.Clamp.First != null && saw.Clamp.First.Locked)} owned={saw.Owned} active={saw.Active} machine={saw.Machine.activeInHierarchy} teaser={saw.Teaser.activeInHierarchy} cam={P.Cam.transform.position:F2} fwd={P.Cam.transform.forward:F2} player={D.Controller.transform.position:F2}");
            foreach (var col in saw.Machine.GetComponentsInChildren<Collider>(true)) L($"     machine collider {col.name} enabled={col.enabled} active={col.gameObject.activeInHierarchy} bounds={col.bounds.center:F2}/{col.bounds.size:F2}");
            ProbeAll("clamp");
            yield return Interact();
            yield return new WaitForSeconds(0.6f);
            saw.PlanInRockFrame(out var n2, out float h2);
            L($"resumed: active={saw.Active} state={saw.State} progress={saw.Progress:P0} plan n={n2:F3} h={h2 * 1000f:F1}mm same={(Vector3.Distance(n1, n2) < 0.01f && Mathf.Abs(h1 - h2) < 0.0005f && Mathf.Abs(saw.Progress - pBefore) < 0.02f)}");
            if (!saw.Active) { L("resume failed; stopping here"); Phase = "done"; Running = false; yield break; }
            saw.DevFeed = true;
            t0 = Time.time;
            while (saw.Active && saw.State == Lapidary.SawStation.Phase.Cutting && Time.time - t0 < 60f) yield return null;
            saw.DevFeed = false;
            yield return new WaitForSeconds(1.2f);
            string aId = saw.PieceA != null ? saw.PieceA.Id : "none", bId = saw.PieceB != null ? saw.PieceB.Id : "none";
            L($"finished: state={saw.State} pieces {aId} {bId} note='{saw.ResultNote}'");
            if (saw.Active) { if (UseGamepad) yield return D.PadTap(GamepadButton.East, 0.1f); else yield return D.Tap(Key.Escape, 0.1f); }
            yield return new WaitForSeconds(0.4f);
            if (CursorController.InMenu) { L("  pause menu opened by the Back press: closing it"); if (UseGamepad) yield return D.PadTap(GamepadButton.Start, 0.1f); else yield return D.Tap(Key.Escape, 0.1f); yield return new WaitForSeconds(0.3f); }
            S.FlushSave("test");
            S.ContinueGame();
            yield return new WaitForSeconds(0.8f);
            int copiesA = 0, copiesB = 0, parents = 0;
            foreach (var r in S.State.Specimens) { if (r.Id == aId) copiesA++; if (r.Id == bId) copiesB++; if (r.Id == rock.Id) parents++; }
            var pa = S.State.FindSpecimen(aId); var pb = S.State.FindSpecimen(bId); var pp = S.State.FindSpecimen(rock.Id);
            L($"after second reload: A records={copiesA} entity={(S.GetEntity(aId) != null)} loc={(pa != null ? pa.Location.ToString() : "-")}  B records={copiesB} entity={(S.GetEntity(bId) != null)} loc={(pb != null ? pb.Location.ToString() : "-")}  parent records={parents} loc={(pp != null ? pp.Location.ToString() : "-")} entity={(S.GetEntity(rock.Id) != null)}");
            L(Core.CollisionAudit.Report("saw persistence end"));
            Phase = "done";
            Running = false;
        }

        /// <summary>
        /// V4 polish: take the best sawn piece in the workshop (cut one first with RunSawCut), buy the lap, set the piece
        /// face-down, hold the button and sweep it about until it is polished; log the value before and after, appraise,
        /// then save/reload and check the finish stuck.
        /// </summary>
        public void RunPolish() { if (!Running) StartCoroutine(Polish()); }
        public void RunStage2() { if (!Running) StartCoroutine(Stage2()); }
        private PlacementZone FreeDisplaySlot()
        {
            PlacementZone best = null;
            foreach (var z in FindObjectsByType<PlacementZone>(FindObjectsInactive.Exclude))
                if (z.Kind == ZoneKind.DisplaySlot && !z.Locked && z.IsEmpty && (best == null || z.SlotIndex < best.SlotIndex)) best = z;
            return best;
        }

        public void RunCareer(string style, float minutes = 12f, bool boost = false) { if (!Running) StartCoroutine(Career(style, minutes, boost)); }

        /// <summary>
        /// A clean-start career in one style for a while: hammer (A), saw (B), collector (C), seller (D), poorsaw (E),
        /// mixed (F), saveheavy (G: reload every cycle), controller (H: pad input). Each cycle buys a crate the style
        /// would choose, processes it with the tools it favours, sells, restocks and buys upgrades in its own order.
        /// "boost" grants a lump of cash after the first cycle so the saw/Stage-2 paths run inside a short window.
        /// </summary>
        private IEnumerator Career(string style, float minutes, bool boost)
        {
            Running = true;
            float t0 = Time.time;
            int cycle = 0, reloads = 0, cuts = 0, polishes = 0, sawRefusals = 0;
            bool boosted = false;
            _walkRescues = 0;
            D.Dodges = 0;
            UseGamepad = style == "controller";
            string crackStyle = style == "careless" ? "careless" : "careful";
            L($"== Career {style} {minutes:F0} min boost={boost} pad={UseGamepad} cash={S.State.Cash}");
            yield return FirstCrate(crackStyle);
            Running = true;
            while (Time.time - t0 < minutes * 60f)
            {
                cycle++;
                Phase = $"career {style} {cycle}";
                if (boost && cycle == 2 && !boosted) { boosted = true; S.AddCash(2400f, "test"); L($"  boost: +$2400 -> {S.State.Cash}"); }
                yield return CareerUpgrades(style);
                Running = true;
                // saw work first, while the crate is still full of candidates
                if (S.State.HasUpgrade(Economy.UpgradeCatalog.TrimSaw) && style != "hammer" && style != "careless")
                {
                    int want = style == "saw" || style == "poorsaw" ? 3 : 1;
                    for (int i = 0; i < want; i++)
                    {
                        yield return SawOne(style == "poorsaw" ? "poor" : "good");
                        Running = true;
                        if (_sawResult == 1) cuts++; else if (_sawResult == -1) sawRefusals++; else break;
                    }
                }
                yield return CrackAllCore(crackStyle);
                Running = true;
                if (S.State.HasUpgrade(Economy.UpgradeCatalog.PolishLap) && style != "hammer")
                {
                    yield return Polish();
                    Running = true;
                    polishes++;
                }
                yield return SellOutbox();
                Running = true;
                yield return RetailCycle(style == "seller" ? 3 : 1);
                Running = true;
                yield return BreakDownEmptyCrates();
                Running = true;
                if (style == "saveheavy")
                {
                    S.FlushSave("test");
                    S.ContinueGame();
                    yield return new WaitForSeconds(1.0f);
                    reloads++;
                    L($"  reload {reloads}: {DuplicateCheck()} {WorldSummary()}");
                }
                L($"-- {style} cycle {cycle} at {(Time.time - t0) / 60f:F1} min: cash={S.State.Cash} opened={S.State.Stats.SpecimensOpened} cuts={S.State.Stats.SawCuts} polished={S.State.Stats.PiecesPolished} kept={S.State.DisplayedCount()} forSale={S.State.ForSaleCount()} retail={S.State.Stats.RetailSales} upgrades=[{string.Join(",", S.State.Upgrades)}] stage={S.State.WorkshopStage} families={S.State.Encyclopedia.Count} prestige={S.State.Prestige}");
                if (Time.time - t0 >= minutes * 60f) break;
                string sup = ChooseSupplier(style);
                if (sup == null && boost && !boosted) { boosted = true; S.AddCash(2400f, "test"); L($"  boost: +$2400 -> {S.State.Cash}"); sup = ChooseSupplier(style); }
                string err = null;
                if (sup == null || !S.BuyCrate(sup, out err)) { L($"  cannot buy a crate ({sup}: {err ?? "none affordable"}) cash={S.State.Cash} forSale={S.State.ForSaleCount()} outbox={S.State.Stats.SpecimensSold}"); break; }
                L($"  bought {sup} cash={S.State.Cash}");
                yield return new WaitForSeconds(1.5f);
                yield return OpenNewestCrate();
            }
            L($"== Career {style} end: cycles={cycle} minutes={(Time.time - t0) / 60f:F1} cash={S.State.Cash} opened={S.State.Stats.SpecimensOpened} cuts={cuts}/{S.State.Stats.SawCuts} sawRefusals={sawRefusals} polishes={polishes} kept={S.State.DisplayedCount()} sold={S.State.Stats.SpecimensSold} retail={S.State.Stats.RetailSales} upgrades={S.State.Upgrades.Count} stage={S.State.WorkshopStage} families={S.State.Encyclopedia.Count} reloads={reloads} walkRescues={_walkRescues} walkDodges={D.Dodges} {DuplicateCheck()}");
            L(Core.CollisionAudit.Report($"career {style} end"));
            UseGamepad = false;
            Phase = "done";
            Running = false;
        }

        private static readonly Dictionary<string, string[]> CareerOrders = new Dictionary<string, string[]>
        {
            ["hammer"] = new[] { Economy.UpgradeCatalog.Loupe, Economy.UpgradeCatalog.InspectionLamp, Economy.UpgradeCatalog.BenchClamp, Economy.UpgradeCatalog.FineChisel, Economy.UpgradeCatalog.CalibratedScale, Economy.UpgradeCatalog.HeavyCradle, Economy.UpgradeCatalog.Wedge, Economy.UpgradeCatalog.DisplayExpansion, Economy.UpgradeCatalog.SalesTable },
            ["careless"] = new[] { Economy.UpgradeCatalog.BenchClamp, Economy.UpgradeCatalog.InspectionLamp, Economy.UpgradeCatalog.FineChisel, Economy.UpgradeCatalog.Loupe },
            ["saw"] = new[] { Economy.UpgradeCatalog.TrimSaw, Economy.UpgradeCatalog.ThinBlade, Economy.UpgradeCatalog.CoolantPump, Economy.UpgradeCatalog.Stage2, Economy.UpgradeCatalog.PolishLap, Economy.UpgradeCatalog.Loupe, Economy.UpgradeCatalog.CalibratedScale },
            ["poorsaw"] = new[] { Economy.UpgradeCatalog.TrimSaw, Economy.UpgradeCatalog.Stage2, Economy.UpgradeCatalog.PolishLap },
            ["collector"] = new[] { Economy.UpgradeCatalog.Loupe, Economy.UpgradeCatalog.InspectionLamp, Economy.UpgradeCatalog.DisplayExpansion, Economy.UpgradeCatalog.FineChisel, Economy.UpgradeCatalog.CalibratedScale, Economy.UpgradeCatalog.TrimSaw, Economy.UpgradeCatalog.Stage2 },
            ["seller"] = new[] { Economy.UpgradeCatalog.SalesTable, Economy.UpgradeCatalog.CalibratedScale, Economy.UpgradeCatalog.Loupe, Economy.UpgradeCatalog.TrimSaw, Economy.UpgradeCatalog.Stage2, Economy.UpgradeCatalog.PolishLap },
            ["saveheavy"] = new[] { Economy.UpgradeCatalog.TrimSaw, Economy.UpgradeCatalog.Stage2, Economy.UpgradeCatalog.PolishLap, Economy.UpgradeCatalog.Loupe },
            ["mixed"] = new[] { Economy.UpgradeCatalog.Loupe, Economy.UpgradeCatalog.InspectionLamp, Economy.UpgradeCatalog.BenchClamp, Economy.UpgradeCatalog.TrimSaw, Economy.UpgradeCatalog.FineChisel, Economy.UpgradeCatalog.CalibratedScale, Economy.UpgradeCatalog.SalesTable, Economy.UpgradeCatalog.Stage2, Economy.UpgradeCatalog.PolishLap },
        };

        private IEnumerator CareerUpgrades(string style)
        {
            var order = CareerOrders.TryGetValue(style, out var o) ? o : CareerOrders["mixed"];
            foreach (var id in order)
            {
                if (S.State.HasUpgrade(id)) continue;
                var up = Economy.UpgradeCatalog.Get(id);
                if (S.State.Cash - up.Price < 100f) break;
                if (!S.CanBuyUpgrade(id, out string why)) { L($"  upgrade {id} not available: {why}"); continue; }
                bool ok = S.BuyUpgrade(id, out string err);
                L($"  upgrade {id}: {(ok ? "bought" : err)} cash={S.State.Cash}");
                yield return new WaitForSeconds(0.3f);
            }
            // a worn blade gets replaced
            if (S.State.HasUpgrade(Economy.UpgradeCatalog.TrimSaw) && S.State.BladeWear >= 0.7f && S.CanBuyUpgrade(Economy.UpgradeCatalog.SawBlade, out _))
            {
                S.BuyUpgrade(Economy.UpgradeCatalog.SawBlade, out _);
                L($"  new blade fitted cash={S.State.Cash}");
            }
        }

        private string ChooseSupplier(string style)
        {
            string[] prefs = style switch
            {
                "hammer" => new[] { "regional", "local" },
                "careless" => new[] { "local" },
                "saw" or "poorsaw" => new[] { "cutting", "regional", "local" },
                "collector" => new[] { "desert", "amethyst", "estate", "regional", "local" },
                "seller" => new[] { "amethyst", "regional", "local" },
                _ => new[] { "oversized", "desert", "cutting", "amethyst", "estate", "regional", "local" },
            };
            foreach (var id in prefs)
            {
                var sup = Economy.SupplierCatalog.Get(id);
                if (sup == null || !S.State.HasSupplier(id)) continue;
                if (S.State.Cash >= sup.Price + (id == "local" ? 0f : 60f)) return id;
            }
            return null;
        }

        private IEnumerator OpenNewestCrate()
        {
            CrateEntity crate = null;
            foreach (var c in S.Crates.Values) if (!c.IsOpened) crate = c;
            if (crate == null) yield break;
            Vector3 cratePos = crate.transform.position;
            Vector3 stand = cratePos + (new Vector3(-0.3f, 0f, 0.6f)).normalized * 1.1f; stand.y = 0f;
            yield return RouteTo(stand, 0.3f);
            yield return LookAndInteract(cratePos + Vector3.up * 0.2f, "Open crate");
            yield return new WaitForSeconds(0.9f);
            L($"  crate {crate.Record.Id} opened={crate.IsOpened} rocks={crate.RemainingRocks}");
        }

        private int _sawResult;   // 1 cut, -1 refused/none, 0 failed

        /// <summary>One rock through the saw with a technique, both pieces weighed and dealt with the way the crack loop does.</summary>
        private IEnumerator SawOne(string technique)
        {
            _sawResult = 0;
            var saw = Find<Lapidary.SawStation>();
            if (saw == null || !saw.Owned) { _sawResult = -1; yield break; }
            // the saw's own strengths: nodules and agate, medium or small
            SpecimenEntity rock = null; float best = -1f;
            foreach (var e in S.Entities.Values)
            {
                if (e.IsOpened || e.IsPiece) continue;
                if (e.Record.Location != SpecimenLocation.InCrate && e.Record.Location != SpecimenLocation.World && e.Record.Location != SpecimenLocation.Rack) continue;
                var g = e.Geology;
                if (g.SizeClass == SizeClass.Oversized) continue;
                float score = (g.Mineral == MineralId.Agate ? 0.5f : 0f) + (g.CavityFraction < 0.4f ? 0.3f : 0f) + (g.Tier >= QualityTier.Decent ? 0.2f : 0f) - (g.SizeClass == SizeClass.Large ? 0.3f : 0f);
                if (score > best) { best = score; rock = e; }
            }
            if (rock == null) { _sawResult = -1; yield break; }
            yield return DismissLetters();
            yield return FetchRock(rock);
            if (P.Held == null) { _sawResult = -1; yield break; }
            rock = P.Held;
            Vector3 clamp = ZonePos(ZoneKind.Saw);
            yield return RouteTo(new Vector3(clamp.x - 0.15f, 0f, clamp.z - 0.95f), 0.25f);
            yield return LookAndInteract(clamp, "Clamp in");
            yield return new WaitForSeconds(0.6f);
            if (!saw.Active)
            {
                // refused (too big, dull blade...): this rock goes to the hammer instead
                L($"  saw refused {rock.Id} ({rock.Geology.SizeClass}): '{P.Prompt}'");
                if (P.Held != null) { Vector3 bin = ZonePos(ZoneKind.Cradle); yield return RouteTo(new Vector3(bin.x, 0f, bin.z - 0.95f), 0.3f); P.Drop(); }
                _sawResult = -1;
                yield break;
            }
            bool poor = technique == "poor";
            saw.SetPlan(poor ? 32f : 0f, poor ? 18f : 0f, poor ? 0.02f : 0f);
            yield return new WaitForSeconds(0.2f);
            yield return Interact();
            yield return new WaitForSeconds(0.4f);
            saw.DevFeed = true; saw.DevFast = poor;
            float t0 = Time.time;
            while (saw.State == Lapidary.SawStation.Phase.Cutting && Time.time - t0 < 120f) yield return null;
            saw.DevFeed = false; saw.DevFast = false;
            yield return new WaitForSeconds(1.2f);
            var g2 = rock.Geology;
            L($"  sawed {rock.Id} {g2.Mineral} {g2.Cavity} {g2.Tier} {g2.SizeClass} in {Time.time - t0:F0}s ({technique}) chips={saw.ChipsThisCut} wear={saw.WearThisCut:F3} blade={S.State.BladeWear:F2} A=${(saw.PieceA != null ? saw.PieceA.Record.PristineForSale() : 0f)} B=${(saw.PieceB != null ? saw.PieceB.Record.PristineForSale() : 0f)} note='{saw.ResultNote}'");
            _sawResult = 1;
            // piece A from the vise, piece B from the tray
            for (int k = 0; k < 2; k++)
            {
                if (k == 0) yield return Interact();
                else
                {
                    Vector3 tray = ZonePos(ZoneKind.SawTray);
                    yield return RouteTo(new Vector3(tray.x, 0f, tray.z - 0.9f), 0.3f);
                    yield return LookAndInteract(tray, "Take");
                }
                yield return new WaitForSeconds(0.3f);
                if (P.Held == null) { L("  no piece in hand"); continue; }
                var piece = P.Held;
                yield return AppraiseHeld();
                if (P.Held == null) continue;
                float v = piece.Record.EstimatedValue();
                if (v >= 40f && FreeDisplaySlot() != null && S.State.DisplayedCount() < 3) yield return KeepHeld();
                else if (v >= 12f && FreeSaleSlot() != null) yield return StockHeld();
                else
                {
                    Vector3 outbox = ZonePos(ZoneKind.SellTray);
                    yield return RouteTo(StandNear(outbox), 0.3f);
                    yield return LookAndInteract(outbox, "Place in the dealer outbox");
                }
            }
        }

        public void RunPieceLifecycle() { if (!Running) StartCoroutine(PieceLifecycle()); }

        /// <summary>
        /// Save/anti-duplication QA for cut pieces: cut a rock, display one half, polish and put the other up for
        /// sale, reload, sell it to the dealer, reload again. The parent stays gone, each piece exists exactly once,
        /// polish and prices persist, the sold piece stays sold.
        /// </summary>
        private IEnumerator PieceLifecycle()
        {
            Running = true;
            Phase = "lifecycle";
            int specimensBefore = S.State.Specimens.Count;
            yield return SawCut(0f, 0f, 0f, false);
            Running = true;
            Phase = "lifecycle";
            // the newest cut: two piece records sharing a parent
            string parentId = null; int cutIndex = -1;
            foreach (var r in S.State.Specimens) if (r.IsPiece && r.CutIndex >= cutIndex) { cutIndex = r.CutIndex; parentId = r.ParentId; }
            if (parentId == null) { L("no pieces were made"); Running = false; yield break; }
            var pieces = new List<SpecimenRecord>();
            foreach (var r in S.State.Specimens) if (r.IsPiece && r.ParentId == parentId) pieces.Add(r);
            L($"lifecycle: parent {parentId} pieces={string.Join(",", pieces.ConvertAll(r => r.Id + "@" + r.Location))} {Chk(pieces.Count == 2)} specimens {specimensBefore}->{S.State.Specimens.Count} {DuplicateCheck()}");
            SpecimenRecord recA = null, recB = null;
            foreach (var r in pieces) { if (r.Location == SpecimenLocation.AppraisalStation) recA = r; else recB ??= r; }
            if (recA == null) recA = pieces[0];
            if (recB == null || recB == recA) foreach (var r in pieces) if (r != recA) recB = r;
            if (recB == null) { L("only one piece"); Running = false; yield break; }
            // A: from the scale to the display cabinet
            var entA = S.GetEntity(recA.Id);
            if (entA != null)
            {
                yield return RouteTo(StandNear(entA.transform.position), 0.3f);
                yield return LookAndInteract(entA.transform.position, "Take");
                if (P.Held == null) yield return FetchRock(entA);
                if (P.Held != null) { if (!P.Held.Record.Appraised) yield return AppraiseHeld(); if (P.Held != null) yield return KeepHeld(); }
            }
            L($"A displayed: loc={recA.Location} {Chk(recA.Location == SpecimenLocation.DisplaySlot)} slot={recA.LocationIndex} value={recA.EstimatedValue()}");
            L(Core.CollisionAudit.Report("piece on display"));
            // B: polish it, weigh it, put it in the window
            yield return Polish();
            Running = true;
            Phase = "lifecycle";
            // the polish run reloads the world on its way out: records and entities are new objects now
            recA = S.State.FindSpecimen(recA.Id); recB = S.State.FindSpecimen(recB.Id);
            var entB = S.GetEntity(recB.Id);
            L($"B after polish: loc={recB.Location} polish={recB.Polish:F2} {Chk(recB.Polish > 0.95f)} held={(P.Held != null ? P.Held.Id : "none")}");
            if (P.Held == null && entB != null)
            {
                yield return RouteTo(StandNear(entB.transform.position), 0.3f);
                yield return LookAndInteract(entB.transform.position, "Take");
                if (P.Held == null) yield return FetchRock(entB);
            }
            if (P.Held != null && P.Held.Record.Id == recB.Id)
            {
                yield return AppraiseHeld();
                if (P.Held != null) yield return StockHeld();
            }
            L($"B for sale: loc={recB.Location} {Chk(recB.Location == SpecimenLocation.SaleSlot)} asking={recB.AskingPrice} {Chk(recB.AskingPrice > 0f)} polish={recB.Polish:F2} appraised={recB.AppraisedValue}");
            L(Core.CollisionAudit.Report("polished piece stocked"));
            Snap("lifecycle_stocked");
            // reload 1
            float askBefore = recB.AskingPrice; float polishBefore = recB.Polish; int count1 = S.State.Specimens.Count;
            S.FlushSave("test");
            S.ContinueGame();
            yield return new WaitForSeconds(1.0f);
            recA = S.State.FindSpecimen(recA.Id); recB = S.State.FindSpecimen(recB.Id);
            var parent = S.State.FindSpecimen(parentId);
            L($"reload 1: parent={(parent != null ? parent.Location.ToString() : "missing")} {Chk(parent != null && parent.Location == SpecimenLocation.Cut && S.GetEntity(parentId) == null)} A={recA.Location}/{recA.LocationIndex} {Chk(recA.Location == SpecimenLocation.DisplaySlot && S.GetEntity(recA.Id) != null && S.GetEntity(recA.Id).Zone != null)} B={recB.Location} polish={recB.Polish:F2} asking={recB.AskingPrice} {Chk(recB.Location == SpecimenLocation.SaleSlot && Mathf.Approximately(recB.Polish, polishBefore) && Mathf.Approximately(recB.AskingPrice, askBefore))} specimens={S.State.Specimens.Count} {Chk(S.State.Specimens.Count == count1)} {DuplicateCheck()}");
            // sell B to the dealer
            entB = S.GetEntity(recB.Id);
            if (entB != null)
            {
                yield return RouteTo(StandNear(entB.transform.position), 0.3f);
                yield return LookAndInteract(entB.transform.position, "Take");
                if (P.Held != null)
                {
                    Vector3 tray = ZonePos(ZoneKind.SellTray);
                    yield return RouteTo(StandNear(tray), 0.3f);
                    yield return LookAndInteract(tray, "Place in the dealer outbox");
                    yield return SellCore();
                }
            }
            L($"B sold: loc={recB.Location} {Chk(recB.Location == SpecimenLocation.Sold)} entity={(S.GetEntity(recB.Id) != null)} {Chk(S.GetEntity(recB.Id) == null)} cash={S.State.Cash}");
            // reload 2
            int count2 = S.State.Specimens.Count; float cash2 = S.State.Cash;
            S.FlushSave("test");
            S.ContinueGame();
            yield return new WaitForSeconds(1.0f);
            recA = S.State.FindSpecimen(recA.Id); recB = S.State.FindSpecimen(recB.Id); parent = S.State.FindSpecimen(parentId);
            L($"reload 2: parent={(parent != null ? parent.Location.ToString() : "missing")} {Chk(parent != null && parent.Location == SpecimenLocation.Cut)} A={recA.Location} {Chk(recA.Location == SpecimenLocation.DisplaySlot && S.GetEntity(recA.Id) != null)} B={recB.Location} {Chk(recB.Location == SpecimenLocation.Sold && S.GetEntity(recB.Id) == null)} specimens={S.State.Specimens.Count} {Chk(S.State.Specimens.Count == count2)} cash={S.State.Cash} {Chk(Mathf.Approximately(S.State.Cash, cash2))} {DuplicateCheck()}");
            L(Core.CollisionAudit.Report("lifecycle end"));
            Phase = "done";
            Running = false;
        }

        /// <summary>Every live entity must map to exactly one record and no record may have two entities.</summary>
        private string DuplicateCheck()
        {
            var ids = new HashSet<string>();
            int dup = 0, orphan = 0;
            foreach (var e in S.Entities.Values)
            {
                if (e == null) continue;
                if (!ids.Add(e.Record.Id)) dup++;
                if (S.State.FindSpecimen(e.Record.Id) == null) orphan++;
            }
            var recIds = new HashSet<string>(); int dupRec = 0;
            foreach (var r in S.State.Specimens) if (!recIds.Add(r.Id)) dupRec++;
            return $"dupEntities={dup} dupRecords={dupRec} orphans={orphan} {Chk(dup == 0 && dupRec == 0 && orphan == 0)}";
        }

        /// <summary>
        /// The premises the test needs. The business now opens in one sealed unit, so any run that wants customers
        /// has to sign the leases and put the retail fixtures up first — exactly as a player would, through the
        /// same purchase and placement path, so a test can never assemble a shop the game would refuse.
        /// </summary>
        public void RunOpenShop() { if (!Running) StartCoroutine(OpenShopRoutine()); }

        private IEnumerator OpenShopRoutine() { Running = true; yield return EnsureShop(); Running = false; }

        private IEnumerator EnsureShop()
        {
            if (S?.State == null) yield break;
            float need = 0f;
            foreach (var id in new[] { Economy.UpgradeCatalog.BackRoom, Economy.UpgradeCatalog.ShopFront,
                                       Economy.UpgradeCatalog.SalesTable, Economy.UpgradeCatalog.ShopShelving,
                                       Economy.UpgradeCatalog.ShopSignage })
                if (!S.State.HasUpgrade(id)) need += Economy.UpgradeCatalog.Get(id).Price;
            if (need > 0f && S.State.Cash < need) S.State.Cash = need + 200f;
            foreach (var id in new[] { Economy.UpgradeCatalog.BackRoom, Economy.UpgradeCatalog.ShopFront,
                                       Economy.UpgradeCatalog.SalesTable, Economy.UpgradeCatalog.ShopShelving,
                                       Economy.UpgradeCatalog.ShopSignage })
            {
                if (S.State.HasUpgrade(id)) continue;
                S.BuyUpgrade(id, out string why);
                if (!string.IsNullOrEmpty(why)) L($"lease {id}: {why}");
                yield return null;
            }
            yield return new WaitForSeconds(0.35f);
            var bm = Find<Build.BuildMode>();
            if (bm == null) yield break;
            foreach (var (id, pos, yaw) in new[] {
                ("shop_island", new Vector3(4.47f, 0f, 0.65f), 0f),
                ("display_wall_a", new Vector3(6.72f, 0f, 2.3f), 90f),
                ("display_wall_b", new Vector3(6.72f, 0f, 3.95f), 90f) })
            {
                foreach (var f in Build.PlaceableFixture.All)
                {
                    if (f == null || f.Id != id || f.Pose.Placed) continue;
                    if (!bm.TryPlace(f, pos, yaw, out string why)) L($"site {id}: {why}");
                }
                yield return null;
            }
            yield return new WaitForSeconds(0.25f);
        }

        public void RunRetailStress(float minutes = 16f) { if (!Running) StartCoroutine(RetailStress(minutes)); }

        /// <summary>
        /// The NPC gate: keep the shop stocked, keep customers coming (up to four at once), serve the counter like a
        /// shopkeeper would, and measure stuck time, collision loops, queue stalls and path failures for a long while.
        /// </summary>
        private IEnumerator RetailStress(float minutes)
        {
            Running = true;
            Phase = "stress";
            yield return EnsureShop();
            var shop = Retail.RetailShop.Instance;
            var station = Find<GeodeEmpire.Checkout.CheckoutStation>();
            if (shop == null || station == null) { L("no shop/station"); Running = false; yield break; }
            L($"== RetailStress {minutes:F0} min cash={S.State.Cash} forSale={S.State.ForSaleCount()} slots={S.State.SaleCapacity}");
            // the cashier's spot: the counter's own staff datum
            Vector3 cashier = new Vector3(station.StaffStandPoint.position.x, 0f, station.StaffStandPoint.position.z);
            D.Teleport(cashier, 90f);
            yield return StockDirect(6);
            var stuckRun = new Dictionary<int, float>();      // continuous stuck seconds per customer
            var overlapRun = new Dictionary<long, float>();   // continuous close-contact seconds per pair
            int stuckEvents = 0, collisionLoops = 0, queueStalls = 0, served = 0, spawned = 0, leftEmpty = 0, maxAtOnce = 0;
            float worstStuck = 0f, worstOverlap = 0f, longestCounterWait = 0f;
            var seen = new HashSet<string>();
            float t0 = Time.time, nextSpawn = 2f, nextReport = 60f, counterSince = -1f, serveAt = -1f, playerMoveAt = 240f;
            bool playerInAisle = false;
            Retail.Customer lastAtCounter = null;
            while (Time.time - t0 < minutes * 60f)
            {
                float dt = Time.deltaTime, elapsed = Time.time - t0;
                // arrivals: steady traffic, sometimes two at once
                nextSpawn -= dt;
                if (nextSpawn <= 0f)
                {
                    int want = shop.Customers.Count < 2 && Random.value < 0.35f ? 2 : 1;
                    for (int i = 0; i < want && shop.Customers.Count < 4; i++) { if (shop.SpawnNow() != null) spawned++; }
                    nextSpawn = Random.Range(11f, 20f);
                }
                maxAtOnce = Mathf.Max(maxAtOnce, shop.Customers.Count);
                // restock when the shelves thin out
                if (S.State.ForSaleCount() < 4 && Time.frameCount % 120 == 0) yield return StockDirect(4);
                // the shopkeeper serves whoever reaches the counter, after a beat
                var at = shop.AtCounter;
                if (at != null && at.Wanted != null)
                {
                    if (at != lastAtCounter) { lastAtCounter = at; counterSince = Time.time; serveAt = Time.time + Random.Range(1.2f, 2.5f); }
                    else if (Time.time >= serveAt && serveAt > 0f)
                    {
                        D.LookAt(at.transform.position + Vector3.up * 1.3f);
                        if (!seen.Contains("checkout")) { seen.Add("checkout"); Snap("stress_checkout"); }
                        station.Enter(); yield return new WaitForSeconds(0.45f);
                        yield return station.CompleteFromHere(0.15f);
                        if (shop.AtCounter != at || at.Wanted == null) served++;
                        longestCounterWait = Mathf.Max(longestCounterWait, Time.time - counterSince);
                        serveAt = -1f;
                    }
                    if (Time.time - counterSince > 30f) { queueStalls++; counterSince = Time.time; L($"  [{elapsed:F0}s] queue stall: {at.Archetype.Name} at counter, wanted={(at.Wanted != null)} rung={(station.Tx != null)}"); }
                }
                else lastAtCounter = null;
                // the player in the aisle for a couple of minutes: a moving obstacle on the browse line
                if (elapsed > playerMoveAt)
                {
                    playerInAisle = !playerInAisle;
                    if (playerInAisle) D.Teleport(new Vector3(4.5f, 0f, -1.7f), 0f); else D.Teleport(cashier, 90f);
                    playerMoveAt = elapsed + (playerInAisle ? 120f : 180f);
                    L($"  [{elapsed:F0}s] player {(playerInAisle ? "standing in the aisle" : "back at the counter")}");
                }
                // metrics
                var list = new List<Retail.Customer>();
                foreach (var c in shop.Customers) if (c != null) list.Add(c);
                foreach (var c in list)
                {
                    bool stuck = c.Walking && c.Speed < 0.03f && !c.Arrived;
                    stuckRun.TryGetValue(c.Id, out float run);
                    run = stuck ? run + dt : 0f;
                    stuckRun[c.Id] = run;
                    worstStuck = Mathf.Max(worstStuck, run);
                    if (run > 4f) { stuckEvents++; stuckRun[c.Id] = 0f; L($"  [{elapsed:F0}s] stuck>4s: {c.Archetype.Name}#{c.Id} state={c.State} at {c.transform.position:F2} recoveries={c.Recoveries}"); }
                }
                for (int i = 0; i < list.Count; i++)
                    for (int j = i + 1; j < list.Count; j++)
                    {
                        var a = list[i]; var b = list[j];
                        long key = ((long)Mathf.Min(a.Id, b.Id) << 20) | (long)Mathf.Max(a.Id, b.Id);
                        Vector3 d = a.transform.position - b.transform.position; d.y = 0f;
                        bool close = d.sqrMagnitude < 0.45f * 0.45f && (a.Walking || b.Walking);
                        overlapRun.TryGetValue(key, out float run);
                        run = close ? run + dt : 0f;
                        overlapRun[key] = run;
                        worstOverlap = Mathf.Max(worstOverlap, run);
                        if (run > 2.5f) { collisionLoops++; overlapRun[key] = 0f; L($"  [{elapsed:F0}s] collision loop: #{a.Id}({a.State}) vs #{b.Id}({b.State}) at {a.transform.position:F2}"); }
                        if (!seen.Contains("passing") && d.sqrMagnitude < 1.3f * 1.3f && a.Walking && b.Walking) { seen.Add("passing"); D.LookAt((a.transform.position + b.transform.position) * 0.5f + Vector3.up * 1.1f); Snap("stress_passing"); }
                    }
                // representative moments, from wherever the shopkeeper stands
                foreach (var c in list)
                {
                    if (!seen.Contains("entrance") && c.State == Retail.Customer.Phase.Browsing && c.Walking && shop.DoorPoint != null && (c.transform.position - shop.DoorPoint.position).sqrMagnitude < 1.0f) { seen.Add("entrance"); D.LookAt(c.transform.position + Vector3.up * 1.2f); Snap("stress_entrance"); }
                    if (!seen.Contains("browsing") && c.State == Retail.Customer.Phase.Browsing && !c.Walking && c.Arrived) { seen.Add("browsing"); D.LookAt(c.transform.position + Vector3.up * 1.2f); Snap("stress_browsing"); }
                    if (!seen.Contains("exit") && c.State == Retail.Customer.Phase.Leaving && shop.DoorPoint != null && (c.transform.position - shop.DoorPoint.position).sqrMagnitude < 1.4f) { seen.Add("exit"); D.LookAt(c.transform.position + Vector3.up * 1.2f); Snap("stress_exit"); }
                }
                if (!seen.Contains("queue") && shop.QueueLength >= 2) { seen.Add("queue"); D.LookAt(shop.QueuePoint(1).position + Vector3.up * 1.2f); Snap("stress_queue"); }
                if (elapsed >= nextReport)
                {
                    nextReport += 60f;
                    L($"  [{elapsed / 60f:F0} min] customers={shop.Customers.Count} spawned={spawned} served={served} leftEmpty={S.State.Stats.CustomersLeftEmptyHanded - leftEmpty} stuckEvents={stuckEvents} worstStuck={worstStuck:F1}s loops={collisionLoops} worstOverlap={worstOverlap:F1}s stalls={queueStalls} recoveries={shop.Metrics.StuckRecoveries} repositions={shop.Metrics.Repositions} pathFail={shop.Metrics.PathFailures} forSale={S.State.ForSaleCount()} cash={S.State.Cash} fps={1f / Mathf.Max(0.001f, Time.smoothDeltaTime):F0}");
                }
                yield return null;
            }
            L($"stress end: spawned={spawned} served={served} maxAtOnce={maxAtOnce} stuckEvents={stuckEvents} {Chk(stuckEvents == 0)} worstStuck={worstStuck:F1}s collisionLoops={collisionLoops} {Chk(collisionLoops == 0)} worstOverlap={worstOverlap:F1}s queueStalls={queueStalls} {Chk(queueStalls == 0)} longestCounterWait={longestCounterWait:F1}s recoveries={shop.Metrics.StuckRecoveries} repositions={shop.Metrics.Repositions} {Chk(shop.Metrics.Repositions == 0)} pathFailures={shop.Metrics.PathFailures} customersLeft={shop.Customers.Count} snaps={string.Join(",", seen)}");
            L(Core.CollisionAudit.Report("after stress"));
            Phase = "done";
            Running = false;
        }

        /// <summary>Put appraised test specimens straight onto free sale slots (no walking): stock for the stress run.</summary>
        private IEnumerator StockDirect(int n)
        {
            yield return EnsureShop();
            var shop = Retail.RetailShop.Instance;
            int room = 0;
            foreach (var z in shop.SaleSlots) if (z.gameObject.activeInHierarchy && !z.Locked && z.IsEmpty) room++;
            n = Mathf.Min(n, room);
            if (n <= 0) yield break;
            SpawnTestStock(n, 0f);
            yield return null;
            int placed = 0;
            foreach (var e in new List<SpecimenEntity>(S.Entities.Values))
            {
                if (!e.IsOpened || !e.Record.Appraised || e.Record.Location != SpecimenLocation.World || e.Zone != null) continue;
                PlacementZone free = null;
                foreach (var z in shop.SaleSlots) if (z.gameObject.activeInHierarchy && !z.Locked && z.IsEmpty) { free = z; break; }
                if (free == null) break;
                free.Place(e);
                placed++;
                if (placed >= n) break;
            }
            L($"  stocked {placed} -> forSale={S.State.ForSaleCount()}");
        }


        /// <summary>Buy the Stage-2 workshop, check the room changed, store a rock on the rack, display on the trophy wall, reload.</summary>
        private IEnumerator Stage2()
        {
            Running = true;
            Phase = "stage2";
            var exp = Find<Workshop.WorkshopExpansion>();
            if (exp == null) { L("no WorkshopExpansion in scene"); Running = false; yield break; }
            L($"== Stage2 cash={S.State.Cash} stage={S.State.WorkshopStage} display={S.State.DisplayCapacity} sale={S.State.SaleCapacity} rootActive={exp.Stage2Root.activeSelf} {Chk(!exp.Stage2Root.activeSelf)}");
            if (!S.State.HasUpgrade(Economy.UpgradeCatalog.TrimSaw)) { if (S.State.Cash < 700f) S.AddCash(700f, "test"); S.BuyUpgrade(Economy.UpgradeCatalog.TrimSaw, out _); yield return null; }
            bool canLap = S.CanBuyUpgrade(Economy.UpgradeCatalog.PolishLap, out string whyLap);
            L($"lap before stage 2: can={canLap} why='{whyLap}' {Chk(!canLap)}");
            bool canOversized = S.State.HasSupplier(Economy.SupplierCatalog.OversizedLot);
            L($"oversized lot before: {canOversized} {Chk(!canOversized)}");
            if (S.State.Cash < 1500f) S.AddCash(1500f, "test");
            float cashBefore = S.State.Cash;
            bool ok = S.BuyUpgrade(Economy.UpgradeCatalog.Stage2, out string err);
            yield return null;
            L($"bought stage 2: {ok} {err} cash {cashBefore}->{S.State.Cash} {Chk(Mathf.Approximately(S.State.Cash, cashBefore - 1400f))} stage={S.State.WorkshopStage} {Chk(S.State.WorkshopStage == 2)} rootActive={exp.Stage2Root.activeSelf} {Chk(exp.Stage2Root.activeSelf)} display={S.State.DisplayCapacity} sale={S.State.SaleCapacity}");
            foreach (var h in exp.HideAtStage2) L($"  hidden {h.name}: {Chk(!h.activeSelf)}");
            canLap = S.CanBuyUpgrade(Economy.UpgradeCatalog.PolishLap, out whyLap);
            L($"lap after stage 2: can={canLap} why='{whyLap}' {Chk(canLap)}   oversized lot={S.State.HasSupplier(Economy.SupplierCatalog.OversizedLot)} {Chk(S.State.HasSupplier(Economy.SupplierCatalog.OversizedLot))}");
            var shop = Retail.RetailShop.Instance; int openSale = 0; foreach (var z in shop.SaleSlots) if (z.gameObject.activeInHierarchy && !z.Locked) openSale++;
            var dc = Find<Workshop.DisplayCabinet>(); int openDisp = 0; foreach (var z in dc.Slots) if (z.gameObject.activeInHierarchy && !z.Locked) openDisp++;
            L($"open sale slots={openSale}/{shop.SaleSlots.Count} {Chk(openSale == S.State.SaleCapacity)}  open display slots={openDisp}/{dc.Slots.Count} {Chk(openDisp == S.State.DisplayCapacity)}");

            // a rock onto the rack
            SpecimenEntity rock = null;
            foreach (var e in S.Entities.Values) if (!e.IsPiece && e.Record.Location == SpecimenLocation.World && !e.Record.IsOpened) { rock = e; break; }
            if (rock == null) foreach (var e in S.Entities.Values) if (e.Record.Location == SpecimenLocation.World) { rock = e; break; }
            if (rock == null) { L("no loose rock to store"); Running = false; yield break; }
            yield return FetchRock(rock);
            if (P.Held == null) { L("could not pick up rock"); Running = false; yield break; }
            rock = P.Held;
            Vector3 rackPos = ZonePos(ZoneKind.Rack);
            L($"rack zone at {rackPos:F2}");
            yield return RouteTo(new Vector3(rackPos.x + 0.95f, 0f, rackPos.z), 0.3f);
            yield return LookAndInteract(rackPos, "rock rack");
            yield return new WaitForSeconds(0.4f);
            L($"rack: loc={rock.Record.Location} {Chk(rock.Record.Location == SpecimenLocation.Rack)} idx={rock.Record.LocationIndex} pos={rock.transform.position:F2} held={(P.Held != null)}");
            D.Teleport(new Vector3(rackPos.x + 1.3f, 0f, rackPos.z - 0.2f), -90f);
            D.LookAt(new Vector3(rackPos.x, 0.9f, rackPos.z));
            yield return new WaitForSeconds(0.4f);
            Snap("stage2_rack");

            // an opened specimen onto the trophy wall
            SpecimenEntity trophyRock = null;
            foreach (var e in S.Entities.Values) if (e.Record.IsOpened && (e.Record.Location == SpecimenLocation.World || e.Record.Location == SpecimenLocation.SellTray) && e.Zone == null) { trophyRock = e; break; }
            if (trophyRock == null) foreach (var e in S.Entities.Values) if (e.Record.IsOpened && e.Record.Location != SpecimenLocation.DisplaySlot && e.Record.Location != SpecimenLocation.SaleSlot) { trophyRock = e; break; }
            int trophyIndex = 12;
            if (trophyRock != null)
            {
                if (trophyRock.Zone != null) { yield return RouteTo(StandNear(trophyRock.transform.position), 0.3f); yield return LookAndInteract(trophyRock.transform.position, "Take"); }
                else yield return FetchRock(trophyRock);
                if (P.Held != null)
                {
                    trophyRock = P.Held;
                    Vector3 slotPos = ZonePos(ZoneKind.DisplaySlot, trophyIndex);
                    L($"trophy slot {trophyIndex} at {slotPos:F2}");
                    yield return RouteTo(new Vector3(slotPos.x + 1.0f, 0f, slotPos.z), 0.3f);
                    yield return LookAndInteract(slotPos, "display slot");
                    yield return new WaitForSeconds(0.4f);
                    L($"trophy: loc={trophyRock.Record.Location} {Chk(trophyRock.Record.Location == SpecimenLocation.DisplaySlot)} idx={trophyRock.Record.LocationIndex} {Chk(trophyRock.Record.LocationIndex == trophyIndex)} held={(P.Held != null)} displayed={S.State.DisplayedCount()}");
                    D.Teleport(new Vector3(slotPos.x + 1.5f, 0f, slotPos.z + 0.5f), -90f);
                    D.LookAt(new Vector3(slotPos.x, 1.75f, slotPos.z + 0.5f));
                    yield return new WaitForSeconds(0.4f);
                    Snap("stage2_trophy");
                }
                else L("could not pick up trophy rock");
            }
            else L("no opened specimen for the trophy wall");

            // showroom shelf view
            yield return RouteTo(new Vector3(4.2f, 0f, -0.6f), 0.35f);
            D.LookAt(new Vector3(3.9f, 1.1f, -2.55f));
            yield return new WaitForSeconds(0.4f);
            Snap("stage2_shopshelf");
            yield return RouteTo(new Vector3(0.6f, 0f, 0.9f), 0.35f);
            D.LookAt(new Vector3(1.75f, 1.2f, 2.4f));
            yield return new WaitForSeconds(0.4f);
            Snap("stage2_sawbay");
            D.LookAt(new Vector3(-2.75f, 1.0f, -1.15f));
            yield return new WaitForSeconds(0.4f);
            Snap("stage2_polish");

            // reload: the room stays expanded, the rack keeps its rock
            string rackId = rock.Id; string trophyId = trophyRock != null ? trophyRock.Id : null;
            S.FlushSave("test");
            S.ContinueGame();
            yield return new WaitForSeconds(0.9f);
            exp = Find<Workshop.WorkshopExpansion>();
            var back = S.GetEntity(rackId);
            var backT = trophyId != null ? S.GetEntity(trophyId) : null;
            L($"after reload: stage={S.State.WorkshopStage} rootActive={(exp != null && exp.Stage2Root.activeSelf)} {Chk(exp != null && exp.Stage2Root.activeSelf)} rackRock={(back != null ? back.Record.Location.ToString() : "missing")} {Chk(back != null && back.Record.Location == SpecimenLocation.Rack && back.Zone != null && back.Zone.Kind == ZoneKind.Rack)} trophy={(backT != null ? backT.Record.Location + "/" + backT.Record.LocationIndex : "n/a")} {Chk(trophyId == null || (backT != null && backT.Record.Location == SpecimenLocation.DisplaySlot && backT.Record.LocationIndex == trophyIndex))} display={S.State.DisplayCapacity} sale={S.State.SaleCapacity} suppliers={string.Join(",", S.State.UnlockedSuppliers)}");
            L($"stage2 done {WorldSummary()}");
            Running = false;
        }


        private IEnumerator Polish()
        {
            Running = true;
            Phase = "polish";
            var lap = Find<Lapidary.PolishStation>();
            if (lap == null) { L("no lap"); Running = false; yield break; }
            if (!lap.Owned)
            {
                // the lap sits behind the trim saw and the Stage-2 workshop
                foreach (var id in new[] { Economy.UpgradeCatalog.TrimSaw, Economy.UpgradeCatalog.Stage2, Economy.UpgradeCatalog.PolishLap })
                {
                    if (S.State.HasUpgrade(id)) continue;
                    float price = Economy.UpgradeCatalog.Get(id).Price;
                    if (S.State.Cash < price + 150f) S.AddCash(price + 150f, "test");
                    S.BuyUpgrade(id, out string err);
                    L($"bought {id}: {(err ?? "ok")}");
                    yield return null;
                }
                L("lap owned=" + lap.Owned);
            }
            SpecimenEntity piece = null; float best = -1f;
            foreach (var e in S.Entities.Values)
                if (e.IsPiece && e.Record.Polish < 0.5f && (e.Record.Location == SpecimenLocation.World || e.Record.Location == SpecimenLocation.SellTray || e.Record.Location == SpecimenLocation.AppraisalStation) && e.Record.PristineForSale() > best) { best = e.Record.PristineForSale(); piece = e; }
            if (piece == null) { L("no unpolished piece around"); Running = false; yield break; }
            float valueBefore = piece.Record.PristineForSale();
            L($"== Polish {piece.Id} {piece.Record.DisplayName} value=${valueBefore} polish={piece.Record.Polish:F2} loc={piece.Record.Location}");
            if (piece.Zone != null)
            {
                yield return RouteTo(StandNear(piece.transform.position), 0.3f);
                yield return LookAndInteract(piece.transform.position, "Take");
            }
            else yield return FetchRock(piece);
            if (P.Held == null) { L("could not pick up the piece"); Running = false; yield break; }
            piece = P.Held;
            Vector3 lapPos = ZonePos(ZoneKind.Lap);
            yield return RouteTo(new Vector3(lapPos.x + 0.9f, 0f, lapPos.z), 0.3f);
            yield return LookAndInteract(lapPos, "Set face-down on");
            yield return new WaitForSeconds(0.6f);
            L($"on lap: loc={piece.Record.Location} held={(P.Held != null)} pos={piece.transform.position:F2}");
            Snap("lap_before");
            D.LookAt(lapPos + Vector3.up * 0.02f);
            yield return new WaitForSeconds(0.3f);
            L($"lap prompt='{P.Prompt}'");
            D.KeyDown(Key.E);
            float t0 = Time.time; int frames = 0; bool midSnap = false;
            while (piece.Record.Polish < 0.98f && Time.time - t0 < 20f)
            {
                // sweep in a circle with the mouse
                float a = frames * 0.25f;
                D.MouseDelta(Mathf.Cos(a) * 18f, Mathf.Sin(a) * 18f);
                frames++;
                if (!midSnap && piece.Record.Polish > 0.5f) { midSnap = true; L($"  halfway at {Time.time - t0:F1}s polishing={lap.Polishing} rpm={lap.Rpm:F2} sweep={lap.LastSweep:F2}"); Snap("lap_mid"); }
                yield return null;
            }
            D.KeyUp();
            yield return new WaitForSeconds(0.6f);
            float valueAfter = piece.Record.PristineForSale();
            L($"polished in {Time.time - t0:F1}s: polish={piece.Record.Polish:F2} name={piece.Record.DisplayName} value ${valueBefore} -> ${valueAfter}");
            Snap("lap_after");
            yield return LookAndInteract(lapPos, "Take");
            L($"held={(P.Held != null ? P.Held.Record.DisplayName : "none")}");
            if (P.Held != null)
            {
                D.SetMouseButton(1, true); yield return new WaitForSeconds(0.5f); Snap("lap_piece_inspect"); D.SetMouseButton(1, false); yield return new WaitForSeconds(0.2f);
                Vector3 scale = ZonePos(ZoneKind.Scale);
                yield return RouteTo(StandNear(scale), 0.3f);
                yield return LookAndInteract(scale, "Weigh on the scale");
                yield return new WaitForSeconds(1.6f);
                var ap = Find<AppraisalStation>();
                L("appraised=" + (ap.Current != null && ap.Current.Record.Appraised) + " value=" + (ap.Current != null ? ap.Current.Record.AppraisedValue.ToString() : ""));
                Snap("lap_appraisal");
            }
            L(RunSaveReloadCheck());
            var back = S.State.FindSpecimen(piece.Id);
            L($"after reload polish={(back != null ? back.Polish : -1f):F2}");
            Phase = "done";
            Running = false;
        }

        /// <summary>Run C step: take the nicest opened specimen we can find to display slot 0 and verify it stuck.</summary>
        /// <summary>V5 §80 resolution checks: the same screens at whatever Game view size is set (1080p/1440p/4K), captured for review.</summary>
        public void RunUiSweep(string tag) { if (!Running) StartCoroutine(UiSweep(tag)); }

        private IEnumerator UiSweep(string tag)
        {
            Running = true; Phase = "uisweep";
            L($"== UiSweep {tag} screen={Screen.width}x{Screen.height}");
            S.AddCash(2000f, "test");
            SpawnTestStock(3, 0f);
            yield return new WaitForSeconds(0.5f);
            // 1. free roam: crosshair, prompt and the tutorial card over an opened rock on the floor
            SpecimenEntity first = null; foreach (var e in S.Entities.Values) if (e.IsOpened) { first = e; break; }
            if (first != null)
            {
                yield return RouteTo(StandNear(first.transform.position, 0.9f), 0.3f);
                D.LookAt(first.transform.position); yield return new WaitForSeconds(0.4f);
                Snap($"{tag}_roam_prompt"); yield return new WaitForSecondsRealtime(0.2f);
                L($"prompt='{P.Prompt}' hint='{P.Hint}' tutorial={(Tutorial.Current != null ? Tutorial.Current.Id : "none")}");
            }
            // 2. a kept piece reads like its label when looked at
            yield return DisplayKeepCore();
            yield return new WaitForSeconds(0.4f);
            Snap($"{tag}_cabinet_label"); yield return new WaitForSecondsRealtime(0.2f);
            L($"label hint='{P.Hint}' prompt='{P.Prompt.Replace("\n", " / ")}'");
            foreach (var en in S.Entities.Values) if (en.Record.Location == SpecimenLocation.DisplaySlot) { L($"  displayed {en.Id}: zone={(en.Zone != null ? en.Zone.name + "/" + en.Zone.Kind : "none")} opened={en.IsOpened} hint='{en.GetHint(P)}' target={(P.Target as SpecimenEntity)?.Id}"); break; }
            // 3. the appraisal card
            SpecimenEntity rock = null; foreach (var e in S.Entities.Values) if (e.IsOpened && e.Record.Location == SpecimenLocation.World) { rock = e; break; }
            var scale = Find<AppraisalStation>();
            if (rock != null) { if (rock.Zone != null) rock.Zone.Take(rock, true); scale.Scale.Place(rock); }
            Vector3 sp = ZonePos(ZoneKind.Scale);
            yield return RouteTo(StandNear(sp, 0.95f), 0.3f);
            D.LookAt(sp); yield return new WaitForSeconds(2.8f);
            Snap($"{tag}_appraisal_card"); yield return new WaitForSecondsRealtime(0.2f);
            // 4. the tablet's pages
            var tablet = UI.TabletUI.Instance;
            tablet.Open(); yield return null;
            for (int t = 0; t < 4; t++) { tablet.ShowTab(t); yield return new WaitForSeconds(0.4f); Snap($"{tag}_tablet_{t}"); }
            tablet.Close(); yield return new WaitForSeconds(0.3f);
            // 5. the pause menu
            var pause = UI.PauseMenu.Instance;
            pause.Open(); yield return new WaitForSecondsRealtime(0.3f); Snap($"{tag}_pause"); yield return new WaitForSecondsRealtime(0.2f); pause.Close(); yield return new WaitForSecondsRealtime(0.3f);   // the pause menu stops scaled time
            // 6. the bench panel mid wind-up on a rough rock
            if (S.Crates.Count == 0) { S.BuyCrate("local", out string err); yield return new WaitForSeconds(1.4f); }
            CrateEntity crate = null; foreach (var c in S.Crates.Values) { crate = c; break; }
            if (crate != null && !crate.IsOpened)
            {
                Vector3 cratePos = crate.transform.position; Vector3 stand = cratePos + (new Vector3(-0.3f, 0f, 0.6f)).normalized * 1.1f; stand.y = 0f;
                yield return Walk(stand, 0.3f);
                yield return LookAndInteract(cratePos + Vector3.up * 0.2f, "Open crate");
                yield return new WaitForSeconds(0.9f);
            }
            SpecimenEntity rough = null; foreach (var e in S.Entities.Values) if (!e.IsOpened && e.Record.Location == SpecimenLocation.InCrate) { rough = e; break; }
            if (rough != null)
            {
                yield return FetchRock(rough);
                if (P.Held != null)
                {
                    Vector3 cradle = ZonePos(ZoneKind.Cradle);
                    yield return RouteTo(new Vector3(cradle.x, 0f, cradle.z - 0.95f), 0.25f);
                    yield return LookAndInteract(cradle, "Set on the cradle");
                    yield return new WaitForSeconds(1.0f);
                    var bench = Find<CrackingBench>();
                    if (bench.Active)
                    {
                        yield return AimCursor(bench, bench.SeamCursorHint());
                        D.SetMouseButton(0, true); yield return new WaitForSeconds(0.45f);
                        Snap($"{tag}_bench_windup"); yield return new WaitForSecondsRealtime(0.2f);
                        D.SetMouseButton(0, false); yield return new WaitForSeconds(0.6f);
                        Snap($"{tag}_bench_after"); yield return new WaitForSecondsRealtime(0.2f);
                        if (UseGamepad) yield return D.PadTap(GamepadButton.East, 0.1f); else yield return D.Tap(Key.Escape, 0.1f);
                        yield return new WaitForSeconds(0.5f);
                    }
                }
            }
            L("UI SWEEP DONE");
            Phase = "done"; Running = false;
        }

        /// <summary>V5 §62 auction: consign a displayed exceptional piece, the courier collects it with the next crate, the hammer three crates on, the letter, the save.</summary>
        public void RunAuction() { if (!Running) StartCoroutine(AuctionRun()); }

        private IEnumerator AuctionRun()
        {
            Running = true; Phase = "auction";
            var st = S.State;
            L($"== Auction cash={st.Cash} rep={Economy.Reputation.Word(st)}");
            S.AddCash(3000f, "test");
            st.Stats.SpecimensSold += 60; st.Stats.CustomersServed += 20; st.Stats.CleanOpens += 10;   // a known name (reputation tier 2): the house takes consignments
            SpawnTestStock(4, 0f);
            yield return DisplayKeepCore();
            st = S.State;
            SpecimenRecord kept = null; foreach (var r in st.Specimens) if (r.Location == SpecimenLocation.DisplaySlot) { kept = r; break; }
            if (kept == null) { L("nothing displayed"); Running = false; yield break; }
            // a second exceptional piece straight into the cabinet, so one lot can pass while the other sells
            SpecimenEntity second = null; foreach (var e in S.Entities.Values) if (e.IsOpened && e.Record.Location == SpecimenLocation.World && e.Record.Id != kept.Id && Economy.Auction.IsEligible(e.Record)) { second = e; break; }
            if (second == null) foreach (var e in S.Entities.Values) if (e.IsOpened && e.Record.Location == SpecimenLocation.World && e.Record.Id != kept.Id) { second = e; break; }
            L($"rep={Economy.Reputation.Word(st)} ({Economy.Reputation.Score(st)}) second={(second != null ? second.Record.DisplayName + " eligible=" + Economy.Auction.IsEligible(second.Record) : "none")}");
            var cabinet = Find<Workshop.DisplayCabinet>();
            if (second != null) foreach (var z in cabinet.Slots) if (z.IsEmpty && !z.Locked && z.gameObject.activeInHierarchy && z.RefusalReason(second) == null && z.FitRefusal(second) == null) { z.Place(second, true); break; }
            string secondId = second != null ? second.Record.Id : null;
            L($"eligible kept={Economy.Auction.IsEligible(kept)} cannot='{Economy.Auction.CannotConsign(st, kept)}' estimate={Economy.Auction.Estimate(kept)} mult={Economy.Auction.HammerMultiplier(st, kept):F2}");
            bool ok = Economy.Auction.Consign(S, kept, out string why);
            L($"consign kept: {(ok ? "ok" : why)} consignedAt={kept.ConsignedAtCrate}");
            if (second != null) { bool ok2 = Economy.Auction.Consign(S, second.Record, out string why2); L($"consign second: {(ok2 ? "ok" : why2)} mult={Economy.Auction.HammerMultiplier(st, second.Record):F2}"); }
            // withdraw and re-consign: the tablet's other button
            Economy.Auction.Withdraw(S, kept); L($"withdrawn: consignedAt={kept.ConsignedAtCrate}");
            Economy.Auction.Consign(S, kept, out why);
            yield return new WaitForSeconds(0.4f);
            Snap("auction_consigned");
            // the courier comes with the next crate
            int before = S.Entities.Count;
            if (!S.BuyCrate("local", out string err)) { L("buy failed: " + err); Running = false; yield break; }
            yield return new WaitForSeconds(0.8f);
            st = S.State;
            L($"after delivery: kept loc={kept.Location} entity={(S.GetEntity(kept.Id) != null)} lots={st.AuctionLots.Count} entities {before}->{S.Entities.Count} displayed={st.DisplayedCount()}");
            foreach (var lot in st.AuctionLots) L("  lot: " + Economy.Auction.LotLine(st, lot));
            L(RunSaveReloadCheck());
            st = S.State;
            L($"after reload: lots={st.AuctionLots.Count} kept loc={st.FindSpecimen(kept.Id)?.Location} entity={(S.GetEntity(kept.Id) != null)}");
            // three more crates: sell each crate straight to the dealer so the pallet stays free
            for (int i = 0; i < Economy.Auction.ResolveAfterCrates; i++)
            {
                yield return OpenNewestCrate();   // an unopened crate keeps its rocks and its pallet cell
                yield return SellCrateDirect();
                yield return BreakDownEmptyCrates();
                float cashBefore = S.State.Cash;
                if (!S.BuyCrate("local", out err)) { L($"buy {i} failed: " + err); break; }
                yield return new WaitForSeconds(0.6f);
                st = S.State;
                L($"crate {st.CrateCounter}: lots={st.AuctionLots.Count} sold={st.Stats.AuctionsSold} passed={st.Stats.AuctionsPassed} revenue={st.Stats.AuctionRevenue} cash {cashBefore}->{st.Cash} letters={st.PendingLetters.Count}");
            }
            yield return new WaitForSeconds(0.5f);
            yield return DismissLetters();
            st = S.State;
            var keptNow = st.FindSpecimen(kept.Id);
            L($"kept: loc={keptNow?.Location} history=[{UI.TabletUI.HistoryText(keptNow, 6).Replace("\n", " | ")}]");
            if (secondId != null) { var s2 = st.FindSpecimen(secondId); var e2 = S.GetEntity(secondId); L($"second: loc={s2?.Location}/{s2?.LocationIndex} entity={(e2 != null)} at={(e2 != null ? e2.transform.position.ToString() : "-")} displayed={st.DisplayedCount()}"); }
            L(Core.CollisionAudit.Report("auction return"));
            L(RunSaveReloadCheck());
            L($"stats: sold={S.State.Stats.AuctionsSold} passed={S.State.Stats.AuctionsPassed} revenue={S.State.Stats.AuctionRevenue} biggest={S.State.Stats.BiggestSale} ({S.State.Stats.BiggestSaleName})");
            // a lot that passes: a fresh exceptional piece consigned, its reserve set out of reach, hammered, brought back
            st = S.State;
            var rec = S.CreateSpecimenRecord(0x7D1UL, "regional", ""); rec.Location = SpecimenLocation.World; rec.Condition.Opened = true; rec.Condition.Rinsed = true; rec.Appraised = true; rec.AppraisedValue = Valuation.DamagedValue(rec.Geology, 0f, 0f);
            var ent = S.Spawn(rec, new Vector3(-1.0f, 0.3f, -0.4f), Quaternion.identity, false); ent.ApplyOpenPose();
            foreach (var z in cabinet.Slots) if (z.IsEmpty && !z.Locked && z.gameObject.activeInHierarchy && z.RefusalReason(ent) == null && z.FitRefusal(ent) == null) { z.Place(ent, true); break; }
            bool ok3 = Economy.Auction.Consign(S, rec, out string why3);
            L($"pass test: consign {(ok3 ? "ok" : why3)} loc={rec.Location}");
            yield return OpenNewestCrate(); yield return SellCrateDirect(); yield return BreakDownEmptyCrates();
            if (!S.BuyCrate("local", out err)) L("pass test: buy failed: " + err); yield return new WaitForSeconds(0.6f);
            st = S.State;
            foreach (var lot in st.AuctionLots) if (lot.SpecimenId == rec.Id) { lot.Reserve = 99999f; L($"pass test: collected, reserve forced to {lot.Reserve}"); }
            for (int i = 0; i < Economy.Auction.ResolveAfterCrates; i++) { yield return OpenNewestCrate(); yield return SellCrateDirect(); yield return BreakDownEmptyCrates(); if (!S.BuyCrate("local", out err)) L($"pass test: buy {i} failed: " + err); yield return new WaitForSeconds(0.6f); }
            yield return DismissLetters();
            st = S.State;
            var back = st.FindSpecimen(rec.Id); var backE = S.GetEntity(rec.Id);
            L($"pass test: loc={back?.Location}/{back?.LocationIndex} entity={(backE != null)} at={(backE != null ? backE.transform.position.ToString() : "-")} zone={(backE != null && backE.Zone != null ? backE.Zone.name : "-")} passed={st.Stats.AuctionsPassed} displayed={st.DisplayedCount()}");
            L(Core.CollisionAudit.Report("passed lot back"));
            L(RunSaveReloadCheck());
            Phase = "done"; Running = false;
        }

        public void RunDisplayKeep() { if (!Running) StartCoroutine(DisplayKeep()); }

        private IEnumerator DisplayKeep()
        {
            Running = true;
            yield return DisplayKeepCore();
            Phase = "done";
            Running = false;
        }

        private IEnumerator DisplayKeepCore()
        {
            Phase = "display";
            SpecimenEntity best = null;
            foreach (var e in S.Entities.Values)
                if (e.IsOpened && e.Record.Location != SpecimenLocation.DisplaySlot && e.Record.Location != SpecimenLocation.SaleSlot && e.Record.Location != SpecimenLocation.Sold && (best == null || e.Geology.BaseValue > best.Geology.BaseValue)) best = e;
            if (best == null) { L("no opened specimen to display"); yield break; }
            Vector3 bp = best.transform.position;
            yield return RouteTo(StandNear(bp, 0.9f), 0.3f);
            yield return LookAndInteract(bp, "Take");
            if (P.Held == null) { yield return LookAndInteract(bp, "Pick up"); }
            L("held=" + (P.Held != null ? P.Held.Record.DisplayName : "none"));
            if (P.Held == null) { Running = false; yield break; }
            Vector3 slot = ZonePos(ZoneKind.DisplaySlot, 0);
            yield return Walk(StandNear(slot, 1.0f), 0.3f);
            yield return LookAndInteract(slot, "Place in display slot");
            yield return new WaitForSeconds(0.4f);
            var st = S.State;
            L($"displayed={st.DisplayedCount()} collectionValue={st.CollectionValue()} prestige={st.Prestige} suppliers={string.Join(",", st.UnlockedSuppliers)} kept={st.Stats.SpecimensKept}");
            Snap("cabinet");
        }

        /// <summary>Run G (menus): drive the tablet and pause/settings purely with the virtual gamepad and log what has focus.</summary>
        public void RunControllerMenus() { if (!Running) StartCoroutine(ControllerMenus()); }

        private IEnumerator Pad(GamepadButton b)
        {
            yield return D.PadTap(b, 0.09f);
            yield return new WaitForSecondsRealtime(0.25f);   // the pause menu freezes scaled time
        }

        private IEnumerator ControllerMenus()
        {
            Running = true;
            UseGamepad = true;
            var d = D;
            var tablet = UI.TabletUI.Instance;
            var pause = UI.PauseMenu.Instance;
            L($"== ControllerMenus cash={S.State.Cash}");
            Phase = "tablet";
            yield return Pad(GamepadButton.Select);
            L($"tablet open={tablet.IsOpen} tab={tablet.CurrentTab} focus={tablet.FocusedText}");
            yield return Pad(GamepadButton.DpadDown);
            L($"down: focus={tablet.FocusedText}");
            yield return Pad(GamepadButton.DpadDown);
            L($"down: focus={tablet.FocusedText}");
            yield return Pad(GamepadButton.DpadRight);
            L($"right: tab={tablet.CurrentTab} focus={tablet.FocusedText}");
            yield return Pad(GamepadButton.DpadDown);
            L($"down: tab={tablet.CurrentTab} focus={tablet.FocusedText}");
            yield return Pad(GamepadButton.DpadDown);
            L($"down: focus={tablet.FocusedText}");
            yield return Pad(GamepadButton.DpadUp);
            L($"up: focus={tablet.FocusedText}");
            yield return Pad(GamepadButton.DpadUp);
            L($"up: focus={tablet.FocusedText}");
            yield return Pad(GamepadButton.DpadLeft);
            L($"left(on tab row): tab={tablet.CurrentTab} focus={tablet.FocusedText}");
            // buy the first affordable thing on the suppliers tab, then check focus survived the rebuild
            yield return Pad(GamepadButton.DpadDown);
            float cashBefore = S.State.Cash;
            string before = tablet.FocusedText;
            yield return Pad(GamepadButton.South);
            L($"A on '{before}': cash {cashBefore}->{S.State.Cash} crates={S.Crates.Count} focus={tablet.FocusedText} open={tablet.IsOpen}");
            yield return Pad(GamepadButton.East);
            L($"B: tablet open={tablet.IsOpen} pause open={pause.IsOpen}");

            // V5: the collection page's per-piece buttons (favourite, name, consign) are reachable on the pad
            Phase = "collection";
            SpawnTestStock(2, 0f);
            foreach (var en in new List<SpecimenEntity>(S.Entities.Values)) if (en.IsOpened && en.Record.Location == SpecimenLocation.World) { var cab = Find<Workshop.DisplayCabinet>(); foreach (var z in cab.Slots) if (z.IsEmpty && !z.Locked && z.gameObject.activeInHierarchy && z.RefusalReason(en) == null && z.FitRefusal(en) == null) { z.Place(en, true); break; } break; }
            S.State.Stats.SpecimensSold += 60; S.State.Stats.CustomersServed += 20; S.RaiseStateChanged();
            if (!tablet.IsOpen) yield return Pad(GamepadButton.Select);
            tablet.ShowTab(2); yield return null;
            string seenConsign = null, seenName = null, seenFav = null;
            for (int i = 0; i < 40 && (seenConsign == null || seenName == null); i++)
            {
                yield return Pad(GamepadButton.DpadDown);
                string f = (tablet.FocusedText ?? "").Replace("Button:", "");
                if (f.StartsWith("Consign")) seenConsign = f; if (f == "Name it" || f == "Rename") seenName = f; if (f.Contains("Favourite") || f == "Unstar") seenFav = f;
            }
            L($"collection pad focus: favourite='{seenFav}' name='{seenName}' consign='{seenConsign}'");
            if (seenName != null)
            {
                // land on the name button again and press it: the field opens (typing is keyboard-only by design)
                for (int i = 0; i < 40 && (tablet.FocusedText ?? "").Replace("Button:", "") != seenName; i++) yield return Pad(GamepadButton.DpadUp);
                yield return Pad(GamepadButton.South);
                L($"A on '{seenName}': focus={tablet.FocusedText} open={tablet.IsOpen}");
            }
            yield return Pad(GamepadButton.East);
            L($"B: tablet open={tablet.IsOpen}");

            Phase = "pause";
            yield return Pad(GamepadButton.Start);
            L($"start: pause open={pause.IsOpen} settings={pause.SettingsVisible} focus={pause.FocusedText} timeScale={Time.timeScale}");
            yield return Pad(GamepadButton.DpadDown);
            L($"down: focus={pause.FocusedText}");
            yield return Pad(GamepadButton.South);
            L($"A: settings={pause.SettingsVisible} tab={pause.Settings.Tab} focus={pause.FocusedText}");
            yield return Pad(GamepadButton.DpadDown);
            string sBefore = pause.FocusedText;
            L($"down: focus={sBefore}");
            yield return Pad(GamepadButton.DpadRight);
            L($"right on slider: {sBefore} -> {pause.FocusedText}");
            yield return Pad(GamepadButton.DpadLeft);
            L($"left on slider: -> {pause.FocusedText}");
            yield return Pad(GamepadButton.RightShoulder);
            L($"RB: tab={pause.Settings.Tab} focus={pause.FocusedText}");
            yield return Pad(GamepadButton.DpadDown);
            yield return Pad(GamepadButton.DpadDown);
            L($"down twice: focus={pause.FocusedText}");
            yield return Pad(GamepadButton.RightShoulder);
            yield return Pad(GamepadButton.RightShoulder);
            L($"RB twice: tab={pause.Settings.Tab} focus={pause.FocusedText}");
            yield return Pad(GamepadButton.DpadRight);
            L($"right on choice: focus={pause.FocusedText} confirm={pause.Settings.ConfirmOpen} display={GameSettings.DisplayApplied}");
            yield return Pad(GamepadButton.DpadLeft);
            L($"left in confirm: focus={pause.FocusedText}");
            yield return Pad(GamepadButton.East);
            L($"B in confirm: confirm={pause.Settings.ConfirmOpen} settings={pause.SettingsVisible} mode={GameSettings.Current.DisplayMode} focus={pause.FocusedText}");
            yield return Pad(GamepadButton.LeftShoulder);
            yield return Pad(GamepadButton.LeftShoulder);
            yield return Pad(GamepadButton.LeftShoulder);
            L($"LB thrice: tab={pause.Settings.Tab} focus={pause.FocusedText}");
            yield return Pad(GamepadButton.East);
            L($"B in settings: pause open={pause.IsOpen} settings={pause.SettingsVisible} focus={pause.FocusedText}");
            yield return Pad(GamepadButton.East);
            L($"B on pause page: pause open={pause.IsOpen} timeScale={Time.timeScale} gameplay={GameInput.GameplayEnabled} inMenu={CursorController.InMenu}");
            yield return Pad(GamepadButton.Start);
            yield return Pad(GamepadButton.Start);
            L($"start twice: pause open={pause.IsOpen} timeScale={Time.timeScale}");
            Phase = "done";
            Running = false;
        }

        /// <summary>
        /// Run F (edge cases): save/reload with a crate still closed, mid-lid-animation, freshly opened, and with a rock in hand.
        /// Uses the real save path (FlushSave + ContinueGame) and reports what came back where.
        /// </summary>
        public void RunSaveScenarios() { if (!Running) StartCoroutine(SaveScenarios()); }

        private string WorldSummary()
        {
            var s = S;
            int origin = 0, inCrate = 0, held = 0, world = 0;
            foreach (var e in s.Entities.Values)
            {
                if (e.transform.position.sqrMagnitude < 0.01f) origin++;
                if (e.Record.Location == SpecimenLocation.InCrate) inCrate++;
                if (e.Record.Location == SpecimenLocation.Held) held++;
                if (e.Record.Location == SpecimenLocation.World) world++;
            }
            var sb = new StringBuilder($"entities={s.Entities.Count} atOrigin={origin} inCrate={inCrate} held={held} world={world} crates=[");
            foreach (var c in s.Crates.Values) sb.Append($"{c.Record.Id}:{(c.IsOpened ? "open" : "closed")}/{c.RemainingRocks} ");
            sb.Append("]");
            return sb.ToString();
        }

        private IEnumerator SaveScenarios()
        {
            Running = true;
            var s = S;
            L($"== SaveScenarios cash={s.State.Cash} {WorldSummary()}");
            // 1. closed crate survives a reload closed, with its rocks unspawned
            Phase = "closed-crate";
            var before = new HashSet<string>(s.Crates.Keys);
            if (!s.BuyCrate("local", out string err)) { L("buy failed: " + err); Running = false; yield break; }
            yield return new WaitForSeconds(1.6f);
            CrateEntity crate = null;
            foreach (var kv in s.Crates) if (!before.Contains(kv.Key)) crate = kv.Value;
            string crateId = crate.Record.Id;
            L($"bought {crateId}: {WorldSummary()}");
            s.FlushSave("test");
            s.ContinueGame();
            yield return null;
            crate = s.Crates[crateId];
            L($"reload with closed crate: {WorldSummary()}");
            // 2. save during the lid animation: comes back closed, then opens normally
            Phase = "mid-open";
            crate.Interact(P);
            yield return new WaitForSeconds(0.25f);
            L($"mid-animation: opened={crate.IsOpened} {WorldSummary()}");
            s.FlushSave("test");
            s.ContinueGame();
            yield return null;
            crate = s.Crates[crateId];
            L($"reload mid-animation: {WorldSummary()}");
            // 3. open fully, reload, positions must match
            Phase = "opened";
            crate.Interact(P);
            yield return new WaitForSeconds(1.6f);
            var poses = new Dictionary<string, Vector3>();
            foreach (var e in s.Entities.Values) if (e.Record.CrateId == crateId) poses[e.Id] = e.transform.position;
            L($"opened: opened={crate.IsOpened} {WorldSummary()}");
            s.FlushSave("test");
            s.ContinueGame();
            yield return null;
            crate = s.Crates[crateId];
            float maxMove = 0f; int missing = 0;
            foreach (var kv in poses) { var e = s.GetEntity(kv.Key); if (e == null) missing++; else maxMove = Mathf.Max(maxMove, (e.transform.position - kv.Value).magnitude); }
            L($"reload opened: {WorldSummary()} maxMove={maxMove:F3} missing={missing}");
            // 4. a rock in hand comes back as a loose rock in front of the player, not stuck to nothing
            Phase = "held";
            SpecimenEntity rock = null;
            foreach (var e in s.Entities.Values) if (e.Record.CrateId == crateId && e.Record.Location == SpecimenLocation.InCrate) { rock = e; break; }
            if (rock != null)
            {
                if (rock.Zone != null) rock.Zone.Take(rock);
                P.PickUp(rock);
                yield return new WaitForSeconds(0.4f);
                L($"held {rock.Id}: loc={rock.Record.Location} {WorldSummary()}");
                s.FlushSave("test");
                s.ContinueGame();
                yield return new WaitForSeconds(0.6f);
                var back = s.GetEntity(rock.Id);
                L($"reload held: loc={back.Record.Location} pos={back.transform.position:F2} playerHeld={(P.Held != null)} {WorldSummary()}");
            }
            Phase = "done";
            Running = false;
        }

        /// <summary>Dev fixture: opened, appraised specimens on the workshop floor plus some cash, so retail can be exercised without an hour of cracking.</summary>
        public string SpawnTestStock(int count = 5, float cash = 300f)
        {
            ulong[] seeds = { 0x7D1UL, 0xACCUL, 0xE53UL, 0x8BFUL, 0x3A7F1UL, 0xBCFUL, 0xF02UL, 0xB5FUL };
            int n = 0;
            for (int i = 0; i < Mathf.Min(count, seeds.Length); i++)
            {
                var r = S.CreateSpecimenRecord(seeds[i], "test", "");
                r.Location = SpecimenLocation.World;
                r.Condition.Opened = true;
                r.Appraised = true;
                r.AppraisedValue = Valuation.DamagedValue(r.Geology, 0f, 0f);
                // open floor in the middle of the workshop, clear of the pallets, the outbox and the benches
                var pos = new Vector3(-1.5f + (i % 3) * 0.5f, 0.12f, -0.55f + (i / 3) * 0.55f);
                var e = S.Spawn(r, pos, Quaternion.identity, false);
                pos.y = e.RestHeightOffset(false) + 0.004f;   // resting on the floor, whatever its size
                e.SetPose(pos, Quaternion.identity);
                e.SetPhysics(true);
                n++;
            }
            if (cash > 0f) S.AddCash(cash, "test");
            S.RaiseStateChanged();
            return $"spawned {n} appraised test specimens";
        }

        /// <summary>
        /// Run R (retail): put the best appraised specimens on the sales fixtures, bring customers in, and work the
        /// register through the real prompts. Logs who bought what for how much and whether anything phased.
        /// </summary>
        public void RunRetailCycle(int customers = 3) { if (!Running) StartCoroutine(RetailCycle(customers)); }

        public void RunRetailSaveScenarios() { if (!Running) StartCoroutine(RetailSaveScenarios()); }

        private static string Chk(bool ok) => ok ? "ok" : "FAIL";

        /// <summary>
        /// Retail save integrity: stock survives a reload with its prices; a reserved (carried) piece goes back on the
        /// shelf on reload; a sold piece never comes back and cannot be sold twice; stats persist.
        /// </summary>
        private IEnumerator RetailSaveScenarios()
        {
            Running = true;
            Phase = "retail-save";
            var s = S;
            yield return EnsureShop();
            var shop = Retail.RetailShop.Instance;
            if (shop == null) { L("no shop"); Running = false; yield break; }
            // 1. stock three slots directly (the walking path is covered by RetailCycle)
            var stock = new List<SpecimenEntity>();
            foreach (var e in s.Entities.Values) if (e.IsOpened && e.Record.Appraised && e.Record.Location == SpecimenLocation.World) stock.Add(e);
            if (stock.Count == 0) { yield return StockDirect(3); }   // a fresh career has nothing lying about to shelve
            int placed = 0;
            foreach (var e in stock)
            {
                PlacementZone free = null;
                foreach (var z in shop.SaleSlots) if (z.IsEmpty && !z.Locked) { free = z; break; }
                if (free == null) break;
                if (e.Zone != null) e.Zone.Take(e);
                free.Place(e);          // the real path: the shop prices it and refreshes the card
                placed++;
                if (placed >= 3) break;
            }
            yield return null;
            var ids = new List<string>(); var prices = new Dictionary<string, float>();
            foreach (var z in shop.SaleSlots) { var e = z.First; if (e != null) { ids.Add(e.Id); prices[e.Id] = e.Record.AskingPrice; } }
            L($"stocked {placed}: forSale={s.State.ForSaleCount()} prices=[{string.Join(",", prices.Values)}]");
            L($"  prices set: {Chk(prices.Count > 0 && prices.Values.All(p => p > 0f))}");
            s.FlushSave("test");
            s.ContinueGame();
            yield return null;
            yield return null;
            int back = 0; bool pricesKept = true; bool onSlots = true;
            foreach (var id in ids) { var e = s.GetEntity(id); if (e == null) continue; back++; if (!Mathf.Approximately(e.Record.AskingPrice, prices[id])) pricesKept = false; if (e.Record.Location != SpecimenLocation.SaleSlot || e.Zone == null) onSlots = false; }
            L($"reload stocked: back={back}/{ids.Count} onSlots={Chk(onSlots)} pricesKept={Chk(pricesKept)} forSale={s.State.ForSaleCount()} customers={shop.Customers.Count} {Chk(shop.Customers.Count == 0)}");
            L(Core.CollisionAudit.Report("reload stocked"));

            // 2. a customer carrying a piece: reload puts it back on a shelf, nobody keeps it
            Phase = "retail-reserved";
            var c = shop.SpawnNow();
            float t = 0f;
            while (c != null && c.Wanted == null && c.State != Retail.Customer.Phase.Leaving && c.State != Retail.Customer.Phase.Done && t < 40f) { t += Time.deltaTime; yield return null; }
            if (c == null || c.Wanted == null) { L($"  customer did not pick anything (state={(c != null ? c.State.ToString() : "gone")}), retrying"); c = shop.SpawnNow(); t = 0f; while (c != null && c.Wanted == null && c.State != Retail.Customer.Phase.Leaving && t < 40f) { t += Time.deltaTime; yield return null; } }
            if (c != null && c.Wanted != null)
            {
                string wantedId = c.Wanted.Id;
                yield return new WaitForSeconds(0.5f);
                int forSaleBefore = s.State.ForSaleCount();
                L($"reserved {wantedId} by {c.Archetype.Name} state={c.State} loc={c.Wanted.Record.Location} forSale={forSaleBefore}");
                s.FlushSave("test");
                s.ContinueGame();
                yield return null;
                yield return null;
                var w = s.GetEntity(wantedId);
                L($"reload reserved: entity={(w != null ? "present" : "MISSING")} loc={(w != null ? w.Record.Location.ToString() : "-")} onSlot={Chk(w != null && w.Zone != null && w.Record.Location == SpecimenLocation.SaleSlot)} parentIsWorld={Chk(w != null && w.transform.parent == null)} forSale={s.State.ForSaleCount()} {Chk(s.State.ForSaleCount() == forSaleBefore)} customers={shop.Customers.Count}");
                L(Core.CollisionAudit.Report("reload reserved"));
            }
            else L("  no customer reserved anything; skipping reserved-reload check");

            // 3. sell one at the register, then reload: gone for good, stats kept, no double sale
            Phase = "retail-sold";
            var station2 = FindAnyObjectByType<GeodeEmpire.Checkout.CheckoutStation>();
            c = shop.SpawnNow();
            t = 0f;
            while (c != null && c.State != Retail.Customer.Phase.AtCounter && c.State != Retail.Customer.Phase.Leaving && c.State != Retail.Customer.Phase.Done && t < 90f) { t += Time.deltaTime; yield return null; }
            if (c != null && c.State == Retail.Customer.Phase.AtCounter && c.Wanted != null)
            {
                string soldId = c.Wanted.Id;
                float price = c.Wanted.Record.AskingPrice;
                float cashBefore = s.State.Cash;
                int salesBefore = s.State.Stats.RetailSales;
                station2.Enter();
                yield return station2.CompleteFromHere(0.1f);
                // the piece leaves with the buyer, so wait for them out of the shop before asking whether it is gone
                float outWait = 0f;
                while (c != null && c.State != Retail.Customer.Phase.Done && outWait < 25f) { outWait += Time.deltaTime; yield return null; }
                yield return null; yield return null;
                bool again = shop.CompleteSale(c);
                var rec = s.State.FindSpecimen(soldId);
                L($"sold {soldId} for {price}: cash {cashBefore}->{s.State.Cash} {Chk(Mathf.Approximately(s.State.Cash, cashBefore + price))} loc={rec.Location} {Chk(rec.Location == SpecimenLocation.Sold)} entityGone={Chk(s.GetEntity(soldId) == null)} secondSale={Chk(!again)} sales={s.State.Stats.RetailSales} {Chk(s.State.Stats.RetailSales == salesBefore + 1)}");
                s.FlushSave("test");
                s.ContinueGame();
                yield return null;
                yield return null;
                rec = s.State.FindSpecimen(soldId);
                L($"reload sold: entity={Chk(s.GetEntity(soldId) == null)} recordLoc={rec.Location} cash={s.State.Cash} {Chk(Mathf.Approximately(s.State.Cash, cashBefore + price))} sales={s.State.Stats.RetailSales} {Chk(s.State.Stats.RetailSales == salesBefore + 1)} forSale={s.State.ForSaleCount()}");
            }
            else L($"  no customer reached the counter (state={(c != null ? c.State.ToString() : "gone")}); sale-reload check skipped");
            L(Core.CollisionAudit.Report("retail save end"));
            Phase = "done";
            Running = false;
        }

        public void RunSettingsMatrix() { if (!Running) StartCoroutine(SettingsMatrix()); }
        public void RunUiRenderQa() { if (!Running) StartCoroutine(UiRenderQa()); }

        /// <summary>
        /// V6 §66: lay every screen out at 1920, 2560 and 3840, at two interface scales, and measure it. The panel
        /// is rendered into a texture of the target size so each pass is a real layout rather than a proxy for one.
        /// The instrument is proved first: four faults are planted and each must be caught.
        /// </summary>
        private IEnumerator UiRenderQa()
        {
            Running = true;
            Phase = "ui-qa";
            var panel = Resources.Load<UnityEngine.UIElements.PanelSettings>("UI/GeodePanelSettings");
            if (panel == null) { L("no panel settings"); Running = false; yield break; }
            var g = GameSettings.Current;
            float uiScaleWas = g.UiScale;
            var refWas = panel.referenceResolution;
            var texWas = panel.targetTexture;
            int pass = 0, fail = 0;
            void Chk2(string what, bool ok, string note) { L($"  {what,-46} {(ok ? "ok" : "FAIL")}{(string.IsNullOrEmpty(note) ? "" : "  " + note)}"); if (ok) pass++; else fail++; }

            // --- the instrument first: plant four faults and require each to be found -----------------
            UI.UiRenderAudit.PlantNegatives();
            yield return null; yield return null;
            var planted = UI.UiRenderAudit.Run(Screen.width);
            bool sawTiny = false, sawClip = false, sawTrunc = false, sawSmallBtn = false;
            foreach (var f in planted)
            {
                if (f.Where.Contains("PlantedTiny") && f.Kind == "unreadable") sawTiny = true;
                if (f.Where.Contains("PlantedClipped") && (f.Kind == "clipped" || f.Kind == "off-screen")) sawClip = true;
                if (f.Where.Contains("PlantedTruncated") && f.Kind == "truncated") sawTrunc = true;
                if (f.Where.Contains("PlantedTinyButton") && f.Kind == "tiny-control") sawSmallBtn = true;
            }
            L($"== UiRenderQa: negative controls ({planted.Count} findings with the faults planted)");
            Chk2("instrument catches unreadable text", sawTiny, "");
            Chk2("instrument catches a clipped element", sawClip, "");
            Chk2("instrument catches a truncated label", sawTrunc, "");
            Chk2("instrument catches a control too small to hit", sawSmallBtn, "");
            UI.UiRenderAudit.ClearNegatives();
            yield return null; yield return null;

            // --- the real screens, at each resolution and interface scale -----------------------------
            var sizes = new[] { new Vector2Int(1920, 1080), new Vector2Int(2560, 1440), new Vector2Int(3840, 2160) };
            var scales = new[] { 1f, 1.4f };
            var screens = new (string name, System.Action open, System.Action close)[]
            {
                ("free roam", () => { }, () => { }),
                ("tablet", () => UI.TabletUI.Instance?.Open(), () => UI.TabletUI.Instance?.Close()),
                ("inventory", () => UI.InventoryUI.Instance?.Open(), () => UI.InventoryUI.Instance?.Close()),
                ("pause + settings", () => { UI.PauseMenu.Instance?.Open(); UI.PauseMenu.Instance?.Settings?.Select(1); }, () => UI.PauseMenu.Instance?.Close()),
            };
            int total = 0;
            foreach (var size in sizes)
            {
                var rt = new RenderTexture(size.x, size.y, 0) { name = $"UiQa{size.x}" };
                rt.Create();
                panel.targetTexture = rt;
                foreach (float scale in scales)
                {
                    g.UiScale = scale; g.ApplyUiScale();
                    yield return null; yield return null; yield return null;
                    foreach (var screen in screens)
                    {
                        screen.open();
                        yield return null; yield return null; yield return null;
                        var found = UI.UiRenderAudit.Run(size.x);
                        total += found.Count;
                        Chk2($"{size.x}x{size.y} @ {scale:F1}x  {screen.name}", found.Count == 0, found.Count == 0 ? "" : found.Count + " findings");
                        for (int i = 0; i < found.Count && i < 6; i++) L("      " + found[i]);
                        screen.close();
                        yield return null;
                    }
                }
                panel.targetTexture = null;
                rt.Release();
                Object.Destroy(rt);
                yield return null;
            }
            panel.targetTexture = texWas;
            g.UiScale = uiScaleWas; g.ApplyUiScale();
            panel.referenceResolution = refWas;
            yield return null;
            L($"ui render qa: pass={pass} fail={fail} findings={total}");
            Phase = "done";
            Running = false;
        }

        public void RunTitleFlow() { if (!Running) StartCoroutine(TitleFlow()); }

        private static string SceneName => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        private static string AnyFocused()
        {
            var doc = FindAnyObjectByType<UnityEngine.UIElements.UIDocument>();
            return doc != null && doc.rootVisualElement != null ? UI.UiKit.FocusedText(doc.rootVisualElement) : "(no panel)";
        }
        private IEnumerator WaitScene(string name, float timeout = 15f)
        {
            float t = 0f;
            while (SceneName != name && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(1.2f);
        }

        /// <summary>
        /// Title → Continue → workshop → pause → save and quit → title → New Game (cancel) → Continue, all on the pad.
        /// The career must come back identical after the round trip.
        /// </summary>
        private IEnumerator TitleFlow()
        {
            Running = true;
            Phase = "title";
            UseGamepad = true;
            var s = S;
            float cash = s.State.Cash; int specimens = s.State.Specimens.Count;
            s.FlushSave("test");
            L($"== TitleFlow cash={cash} specimens={specimens}");
            CursorController.Reset(); CursorController.EnterMenu();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
            yield return WaitScene("Title");
            L($"title: scene={SceneName} focus={AnyFocused()} session={(GameSession.Instance == null ? "none" : "ALIVE")}");
            yield return Pad(GamepadButton.South);            // Continue
            yield return WaitScene("Workshop");
            s = S;
            L($"continue: scene={SceneName} cash={s.State.Cash} {Chk(Mathf.Approximately(s.State.Cash, cash))} specimens={s.State.Specimens.Count} {Chk(s.State.Specimens.Count == specimens)} entities={s.Entities.Count} gameplay={GameInput.GameplayEnabled} inMenu={CursorController.InMenu}");
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Pad(GamepadButton.Start);
            var pause = UI.PauseMenu.Instance;
            L($"pause: open={pause.IsOpen} focus={pause.FocusedText}");
            yield return Pad(GamepadButton.DpadDown);
            yield return Pad(GamepadButton.DpadDown);
            L($"down twice: focus={pause.FocusedText}");
            yield return Pad(GamepadButton.South);            // Save and quit to title
            yield return WaitScene("Title");
            L($"quit to title: scene={SceneName} focus={AnyFocused()} timeScale={Time.timeScale} session={(GameSession.Instance == null ? "none" : "ALIVE")}");
            yield return Pad(GamepadButton.DpadDown);         // New Game
            L($"down: focus={AnyFocused()}");
            yield return Pad(GamepadButton.South);
            L($"A on New Game: focus={AnyFocused()}");
            yield return Pad(GamepadButton.East);             // cancel the erase
            L($"B on confirm: focus={AnyFocused()}");
            yield return Pad(GamepadButton.DpadUp);
            yield return Pad(GamepadButton.South);            // Continue again
            yield return WaitScene("Workshop");
            s = S;
            L($"continue again: scene={SceneName} cash={s.State.Cash} {Chk(Mathf.Approximately(s.State.Cash, cash))} specimens={s.State.Specimens.Count} {Chk(s.State.Specimens.Count == specimens)} forSale={s.State.ForSaleCount()} customers={(Retail.RetailShop.Instance != null ? Retail.RetailShop.Instance.Customers.Count : -1)}");
            L(Core.CollisionAudit.Report("after title round trip"));
            UseGamepad = false;
            Phase = "done";
            Running = false;
        }

        /// <summary>
        /// Every visible setting: change it, confirm the thing that reads it changed, then confirm the file round-trips.
        /// Restores defaults at the end.
        /// </summary>
        private IEnumerator SettingsMatrix()
        {
            Running = true;
            Phase = "settings";
            var g = GameSettings.Current;
            var fpc = FindAnyObjectByType<FirstPersonController>();
            var hud = UI.HudController.Instance;
            var cam = Camera.main;
            g.ResetAll(); g.Apply();
            yield return null;
            int pass = 0, fail = 0;
            void Row(string name, string from, string to, bool ok) { L($"  {name,-24} {from,-10} -> {to,-12} {Chk(ok)}"); if (ok) pass++; else fail++; }

            // camera
            g.FieldOfView = 85f; g.Apply(); yield return null; yield return null; yield return null;
            Row("Field of view", "70", "85", Mathf.Abs(cam.fieldOfView - 85f) < 0.6f);
            g.HeadBobAmount = 0f; g.Apply(); Row("Head bob", "100%", "0%", Mathf.Approximately(g.EffectiveHeadBob, 0f));
            g.HeadBobAmount = 1f; g.ReducedMotion = true; g.Apply(); Row("Reduced motion", "off", "on", g.EffectiveHeadBob == 0f && g.EffectiveShake == 0f);
            g.ReducedMotion = false; g.CameraShake = 0.3f; g.Apply(); Row("Camera shake", "100%", "30%", Mathf.Approximately(g.EffectiveShake, 0.3f));
            g.UiScale = 1.2f; g.Apply(); Row("Interface scale", "1.00x", "1.20x", GameSettings.PanelReferenceResolution == new Vector2Int(1600, 900));
            g.CrosshairVisible = false; g.Apply(); yield return null; Row("Show crosshair", "on", "off", hud == null || !hud.CrosshairShown);
            g.CrosshairVisible = true; g.Apply(); yield return null; Row("Show crosshair back", "off", "on", hud == null || hud.CrosshairShown);

            // controls: sensitivity feeds the look math directly; measure a real mouse delta
            g.MouseSensitivity = 1f; g.InvertY = false; g.Apply();
            float yaw0 = fpc.Yaw; D.MouseDelta(80f, 0f); yield return null; yield return null; float d1 = Mathf.DeltaAngle(yaw0, fpc.Yaw);
            g.MouseSensitivity = 2f; g.Apply(); yaw0 = fpc.Yaw; D.MouseDelta(80f, 0f); yield return null; yield return null; float d2 = Mathf.DeltaAngle(yaw0, fpc.Yaw);
            Row("Mouse sensitivity", $"{d1:F1}deg", $"{d2:F1}deg", d1 > 0.5f && Mathf.Abs(d2 / Mathf.Max(0.01f, d1) - 2f) < 0.25f);
            float pitch0 = fpc.Pitch; D.MouseDelta(0f, 40f); yield return null; yield return null; float p1 = fpc.Pitch - pitch0;
            g.InvertY = true; g.Apply(); pitch0 = fpc.Pitch; D.MouseDelta(0f, 40f); yield return null; yield return null; float p2 = fpc.Pitch - pitch0;
            Row("Invert look Y", $"{p1:F1}", $"{p2:F1}", Mathf.Abs(p1) > 0.3f && Mathf.Sign(p1) != Mathf.Sign(p2));
            g.InvertY = false; g.MouseSensitivity = 1f;
            g.GamepadSensitivity = 2f; g.Apply(); Row("Controller sensitivity", "1.00x", "2.00x", Mathf.Approximately(GameSettings.Current.GamepadSensitivity, 2f));
            g.StickDeadzone = 0.3f; g.Apply(); Row("Stick deadzone", "12%", "30%", Mathf.Approximately(UnityEngine.InputSystem.InputSystem.settings.defaultDeadzoneMin, 0.3f));
            int pulses = Haptics.PulseCount; g.Vibration = 1f; g.Apply(); fpc.Impulse(0.5f); int after1 = Haptics.PulseCount;
            g.Vibration = 0f; g.Apply(); fpc.Impulse(0.5f); int after2 = Haptics.PulseCount;
            Row("Controller vibration", "100%", "0%", after1 == pulses + 1 && after2 == after1);
            Haptics.Stop();

            // gameplay
            g.ShowTutorial = false; g.Apply(); Row("Tutorial hints", "on", "off", Tutorial.Current == null);
            g.ShowTutorial = true; g.Apply();

            // graphics
            var rp = GameSettings.Pipeline;
            g.ApplyPreset(0); g.Apply(); Row("Quality preset Low", "Medium", "Low", Mathf.Approximately(rp.renderScale, 0.8f) && rp.msaaSampleCount == 1);
            g.ApplyPreset(2); g.Apply(); Row("Quality preset High", "Low", "High", rp.msaaSampleCount == 4 && rp.shadowDistance > 20f);
            g.ShadowQuality = 0; g.RefreshPresetFromParts(); g.Apply(); Row("Shadows off", "High", "Off", rp.shadowDistance == 0f && g.QualityPreset == 3);
            g.AntiAliasing = 3; g.Apply(); Row("Anti-aliasing 8x", "4x", "8x", rp.msaaSampleCount == 8);
            g.RenderScale = 1.2f; g.Apply(); Row("Render scale", "100%", "120%", Mathf.Approximately(rp.renderScale, 1.2f));
            g.PostProcessing = false; g.Apply(); Row("Post-processing", "on", "off", !cam.GetUniversalAdditionalCameraData().renderPostProcessing);
            g.PostProcessing = true; g.Apply();
            g.Brightness = 1.3f; g.Apply(); Row("Brightness", "100%", "130%", Mathf.Abs(GameSettings.CurrentExposure - (GameSettings.BaseExposure + 0.48f)) < 0.01f);
            g.VSync = false; g.Apply(); Row("VSync", "on", "off", QualitySettings.vSyncCount == 0);
            g.FrameRateLimit = 60; g.Apply(); Row("Frame-rate limit", "Uncapped", "60", Application.targetFrameRate == 60);
            g.DisplayMode = 2; g.ResolutionWidth = 1280; g.ResolutionHeight = 720; g.ApplyDisplay(); Row("Display mode/resolution", "Fullscreen native", "Windowed 1280x720", GameSettings.DisplayApplied == "1280x720 Windowed");

            // audio
            g.MasterVolume = 0.4f; g.Apply(); Row("Master volume", "90%", "40%", Mathf.Approximately(AudioListener.volume, 0.4f));
            g.SfxVolume = 0.5f; g.Apply(); Row("Effects volume", "100%", "50%", Mathf.Approximately(Audio.WorkshopAudio.SfxVolume, 0.5f));
            g.UiVolume = 0.2f; g.Apply(); Row("Interface volume", "80%", "20%", Mathf.Approximately(Audio.WorkshopAudio.UiVolume, 0.2f));
            var amb = FindAnyObjectByType<Audio.AmbiencePlayer>();
            g.AmbienceVolume = 0.3f; g.Apply(); Row("Ambience volume", "80%", "30%", amb == null || Mathf.Approximately(amb.SourceVolume, 0.15f));

            // controls: rebinding, the whole way round (V6 §62) — interaction, runtime effect, conflict, save, reload
            InputBindings.ResetAll();
            string wasE = InputBindings.Display("Interact", InputBindings.KeyboardScheme);
            var interact = InputBindings.Find("Interact");
            int ib = InputBindings.BindingIndex(interact, InputBindings.KeyboardScheme);
            bool listening = false;
            InputBindings.StartRebind("Interact", InputBindings.KeyboardScheme, _ => { });
            listening = InputBindings.Listening;
            D.KeyDown(UnityEngine.InputSystem.Key.J);
            for (int i = 0; i < 30 && InputBindings.Listening; i++) yield return null;
            D.KeyUp();
            yield return null;
            Row("Rebind Interact", wasE, InputBindings.Display("Interact", InputBindings.KeyboardScheme),
                listening && InputBindings.Display("Interact", InputBindings.KeyboardScheme) == "J");
            Row("Prompt follows binding", "E", GameInput.Glyph("Interact"), GameInput.Glyph("Interact") == "J");
            Row("Tutorial follows binding", "{Interact}", Tutorial.Format("{Interact}"), Tutorial.Format("{Interact}") == "J");
            // the conflict: give J to the loupe and Interact must lose it, and say so
            InputBindings.StartRebind("Loupe", InputBindings.KeyboardScheme, _ => { });
            D.KeyDown(UnityEngine.InputSystem.Key.J);
            for (int i = 0; i < 30 && InputBindings.Listening; i++) yield return null;
            D.KeyUp();
            yield return null;
            Row("Binding conflict", "Interact=J", "Loupe=J, Interact unbound",
                InputBindings.Display("Loupe", InputBindings.KeyboardScheme) == "J"
                && string.IsNullOrEmpty(InputBindings.Display("Interact", InputBindings.KeyboardScheme))
                && InputBindings.LastConflict == "Interact");
            // a gamepad binding is separate from the keyboard one
            string padWas = InputBindings.Display("Drop", InputBindings.GamepadScheme);
            InputBindings.StartRebind("Drop", InputBindings.GamepadScheme, _ => { });
            D.PadState(Vector2.zero, Vector2.zero, 0f, 0f, GamepadButton.North);
            for (int i = 0; i < 30 && InputBindings.Listening; i++) yield return null;
            D.PadState(Vector2.zero, Vector2.zero, 0f, 0f);
            yield return null;
            Row("Rebind on the pad", padWas, InputBindings.Display("Drop", InputBindings.GamepadScheme),
                InputBindings.Display("Drop", InputBindings.GamepadScheme) == "Y"
                && InputBindings.Display("Drop", InputBindings.KeyboardScheme) == "G");
            string bindingsJson = GameSettings.Current.Bindings;
            Row("Bindings saved", "-", bindingsJson.Length + " bytes", bindingsJson.Length > 0);
            // reload from disk and re-apply: the remap has to come back
            var reloaded = GameSettings.Load();
            InputBindings.Asset.RemoveAllBindingOverrides();
            InputBindings.Asset.LoadBindingOverridesFromJson(reloaded.Bindings);
            Row("Bindings reloaded", "-", InputBindings.Display("Loupe", InputBindings.KeyboardScheme),
                InputBindings.Display("Loupe", InputBindings.KeyboardScheme) == "J" && InputBindings.Display("Drop", InputBindings.GamepadScheme) == "Y");
            InputBindings.ResetAll();
            Row("Reset bindings", "remapped", InputBindings.Display("Interact", InputBindings.KeyboardScheme),
                InputBindings.Display("Interact", InputBindings.KeyboardScheme) == "E"
                && InputBindings.Display("Loupe", InputBindings.KeyboardScheme) == "F"
                && InputBindings.Display("Drop", InputBindings.GamepadScheme) == "X"
                && GameSettings.Current.Bindings.Length == 0);

            // persistence: every changed value must come back from disk
            g.Save();
            var back = GameSettings.Load();
            bool persisted = back.FieldOfView == 85f && back.HeadBobAmount == 1f && back.CameraShake == 0.3f && back.UiScale == 1.2f && back.GamepadSensitivity == 2f && back.StickDeadzone == 0.3f
                && back.Vibration == 0f && back.QualityPreset == 3 && back.ShadowQuality == 0 && back.AntiAliasing == 3 && back.RenderScale == 1.2f && back.Brightness == 1.3f && !back.VSync && back.FrameRateLimit == 60
                && back.DisplayMode == 2 && back.ResolutionWidth == 1280 && back.MasterVolume == 0.4f && back.SfxVolume == 0.5f && back.UiVolume == 0.2f && back.AmbienceVolume == 0.3f;
            Row("Persistence (all)", "changed", "reloaded", persisted);
            // reset: defaults return and are written
            g.ResetAll(); g.Apply(); g.ApplyDisplay(); g.Save();
            var def = new GameSettings(); var again = GameSettings.Load();
            Row("Reset all", "changed", "defaults", again.FieldOfView == def.FieldOfView && again.UiScale == 1f && again.QualityPreset == 1 && again.MasterVolume == def.MasterVolume && again.DisplayMode == 0);
            L($"settings matrix: pass={pass} fail={fail}");
            Phase = "done";
            Running = false;
        }

        private IEnumerator RetailCycle(int customers)
        {
            Running = true;
            Phase = "retail-stock";
            yield return EnsureShop();
            var shop = Retail.RetailShop.Instance;
            if (shop == null) { L("no shop"); Running = false; yield break; }
            L($"== RetailCycle cash={S.State.Cash} forSale={S.State.ForSaleCount()}");
            // stock: appraised opened specimens not yet displayed, best first
            var stock = new List<SpecimenEntity>();
            foreach (var e in S.Entities.Values) if (e.IsOpened && e.Record.Appraised && e.Record.Location != SpecimenLocation.DisplaySlot && e.Record.Location != SpecimenLocation.SaleSlot) stock.Add(e);
            stock.Sort((a, b) => b.Record.EstimatedValue().CompareTo(a.Record.EstimatedValue()));
            int placed = 0;
            foreach (var e in stock)
            {
                if (FreeSaleSlot() == null) break;
                yield return FetchRock(e);
                if (P.Held != e) { L("could not fetch " + e.Id); continue; }
                yield return StockHeld();
                placed++;
                if (placed >= 4) break;
            }
            L(Core.CollisionAudit.Report("stocked shelves"));
            Snap("shelf_stocked");
            // step back behind the counter so nobody is standing on a browse point
            if (shop.CounterCustomerPoint != null) { var behind = shop.CounterCustomerPoint.position + new Vector3(-1.2f, 0f, 0f); behind.y = 0f; yield return RouteTo(behind, 0.3f); }
            // customers
            for (int n = 0; n < customers; n++)
            {
                Phase = $"customer {n + 1}";
                var c = shop.SpawnNow();
                if (c == null) { L("spawn failed"); break; }
                L($"customer {c.Id}: {c.Archetype.Name} budget={c.Budget}");
                float t = 0f;
                while (c != null && c.State != Retail.Customer.Phase.AtCounter && c.State != Retail.Customer.Phase.Leaving && c.State != Retail.Customer.Phase.Done && t < 90f)
                {
                    t += Time.deltaTime;
                    // a shopkeeper serves whoever is at the counter, not just the one being watched
                    var other = shop.AtCounter;
                    if (other != null && other != c && other.Wanted != null) { yield return ServeCounter(other); }
                    yield return null;
                }
                if (c == null || c.State != Retail.Customer.Phase.AtCounter)
                {
                    L($"  left without buying (state={(c != null ? c.State.ToString() : "gone")}) after {t:F0}s");
                    while (c != null && c.State != Retail.Customer.Phase.Done) yield return null;
                    continue;
                }
                L($"  at counter after {t:F0}s wanting {c.Wanted.Record.DisplayName} for {c.Wanted.Record.AskingPrice}");
                L(Core.CollisionAudit.Report("customer at counter"));
                // take the register at the counter's own staff datum and work the sale through
                var station = Find<GeodeEmpire.Checkout.CheckoutStation>();
                yield return RouteTo(new Vector3(station.StaffStandPoint.position.x, 0f, station.StaffStandPoint.position.z), 0.35f);
                float cashBefore = S.State.Cash;
                D.LookAt(c.transform.position + Vector3.up * 1.3f);
                yield return null; yield return null;
                Snap("customer_counter");
                station.Enter();
                yield return new WaitForSeconds(0.3f);
                Snap("checkout_card");
                yield return station.CompleteFromHere(0.3f);
                yield return new WaitForSeconds(0.5f);
                L($"  sale: cash {cashBefore} -> {S.State.Cash} retailSales={S.State.Stats.RetailSales} forSale={S.State.ForSaleCount()}");
                while (c != null && c.State != Retail.Customer.Phase.Done) yield return null;
            }
            L($"retail end: cash={S.State.Cash} revenue={S.State.Stats.RetailRevenue} served={S.State.Stats.CustomersServed} leftEmpty={S.State.Stats.CustomersLeftEmptyHanded} customersNow={shop.Customers.Count}");
            Phase = "done";
            Running = false;
        }

        /// <summary>V6 checkout round on the Golf-derived station: one customer, a forced payment method, every physical step captured.</summary>
        public void RunStation(string method = "cash", string size = "") { if (!Running) StartCoroutine(StationCheckout(method, size)); }

        private IEnumerator StationCheckout(string method, string size)
        {
            Running = true;
            Phase = "station-" + method;
            foreach (var st in Workshop.Tutorial.Steps) Workshop.Tutorial.Notify(st.DoneBy);
            yield return EnsureShop();
            var shop = Retail.RetailShop.Instance;
            var station = Find<GeodeEmpire.Checkout.CheckoutStation>();
            if (shop == null || station == null) { L("no shop/station"); Running = false; yield break; }
            Retail.Customer.ForcedMethod = method == "card" ? 1 : 0;
            if (string.IsNullOrEmpty(size)) yield return StockDirect(4); else yield return StockSized(size, 2);
            var drawerBefore = station.Drawer != null ? station.Drawer.Copy() : null;
            // some archetypes cannot afford a big piece; keep sending shoppers in until one actually reaches the counter
            Retail.Customer c = null;
            for (int attempt = 0; attempt < 5 && (c == null || c.State != Retail.Customer.Phase.AtCounter); attempt++)
            {
                c = shop.SpawnNow();
                float w = 0f;
                while (c != null && c.State != Retail.Customer.Phase.AtCounter && c.State != Retail.Customer.Phase.Done && w < 70f) { w += Time.deltaTime; yield return null; }
                if (c != null && c.State == Retail.Customer.Phase.AtCounter && c.Wanted != null) break;
                L($"  attempt {attempt + 1}: {(c == null ? "no spawn" : c.State.ToString())} - trying another shopper");
                while (c != null && c.State != Retail.Customer.Phase.Done) yield return null;
            }
            if (c == null || c.State != Retail.Customer.Phase.AtCounter || c.Wanted == null) { L("no customer at the counter"); Retail.Customer.ForcedMethod = -1; Running = false; yield break; }
            string wantedId = c.Wanted.Id;
            float price = c.Wanted.Record.AskingPrice, cashBefore = S.State.Cash;
            L($"at counter: {c.Archetype.Name} pays by {c.Method} for {c.Wanted.Record.DisplayName} ({c.Wanted.Geology.SizeClass}) {price}");
            var stand = station.StaffStandPoint;
            yield return RouteTo(new Vector3(stand.position.x, 0f, stand.position.z), 0.35f);
            D.LookAt(station.MonitorRig.transform.position);
            yield return null; yield return null;
            Snap("00_arrival");
            station.Enter();
            yield return new WaitForSeconds(0.3f);
            L($"entered: state={station.State} active={station.Active}");
            int shots = 1;
            var seen = new List<string>();
            float guard = 0f, busyShot = 0f, stall = 0f;
            while (station.Tx != null && station.State != GeodeEmpire.Checkout.CheckoutState.TransactionComplete && guard < 90f)
            {
                yield return new WaitForSeconds(0.25f); guard += 0.25f;
                if (station.Busy)
                {
                    busyShot += 0.25f;
                    if (busyShot >= 0.5f) { busyShot = 0f; Snap($"{shots++:00}_{station.State}_busy"); }
                    continue;
                }
                busyShot = 0f;
                string label = station.State.ToString();
                if (seen.Count == 0 || seen[seen.Count - 1] != label)
                {
                    seen.Add(label);
                    var tx = station.Tx;
                    if (tx != null) L($"  {label}: stage={tx.Stage} total={tx.Total:F2} tendered={tx.TenderedTotal:F2} change={tx.ChangeDue:F2} hand={tx.HandTotal:F2} status='{station.StatusLine}'");
                    else L($"  {label}: (station reset)");
                    var cam = Camera.main;
                    string head = c != null ? cam.WorldToViewportPoint(c.transform.position + Vector3.up * 1.62f).ToString("F2") : "(gone)";
                    L($"    cam={cam.transform.position:F2} fov={cam.fieldOfView:F0} head vp={head} drawer={station.DrawerOpen:F2} trace={station.Trace}");
                    Snap($"{shots++:00}_{label}");
                }
                if (!station.HarnessStep())
                {
                    stall += 0.2f;
                    if (stall > 6f) { L($"  STALLED at {station.State} trace={station.Trace} busy={station.Busy} stage={(station.Tx != null ? station.Tx.Stage.ToString() : "-")}"); break; }
                    yield return new WaitForSeconds(0.2f);
                }
                else stall = 0f;
            }
            yield return new WaitForSeconds(0.6f);
            Snap("99_reset");
            var rec = S.State.FindSpecimen(wantedId);
            L($"after: cash {cashBefore} -> {S.State.Cash} {Chk(S.State.Cash > cashBefore)} recordLoc={(rec != null ? rec.Location.ToString() : "missing")} {Chk(rec != null && rec.Location == SpecimenLocation.Sold)} states={string.Join(">", seen)}");
            var drawerAfter = station.Drawer;
            float delta = drawerAfter != null && drawerBefore != null ? drawerAfter.Total - drawerBefore.Total : 0f;
            L($"drawer: {(drawerBefore != null ? drawerBefore.Total : 0f):F2} -> {(drawerAfter != null ? drawerAfter.Total : 0f):F2} delta={delta:F2} expected={(method == "cash" ? price : 0f):F2} {Chk(method != "cash" || Mathf.Abs(delta - price) < 0.011f)}");
            float leaveWait = 0f;
            while (c != null && c.State != Retail.Customer.Phase.Done && leaveWait < 30f) { leaveWait += Time.deltaTime; yield return null; }
            yield return null; yield return null;
            L($"customer gone={Chk(c == null)} entity gone={Chk(S.GetEntity(wantedId) == null)} station idle={Chk(station.Tx == null)} active={station.Active}");
            int leftovers = 0;
            foreach (var go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go.name == "Card" || go.name == "Bag" || go.name.StartsWith("Tender_") || go.name.StartsWith("Change_")) leftovers++;
            L($"leftover checkout objects={leftovers} {Chk(leftovers == 0)}");
            Retail.Customer.ForcedMethod = -1;
            Phase = "done";
            Running = false;
        }

        /// <summary>Repeated customers through the station back to back: the till has to keep balancing sale after sale.</summary>
        public void RunStationRepeat(int customers = 3) { if (!Running) StartCoroutine(StationRepeat(customers)); }

        private IEnumerator StationRepeat(int customers)
        {
            Running = true;
            Phase = "station-repeat";
            foreach (var st in Workshop.Tutorial.Steps) Workshop.Tutorial.Notify(st.DoneBy);
            yield return EnsureShop();
            var shop = Retail.RetailShop.Instance;
            var station = Find<GeodeEmpire.Checkout.CheckoutStation>();
            if (shop == null || station == null) { L("no shop/station"); Running = false; yield break; }
            yield return StockDirect(6);
            var stand = station.StaffStandPoint;
            yield return RouteTo(new Vector3(stand.position.x, 0f, stand.position.z), 0.35f);
            float cash0 = S.State.Cash;
            var drawer0 = station.Drawer != null ? station.Drawer.Copy() : null;
            int served = 0;
            float soldTotal = 0f;
            for (int n = 0; n < customers; n++)
            {
                Retail.Customer c = null;
                for (int attempt = 0; attempt < 3 && (c == null || c.State != Retail.Customer.Phase.AtCounter); attempt++)
                {
                    c = shop.SpawnNow();
                    float w = 0f;
                    while (c != null && c.State != Retail.Customer.Phase.AtCounter && c.State != Retail.Customer.Phase.Done && w < 70f) { w += Time.deltaTime; yield return null; }
                    if (c != null && c.State == Retail.Customer.Phase.AtCounter && c.Wanted != null) break;
                    while (c != null && c.State != Retail.Customer.Phase.Done) yield return null;
                }
                if (c == null || c.State != Retail.Customer.Phase.AtCounter || c.Wanted == null)
                {
                    L($"  customer {n + 1}: never reached the counter (state={(c == null ? "gone" : c.State.ToString())} atCounter={(shop.AtCounter != null ? shop.AtCounter.Archetype.Name : "none")} queue={shop.QueueLength} inShop={shop.Customers.Count} forSale={S.State.ForSaleCount()})");
                    continue;
                }
                float price = c.Wanted.Record.AskingPrice, before = S.State.Cash;
                string what = c.Wanted.Record.DisplayName, id = c.Wanted.Id;
                var drawerBefore = station.Drawer.Copy();
                station.Enter();
                yield return station.CompleteFromHere(0.2f);
                yield return new WaitForSeconds(0.8f);
                float delta = S.State.Cash - before;
                float tillDelta = station.Drawer.Total - drawerBefore.Total;
                bool cashSale = c != null && c.Method == Retail.Customer.Payment.Cash;
                bool ok = Mathf.Abs(delta - price) < 0.011f && S.State.FindSpecimen(id) != null && S.State.FindSpecimen(id).Location == SpecimenLocation.Sold;
                bool tillOk = !cashSale || Mathf.Abs(tillDelta - price) < 0.011f;
                if (ok) { served++; soldTotal += price; }
                L($"  customer {n + 1}: {what} for {price:F2} by {(cashSale ? "cash" : "card")} -> cash +{delta:F2} till +{tillDelta:F2} {Chk(ok)} till {Chk(tillOk)} station idle={Chk(station.Tx == null)}");
                Snap($"repeat_{n + 1}");
                float leave = 0f;
                while (c != null && c.State != Retail.Customer.Phase.Done && leave < 25f) { leave += Time.deltaTime; yield return null; }
            }
            L($"repeat end: served={served}/{customers} cash {cash0:F2} -> {S.State.Cash:F2} (+{S.State.Cash - cash0:F2}, sold {soldTotal:F2}) {Chk(Mathf.Abs(S.State.Cash - cash0 - soldTotal) < 0.02f)}");
            L($"  till {(drawer0 != null ? drawer0.Total : 0f):F2} -> {station.Drawer.Total:F2} pieces={station.Drawer.Pieces}");
            int leftovers = 0;
            foreach (var go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (go.name == "Card" || go.name == "Bag" || go.name.StartsWith("Tender_") || go.name.StartsWith("Change_")) leftovers++;
            L($"  leftover checkout objects={leftovers} {Chk(leftovers == 0)}");
            Phase = "done";
            Running = false;
        }

        /// <summary>A whole sale worked with nothing but the interact button and the target cycle: the controller path.</summary>
        public void RunStationButtons(string method = "cash", string size = "") { if (!Running) StartCoroutine(StationButtons(method, size)); }

        private IEnumerator StationButtons(string method, string size)
        {
            Running = true;
            Phase = "station-buttons";
            foreach (var st in Workshop.Tutorial.Steps) Workshop.Tutorial.Notify(st.DoneBy);
            yield return EnsureShop();
            var shop = Retail.RetailShop.Instance;
            var station = Find<GeodeEmpire.Checkout.CheckoutStation>();
            if (shop == null || station == null) { L("no shop/station"); Running = false; yield break; }
            Retail.Customer.ForcedMethod = method == "card" ? 1 : 0;
            if (string.IsNullOrEmpty(size)) yield return StockDirect(4); else yield return StockSized(size, 2);
            Retail.Customer c = null;
            for (int attempt = 0; attempt < 4 && (c == null || c.State != Retail.Customer.Phase.AtCounter); attempt++)
            {
                c = shop.SpawnNow();
                float w = 0f;
                while (c != null && c.State != Retail.Customer.Phase.AtCounter && c.State != Retail.Customer.Phase.Done && w < 70f) { w += Time.deltaTime; yield return null; }
                if (c != null && c.State == Retail.Customer.Phase.AtCounter && c.Wanted != null) break;
                while (c != null && c.State != Retail.Customer.Phase.Done) yield return null;
            }
            if (c == null || c.State != Retail.Customer.Phase.AtCounter || c.Wanted == null) { L("no customer at the counter"); Retail.Customer.ForcedMethod = -1; Running = false; yield break; }
            float price = c.Wanted.Record.AskingPrice, cashBefore = S.State.Cash;
            L($"buttons: {c.Archetype.Name} pays by {c.Method} for {c.Wanted.Record.DisplayName} {price:F2}");
            var stand = station.StaffStandPoint;
            yield return RouteTo(new Vector3(stand.position.x, 0f, stand.position.z), 0.35f);
            station.Enter();
            int presses = 0, cycles = 0;
            float guard = 0f;
            while (station.Tx != null && station.State != GeodeEmpire.Checkout.CheckoutState.TransactionComplete && guard < 90f)
            {
                yield return new WaitForSeconds(0.2f); guard += 0.2f;
                if (station.Busy) continue;
                var tx = station.Tx;
                if (tx == null) break;
                if (tx.Stage == GeodeEmpire.Checkout.TxStage.CardEntry)
                {
                    // the keypad: cycle to the digit's key and press it, exactly as a controller would
                    string want = GeodeEmpire.Checkout.Money.Cents(tx.Total).ToString();
                    string action = tx.CardEntryDigits.Length < want.Length ? "digit:" + want[tx.CardEntryDigits.Length] : "confirm";
                    int spins = 0;
                    while (spins++ < 40 && (station.Hovered == null || station.Hovered.Kind != GeodeEmpire.Checkout.CheckoutTargetKind.TerminalKey || station.Hovered.Payload != action))
                        station.CycleTarget(1);
                    if (station.PressInteract()) presses++;
                    cycles += spins;
                    continue;
                }
                if (tx.Stage == GeodeEmpire.Checkout.TxStage.CashDrawer && tx.Deposited)
                {
                    int remaining = GeodeEmpire.Checkout.Money.Cents(tx.ChangeDue) - GeodeEmpire.Checkout.Money.Cents(tx.HandTotal);
                    if (remaining <= 0) { station.ConfirmChangeFromInput(); presses++; continue; }
                    var plan = GeodeEmpire.Checkout.Money.MakeChangeFrom(tx.DrawerContents(station.Drawer), GeodeEmpire.Checkout.Money.Dollars(remaining));
                    if (plan == null) { station.ConfirmChangeFromInput(); presses++; continue; }
                    float denom = 0f;
                    for (int i = 0; i < GeodeEmpire.Checkout.Money.Denoms.Length; i++) if (plan[i] > 0) { denom = GeodeEmpire.Checkout.Money.Denoms[i]; break; }
                    int spins = 0;
                    while (spins++ < 40 && (station.Hovered == null || station.Hovered.Kind != GeodeEmpire.Checkout.CheckoutTargetKind.DrawerWell || Mathf.Abs(station.Hovered.Denom - denom) > 0.001f))
                        station.CycleTarget(1);
                    if (station.PressInteract()) presses++;
                    cycles += spins;
                    continue;
                }
                if (station.PressInteract()) presses++;
            }
            yield return new WaitForSeconds(0.8f);
            var rec = S.State.FindSpecimen(c != null ? "" : "");
            L($"buttons done: presses={presses} cycles={cycles} cash {cashBefore:F2} -> {S.State.Cash:F2} {Chk(Mathf.Abs(S.State.Cash - cashBefore - price) < 0.011f)} state={station.State} idle={Chk(station.Tx == null)}");
            Retail.Customer.ForcedMethod = -1;
            Phase = "done";
            Running = false;
        }

        private static Transform FindDeep(Transform t, string name) { foreach (Transform ch in t) { if (ch.name == name) return ch; var d = FindDeep(ch, name); if (d != null) return d; } return null; }

        /// <summary>Harness: n appraised pieces of one size class on the sale fixtures (seeds searched for the class), so a checkout round can exercise the bag, the box and the two-handed lift.</summary>
        private IEnumerator StockSized(string size, int n)
        {
            yield return EnsureShop();
            var shop = Retail.RetailShop.Instance;
            var want = (SizeClass)System.Enum.Parse(typeof(SizeClass), size, true);
            int placed = 0;
            for (ulong seed = 0x5100UL; seed < 0x5100UL + 6000UL && placed < n; seed += 7UL)
            {
                if (SpecimenGenerator.Generate(seed).SizeClass != want) continue;
                PlacementZone free = null;
                foreach (var z in shop.SaleSlots) if (z.gameObject.activeInHierarchy && !z.Locked && z.IsEmpty) { free = z; break; }
                if (free == null) break;
                var r = S.CreateSpecimenRecord(seed, "test", "");
                r.Location = SpecimenLocation.World; r.Condition.Opened = true; r.Appraised = true;
                r.AppraisedValue = Valuation.DamagedValue(r.Geology, 0f, 0f);
                var e = S.Spawn(r, new Vector3(-1.5f, 0.12f, -0.55f), Quaternion.identity, false);
                yield return null;
                free.Place(e);
                placed++;
            }
            L($"  stocked {placed} {want} -> forSale={S.State.ForSaleCount()}");
        }

        /// <summary>Walk behind the counter and ring up the customer standing at it, then run the steps.</summary>
        private IEnumerator ServeCounter(Retail.Customer c)
        {
            var station = Find<GeodeEmpire.Checkout.CheckoutStation>();
            if (station == null || c == null || c.Wanted == null) yield break;
            string what = c.Wanted.Record.DisplayName; float price = c.Wanted.Record.AskingPrice; float cashBefore = S.State.Cash;
            yield return RouteTo(new Vector3(station.StaffStandPoint.position.x, 0f, station.StaffStandPoint.position.z), 0.35f);
            station.Enter();
            yield return station.CompleteFromHere(0.2f);
            yield return new WaitForSeconds(0.3f);
            if (S.State.Cash > cashBefore) L($"  served {c.Archetype.Name}: {what} for {price}: cash {cashBefore} -> {S.State.Cash}");
            else L($"  SALE FAILED for {c.Archetype.Name}: {what} for {price} (cash unchanged at {S.State.Cash}, prompt='{P.Prompt}')");
        }

        private PlacementZone FreeSaleSlot()
        {
            var shop = Retail.RetailShop.Instance;
            if (shop == null) return null;
            foreach (var z in shop.SaleSlots) if (z.IsEmpty && !z.Locked) return z;
            return null;
        }

        /// <summary>Carry the held piece to the showroom and put it on the first free sale fixture, through the real prompt.</summary>
        private IEnumerator StockHeld()
        {
            yield return EnsureShop();
            var shop = Retail.RetailShop.Instance;
            var e = P.Held;
            var free = FreeSaleSlot();
            if (shop == null || e == null || free == null) { L("nothing to stock"); yield break; }
            // stand in front of the fixture: its browse point is exactly that spot
            var stand = shop.BrowsePointFor(free) != null ? shop.BrowsePointFor(free).position : StandNear(free.transform.position, 0.9f);
            stand.y = 0f;
            yield return RouteTo(stand, 0.3f);
            yield return LookAndInteract(free.transform.position, "Put up for sale");
            if (P.Held == e)
            {
                // a customer put something back on that slot while we walked: take the next free one
                var other = FreeSaleSlot();
                if (other != null && other != free)
                {
                    var stand2 = shop.BrowsePointFor(other) != null ? shop.BrowsePointFor(other).position : StandNear(other.transform.position, 0.9f);
                    stand2.y = 0f;
                    yield return RouteTo(stand2, 0.3f);
                    yield return LookAndInteract(other.transform.position, "Put up for sale");
                    free = other;
                }
            }
            if (P.Held == e)
            {
                L($"could not stock {e.Record.DisplayName}: outbox instead");
                Vector3 tray = ZonePos(ZoneKind.SellTray);
                yield return RouteTo(StandNear(tray), 0.3f);
                yield return LookAndInteract(tray, "Place in the dealer outbox");
                yield break;
            }
            L($"stocked {e.Record.DisplayName} asking={e.Record.AskingPrice} value={e.Record.EstimatedValue()} slot={free.SlotIndex} loc={e.Record.Location}");
        }

        /// <summary>Put the held piece in the first free cabinet slot (the collector's decision).</summary>
        private IEnumerator KeepHeld()
        {
            if (P.Held == null) yield break;
            var e = P.Held;
            var free = FreeDisplaySlot();
            if (free == null) { L("no free display slot"); yield break; }
            Vector3 slot = free.transform.position;
            yield return RouteTo(StandNear(slot, 1.0f), 0.3f);
            yield return LookAndInteract(slot, "Place in display slot");
            yield return new WaitForSeconds(0.4f);
            L($"kept {e.Record.DisplayName}: displayed={S.State.DisplayedCount()} collectionValue={S.State.CollectionValue()} prestige={S.State.Prestige}");
            if (_snap > 0) Snap("cabinet");
        }

        /// <summary>Weigh whatever is in hand on the appraisal scale and take it back.</summary>
        private IEnumerator AppraiseHeld()
        {
            if (P.Held == null) yield break;
            Vector3 scale = ZonePos(ZoneKind.Scale);
            yield return RouteTo(StandNear(scale), 0.3f);
            yield return LookAndInteract(scale, "Weigh on the scale");
            yield return new WaitForSeconds(1.6f);
            var ap = Find<AppraisalStation>();
            L($"  appraised {(ap.Current != null ? ap.Current.Record.DisplayName : "?")} value={(ap.Current != null ? ap.Current.Record.AppraisedValue : 0f)}");
            yield return LookAndInteract(scale, "Take");
        }

        /// <summary>Press the dealer intercom: sells the outbox.</summary>
        private IEnumerator SellOutbox()
        {
            var outbox = Find<SellOutbox>();
            var intercom = Find<DealerIntercom>();
            if (outbox == null || intercom == null || outbox.Count == 0) yield break;
            float before = S.State.Cash;
            yield return RouteTo(StandNear(new Vector3(intercom.transform.position.x, 0f, intercom.transform.position.z), 1.7f), 0.3f);
            yield return LookAndInteract(intercom.transform.position, "Call dealer");
            yield return new WaitForSeconds(0.6f);
            yield return DismissLetters();
            L($"dealer: {outbox.Count} left, cash {before} -> {S.State.Cash}");
        }

        /// <summary>Empty crates are broken down like a player would, freeing the pallet.</summary>
        private IEnumerator BreakDownEmptyCrates()
        {
            foreach (var c in new List<CrateEntity>(S.Crates.Values))
            {
                if (c == null || !c.IsOpened || c.RemainingRocks > 0) continue;
                Vector3 p = c.transform.position;
                Vector3 stand = p + (new Vector3(-0.3f, 0f, 0.6f)).normalized * 1.1f; stand.y = 0f;
                yield return RouteTo(stand, 0.3f);
                yield return LookAndInteract(p + Vector3.up * 0.2f, "Break down");
                yield return new WaitForSeconds(0.5f);
                L("broke down " + c.Record.Id + " crates=" + S.Crates.Count);
            }
        }

        public void RunCrackAll(string style = "careful") { if (!Running) StartCoroutine(CrackAll(style)); }

        public void RunFreshPlayer(string style = "careful") { if (!Running) StartCoroutine(FreshPlayer(style)); }

        /// <summary>
        /// The intended first session, end to end, through the real prompts: first crate (buy, open, crack, appraise,
        /// dealer), the rest of the crate, a keeper in the cabinet, a second crate, the showroom with customers, and
        /// the menus on the pad. Frames are captured at each milestone; the log carries the pacing.
        /// </summary>
        private IEnumerator FreshPlayer(string style)
        {
            Running = true;
            _snap = 0; _stockedSnap = false;
            float t0 = Time.time;
            L($"== FreshPlayer ({style}) cash={S.State.Cash} specimens={S.State.Specimens.Count}");
            yield return FirstCrate(style);
            Running = true;
            L($"-- first crate done at {Time.time - t0:F0}s");
            yield return CrackAllCore(style);
            Running = true;
            yield return SellOutbox();
            L($"-- crate cracked out at {Time.time - t0:F0}s cash={S.State.Cash} forSale={S.State.ForSaleCount()}");
            if (S.State.DisplayedCount() == 0) yield return DisplayKeepCore();
            Running = true;
            L($"-- keeper displayed at {Time.time - t0:F0}s kept={S.State.DisplayedCount()}");
            yield return BreakDownEmptyCrates();
            // second crate: whatever the tablet now offers beyond the local quarry
            string next = S.State.UnlockedSuppliers.Contains("regional") && S.State.Cash >= 150f ? "regional" : "local";
            if (S.BuyCrate(next, out string err)) L($"bought {next} at {Time.time - t0:F0}s cash={S.State.Cash}"); else L($"could not buy {next}: {err}");
            yield return new WaitForSeconds(1.5f);
            yield return FirstCrate(style);
            Running = true;
            yield return CrackAllCore(style);
            Running = true;
            yield return SellOutbox();
            L($"-- second crate cracked out at {Time.time - t0:F0}s cash={S.State.Cash} opened={S.State.Stats.SpecimensOpened} forSale={S.State.ForSaleCount()}");
            yield return RetailCycle(3);
            Running = true;
            L($"-- retail done at {Time.time - t0:F0}s cash={S.State.Cash}");
            yield return ControllerMenus();
            Running = true;
            L($"== FreshPlayer end at {Time.time - t0:F0}s cash={S.State.Cash} opened={S.State.Stats.SpecimensOpened} sold={S.State.Stats.SpecimensSold} kept={S.State.DisplayedCount()} retail={S.State.Stats.RetailSales} families={S.State.Encyclopedia.Count}");
            L(Core.CollisionAudit.Report("fresh player end"));
            Phase = "done";
            Running = false;
        }

        private IEnumerator CrackAll(string style)
        {
            Running = true;
            yield return CrackAllCore(style);
            Phase = "done";
            Running = false;
        }

        /// <summary>Dealer letters (tease, premium invite) block gameplay input until dismissed, exactly as for a player.</summary>
        private UI.SliceDirector _sliceDirector;

        /// <summary>A milestone letter is modal: it swallows movement and prompts until it is dismissed.</summary>
        private IEnumerator DismissLetters()
        {
            if (_sliceDirector == null) _sliceDirector = Find<UI.SliceDirector>();
            var sd = _sliceDirector;
            if (sd == null || !sd.IsOpen) yield break;
            for (int i = 0; i < 3 && sd.IsOpen; i++)
            {
                L("  letter shown: '" + sd.CurrentTitle + "' (dismissing)");
                if (UseGamepad) yield return D.PadTap(GamepadButton.South, 0.1f); else yield return D.Tap(Key.Enter, 0.1f);
                yield return new WaitForSeconds(0.35f);
            }
        }

        /// <summary>
        /// Approach a loose rock from the outside of whatever holds it (crate or floor) so it is the nearest thing under
        /// the crosshair; if that side is blocked, try from the opposite side, the way a player would step around.
        /// </summary>
        private IEnumerator FetchRock(SpecimenEntity rock)
        {
            Vector3 rp = rock.transform.position;
            Vector3 center = rp;
            foreach (var c in S.Crates.Values) if (c.IsOpened && (c.transform.position - rp).sqrMagnitude < 1.2f) { center = c.transform.position; break; }
            Vector3 dir = rp - center; dir.y = 0f;
            if (dir.sqrMagnitude < 0.0004f) dir = new Vector3(-0.2f, 0f, 0.7f);
            dir.Normalize();
            // rocks on the receiving pallets are approached from the room side: the pocket between the pallet deck,
            // the shutter and the north wall wedges a controller that only pushes toward its target
            bool receiving = rp.z > Build.ShopPlan.BackZ + 0.4f && rp.x > Build.ShopPlan.BayX0 - 0.6f && rp.x < Build.ShopPlan.BayX1 + 0.6f;
            for (int attempt = 0; attempt < 2 && P.Held == null; attempt++)
            {
                Vector3 stand = rp + dir * 0.8f; stand.y = 0f;
                if (receiving) stand = new Vector3(rp.x + (attempt == 0 ? 0f : (rp.x < (Build.ShopPlan.BayX0 + Build.ShopPlan.BayX1) * 0.5f ? 0.7f : -0.7f)), 0f, rp.z - 0.95f);
                // the room, not the V5 garage: M1 moved the west wall to -6.4 and added the back of house north of
                // z 3.2, and these clamps still pinned the walker at x -3.1 / z 2.25, three metres from the bay
                stand.x = Mathf.Clamp(stand.x, Build.ShopPlan.XMin + 0.45f, Build.ShopPlan.PartitionX - 0.45f);
                stand.z = Mathf.Clamp(stand.z, Build.ShopPlan.ZMin + 0.45f, Build.ShopPlan.ZMax - 0.45f);
                yield return RouteTo(stand, 0.25f);
                // aim at the collider's centre, not the pivot: a sawn half's pivot sits above its hull, and a ray at
                // the pivot from a steep angle can graze past a 13 cm piece
                var col = rock != null ? rock.GetComponentInChildren<Collider>() : null;
                yield return LookAndInteract(col != null ? col.bounds.center : rp, "Pick up");
                dir = -dir;
            }
        }

        private IEnumerator SellCore()
        {
            Phase = "sell";
            var outbox = Find<SellOutbox>();
            if (outbox.Count == 0) { L("outbox empty, nothing to sell"); yield break; }
            var intercom = Find<DealerIntercom>();
            yield return Walk(StandNear(new Vector3(intercom.transform.position.x, 0f, intercom.transform.position.z), 1.7f), 0.3f);
            yield return LookAndInteract(intercom.transform.position, "Call dealer");
            yield return new WaitForSeconds(0.6f);
            L("sold: cash=" + S.State.Cash + " outbox=" + outbox.Count);
        }

        /// <summary>Buy a crate from a supplier, open it, crack everything, optionally keep the best piece, sell the rest.</summary>
        public void RunSupplierCycle(string supplier, string style = "careful", bool keepBest = false) { if (!Running) StartCoroutine(SupplierCycle(supplier, style, keepBest)); }

        private IEnumerator SupplierCycle(string supplier, string style, bool keepBest)
        {
            Running = true;
            Phase = "buy " + supplier;
            L($"== SupplierCycle ({supplier}, {style}, keep={keepBest}) cash={S.State.Cash}");
            yield return DismissLetters();
            if (Find<SellOutbox>().Count > 0) yield return SellCore();   // cash in whatever the last crate left in the tray
            var before = new HashSet<string>(S.Crates.Keys);
            if (!S.BuyCrate(supplier, out string err)) { L("buy failed: " + err + " cash=" + S.State.Cash); Phase = "done"; Running = false; yield break; }
            L("buy: ok cash=" + S.State.Cash);
            yield return new WaitForSeconds(2.0f);
            CrateEntity crate = null;
            foreach (var kv in S.Crates) if (!before.Contains(kv.Key)) crate = kv.Value;
            if (crate == null) { L("no new crate"); Phase = "done"; Running = false; yield break; }
            Phase = "open-crate";
            Vector3 cratePos = crate.transform.position;
            Vector3 stand = cratePos + (new Vector3(-0.3f, 0f, 0.6f)).normalized * 1.1f; stand.y = 0f;
            yield return Walk(stand, 0.3f);
            yield return LookAndInteract(cratePos + Vector3.up * 0.25f, "Open crate");
            yield return new WaitForSeconds(1.0f);
            L("crate opened=" + crate.IsOpened + " remaining=" + crate.RemainingRocks);
            if (!crate.IsOpened) { Phase = "done"; Running = false; yield break; }
            yield return CrackAllCore(style);
            if (keepBest) yield return DisplayKeepCore();
            yield return SellCore();
            yield return DismissLetters();
            var st = S.State;
            L($"cycle end: cash={st.Cash} sold={st.Stats.SpecimensSold} opened={st.Stats.SpecimensOpened} crates={st.Stats.CratesPurchased} displayed={st.DisplayedCount()} suppliers={string.Join(",", st.UnlockedSuppliers)} tease={st.SliceTeaseShown} premiumInvite={st.PremiumInviteShown}");
            Phase = "done";
            Running = false;
        }

        private IEnumerator CrackAllCore(string style)
        {
            var bench = Find<CrackingBench>();
            Vector3 cradle = ZonePos(ZoneKind.Cradle);
            Vector3 benchStand = new Vector3(cradle.x, 0f, cradle.z - 0.95f);
            int processed = 0;
            while (processed < 12)
            {
                SpecimenEntity rock = null;
                foreach (var e in S.Entities.Values) if (!e.IsOpened && (e.Record.Location == SpecimenLocation.InCrate || e.Record.Location == SpecimenLocation.World)) { rock = e; break; }
                if (rock == null) break;
                Phase = "fetch " + rock.Id;
                yield return DismissLetters();
                yield return FetchRock(rock);
                if (P.Held == null) { L("could not pick " + rock.Id); break; }
                if (P.Held != rock) L($"  aimed at {rock.Id} but picked up {P.Held.Id} (rocks too close together)");
                rock = P.Held;
                yield return Walk(benchStand, 0.25f);
                yield return LookAndInteract(cradle, "Set on the cradle");
                yield return new WaitForSeconds(1.0f);
                if (!bench.Active) { L("bench did not activate"); break; }
                int strikes = 0;
                float hold = style == "careless" ? 1.0f : 0.5f;
                while (bench.Active && !bench.Opened && strikes < 60)
                {
                    yield return AimCursor(bench, bench.SeamCursorHint() + new Vector2(Random.Range(-0.02f, 0.02f), Random.Range(-0.02f, 0.02f)));
                    yield return Strike(hold);
                    strikes++;
                    if (style != "careless" || strikes % 2 == 0) yield return Rotate(0.42f, 1);
                    while (bench.Revealing) yield return null;
                }
                if (!bench.Opened) L($"  loop ended early: active={bench.Active} revealing={bench.Revealing} strikes={strikes} exitReason={bench.LastExitReason.Split('\n')[0]}");
                yield return new WaitForSeconds(1.8f);
                var g = rock.Geology;
                L($"{rock.Id} {g.Mineral} {g.Tier} {g.MassKg:F1}kg strikes={strikes} dmgEvents={bench.DamageEventsThisRock} note='{bench.ResultNote}' base=${g.BaseValue} dmgFrac={rock.Visual.CrystalDamageFraction():F2}");
                yield return Interact();
                yield return new WaitForSeconds(0.3f);
                yield return DismissLetters();
                if (P.Held != null && g.Tier >= QualityTier.Good && S.State.DisplayedCount() == 0)
                {
                    // the first rare piece is the one a player keeps
                    yield return AppraiseHeld();
                    if (P.Held != null) yield return KeepHeld();
                }
                else if (P.Held != null && g.Tier >= QualityTier.Decent && FreeSaleSlot() != null)
                {
                    // promising: weigh it, then put it in the showroom window
                    yield return AppraiseHeld();
                    if (P.Held != null) yield return StockHeld();
                    if (_snap > 0 && !_stockedSnap) { _stockedSnap = true; Snap("shelf_stocked"); }
                }
                else
                {
                    // ordinary: the dealer outbox
                    Vector3 tray = ZonePos(ZoneKind.SellTray);
                    yield return RouteTo(StandNear(tray), 0.3f);
                    yield return LookAndInteract(tray, "Place in the dealer outbox");
                }
                processed++;
            }
            L("crackall processed=" + processed);
        }
    }
}
