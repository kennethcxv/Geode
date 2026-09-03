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
            Instance = go.AddComponent<Playtest>();
            return Instance;
        }

        private DevDriver D { get { var d = DevDriver.Get(); d.UseGamepad = UseGamepad; return d; } }
        private GameSession S => GameSession.Instance;
        private PlayerInteractor P => D.Player;

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
            foreach (var r in s.State.Specimens) if (r.Location != SpecimenLocation.Sold && r.Location != SpecimenLocation.Discarded) expectedEntities++;
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
        private const float PartitionX = 2.55f;
        private static readonly Vector3 OpeningWest = new Vector3(2.2f, 0f, 1.8f), OpeningEast = new Vector3(2.95f, 0f, 1.8f);

        /// <summary>Walk to a point, going round through the partition opening when the target is in the other room.</summary>
        private IEnumerator RouteTo(Vector3 target, float tolerance = 0.3f)
        {
            var c = D.Controller;
            bool hereEast = c.transform.position.x > PartitionX, thereEast = target.x > PartitionX;
            if (hereEast != thereEast)
            {
                yield return D.WalkTo(hereEast ? OpeningEast : OpeningWest, 0.3f, 14f);
                yield return D.WalkTo(hereEast ? OpeningWest : OpeningEast, 0.3f, 8f);
            }
            yield return D.WalkTo(target, tolerance, 14f);
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
            foreach (var c in S.Crates.Values) { crate = c; break; }
            if (crate == null) { L("no crate"); Running = false; yield break; }
            Vector3 cratePos = crate.transform.position;
            Vector3 stand = cratePos + (new Vector3(-0.3f, 0f, 0.6f)).normalized * 1.1f;
            stand.y = 0f;
            yield return D.WalkTo(stand, 0.3f);
            if (!crate.IsOpened) yield return LookAndInteract(cratePos + Vector3.up * 0.2f, "Open crate");
            yield return new WaitForSeconds(0.9f);
            L("crate opened=" + crate.IsOpened + " remaining=" + crate.RemainingRocks);

            Phase = "pick-rock";
            SpecimenEntity rock = null;
            foreach (var e in S.Entities.Values) if (!e.IsOpened && e.Record.Location == SpecimenLocation.InCrate) { rock = e; break; }
            if (rock == null) { L("no rock"); Running = false; yield break; }
            yield return LookAndInteract(rock.transform.position, "Pick up rock");
            L("held=" + (P.Held != null ? P.Held.Id : "none"));
            if (P.Held == null) { Running = false; yield break; }

            Phase = "to-bench";
            Vector3 cradle = ZonePos(ZoneKind.Cradle);
            Vector3 benchStand = new Vector3(cradle.x, 0f, cradle.z - 0.95f);
            yield return D.WalkTo(benchStand, 0.25f);
            yield return LookAndInteract(cradle, "Set on the cradle");
            yield return new WaitForSeconds(1.2f);
            var bench = Find<CrackingBench>();
            L("bench active=" + bench.Active + " rock=" + (bench.Rock != null ? bench.Rock.Id : "none"));
            if (!bench.Active) { Running = false; yield break; }

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
                if (style != "careless" || strikes % 2 == 0) yield return Rotate(0.42f, 1);   // work around the ring
                while (bench.Revealing) yield return null;
            }
            yield return new WaitForSeconds(2.2f);
            L($"opened={bench.Opened} strikes={strikes} note='{bench.ResultNote}' damageEvents={bench.DamageEventsThisRock}");
            if (!bench.Opened) { Running = false; yield break; }

            Phase = "take";
            yield return Interact();
            yield return new WaitForSeconds(0.3f);
            L("held after take=" + (P.Held != null ? P.Held.Record.DisplayName : "none") + " value=" + (P.Held != null ? P.Held.Geology.BaseValue.ToString() : ""));

            Phase = "appraise";
            Vector3 scale = ZonePos(ZoneKind.Scale);
            yield return D.WalkTo(StandNear(scale), 0.3f);
            yield return LookAndInteract(scale, "Weigh on the scale");
            yield return new WaitForSeconds(1.6f);
            var ap = Find<AppraisalStation>();
            L("appraised=" + (ap.Current != null && ap.Current.Record.Appraised) + " value=" + (ap.Current != null ? ap.Current.Record.AppraisedValue.ToString() : ""));
            yield return LookAndInteract(scale, "Take");
            L("held=" + (P.Held != null ? P.Held.Record.DisplayName : "none"));

            Phase = "sell";
            Vector3 tray = ZonePos(ZoneKind.SellTray);
            yield return D.WalkTo(StandNear(tray), 0.3f);
            yield return LookAndInteract(tray, "Place in the dealer outbox");
            var outbox = Find<SellOutbox>();
            L("outbox count=" + outbox.Count + " est=" + outbox.EstimateTotal());
            var intercom = Find<DealerIntercom>();
            yield return D.WalkTo(StandNear(new Vector3(intercom.transform.position.x, 0f, intercom.transform.position.z), 1.7f), 0.3f);
            yield return LookAndInteract(intercom.transform.position, "Call dealer");
            yield return new WaitForSeconds(0.5f);
            L($"cash={S.State.Cash} sold={S.State.Stats.SpecimensSold} suppliers={string.Join(",", S.State.UnlockedSuppliers)}");
            Phase = "done";
            Running = false;
        }

        /// <summary>Run C step: take the nicest opened specimen we can find to display slot 0 and verify it stuck.</summary>
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
                if (e.IsOpened && e.Record.Location != SpecimenLocation.DisplaySlot && (best == null || e.Geology.BaseValue > best.Geology.BaseValue)) best = e;
            if (best == null) { L("no opened specimen to display"); Running = false; yield break; }
            Vector3 bp = best.transform.position;
            yield return D.WalkTo(StandNear(bp, 0.9f), 0.3f);
            yield return LookAndInteract(bp, "Take");
            if (P.Held == null) { yield return LookAndInteract(bp, "Pick up"); }
            L("held=" + (P.Held != null ? P.Held.Record.DisplayName : "none"));
            if (P.Held == null) { Running = false; yield break; }
            Vector3 slot = ZonePos(ZoneKind.DisplaySlot, 0);
            yield return D.WalkTo(StandNear(slot, 1.0f), 0.3f);
            yield return LookAndInteract(slot, "Place in display slot");
            yield return new WaitForSeconds(0.4f);
            var st = S.State;
            L($"displayed={st.DisplayedCount()} collectionValue={st.CollectionValue()} prestige={st.Prestige} suppliers={string.Join(",", st.UnlockedSuppliers)} kept={st.Stats.SpecimensKept}");
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
                var pos = new Vector3(-0.2f + (i % 3) * 0.45f, 0.12f, -1.2f + (i / 3) * 0.5f);
                S.Spawn(r, pos, Quaternion.identity, true);
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
            var shop = Retail.RetailShop.Instance;
            if (shop == null) { L("no shop"); Running = false; yield break; }
            // 1. stock three slots directly (the walking path is covered by RetailCycle)
            var stock = new List<SpecimenEntity>();
            foreach (var e in s.Entities.Values) if (e.IsOpened && e.Record.Appraised && e.Record.Location == SpecimenLocation.World) stock.Add(e);
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
            L($"  prices set: {Chk(placed > 0 && prices.Values.All(p => p > 0f))}");
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
            var reg = FindAnyObjectByType<Retail.CheckoutRegister>();
            c = shop.SpawnNow();
            t = 0f;
            while (c != null && c.State != Retail.Customer.Phase.AtCounter && c.State != Retail.Customer.Phase.Leaving && c.State != Retail.Customer.Phase.Done && t < 90f) { t += Time.deltaTime; yield return null; }
            if (c != null && c.State == Retail.Customer.Phase.AtCounter && c.Wanted != null)
            {
                string soldId = c.Wanted.Id;
                float price = c.Wanted.Record.AskingPrice;
                float cashBefore = s.State.Cash;
                int salesBefore = s.State.Stats.RetailSales;
                reg.Interact(P);
                yield return null;
                reg.Interact(P);
                yield return null;
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
                PlacementZone free = null;
                foreach (var z in shop.SaleSlots) if (z.IsEmpty && !z.Locked) { free = z; break; }
                if (free == null) break;
                yield return FetchRock(e);
                if (P.Held != e) { L("could not fetch " + e.Id); continue; }
                // stand in front of the fixture: its browse point is exactly that spot
                var stand = shop.BrowsePointFor(free) != null ? shop.BrowsePointFor(free).position : StandNear(free.transform.position, 0.9f);
                stand.y = 0f;
                yield return RouteTo(stand, 0.3f);
                yield return LookAndInteract(free.transform.position, "Put up for sale");
                L($"stocked {e.Record.DisplayName} asking={e.Record.AskingPrice} value={e.Record.EstimatedValue()} slot={free.SlotIndex} loc={e.Record.Location}");
                placed++;
                if (placed >= 4) break;
            }
            L(Core.CollisionAudit.Report("stocked shelves"));
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
                while (c != null && c.State != Retail.Customer.Phase.AtCounter && c.State != Retail.Customer.Phase.Leaving && c.State != Retail.Customer.Phase.Done && t < 90f) { t += Time.deltaTime; yield return null; }
                if (c == null || c.State != Retail.Customer.Phase.AtCounter)
                {
                    L($"  left without buying (state={(c != null ? c.State.ToString() : "gone")}) after {t:F0}s");
                    while (c != null && c.State != Retail.Customer.Phase.Done) yield return null;
                    continue;
                }
                L($"  at counter after {t:F0}s wanting {c.Wanted.Record.DisplayName} for {c.Wanted.Record.AskingPrice}");
                L(Core.CollisionAudit.Report("customer at counter"));
                // walk to the register on the workshop side and ring it up twice
                var reg = Find<Retail.CheckoutRegister>();
                yield return RouteTo(new Vector3(reg.transform.position.x - 0.85f, 0f, reg.transform.position.z + 0.15f), 0.3f);
                float cashBefore = S.State.Cash;
                yield return LookAndInteract(reg.transform.position + Vector3.up * 0.12f, "Ring up");
                yield return LookAndInteract(reg.transform.position + Vector3.up * 0.12f, "Take");
                yield return new WaitForSeconds(0.5f);
                L($"  sale: cash {cashBefore} -> {S.State.Cash} retailSales={S.State.Stats.RetailSales} forSale={S.State.ForSaleCount()}");
                while (c != null && c.State != Retail.Customer.Phase.Done) yield return null;
            }
            L($"retail end: cash={S.State.Cash} revenue={S.State.Stats.RetailRevenue} served={S.State.Stats.CustomersServed} leftEmpty={S.State.Stats.CustomersLeftEmptyHanded} customersNow={shop.Customers.Count}");
            Phase = "done";
            Running = false;
        }

        /// <summary>Crack every remaining rock on the bench quickly (for economy/pacing checks).</summary>
        public void RunCrackAll(string style = "careful") { if (!Running) StartCoroutine(CrackAll(style)); }

        private IEnumerator CrackAll(string style)
        {
            Running = true;
            yield return CrackAllCore(style);
            Phase = "done";
            Running = false;
        }

        /// <summary>Dealer letters (tease, premium invite) block gameplay input until dismissed, exactly as for a player.</summary>
        private IEnumerator DismissLetters()
        {
            var sd = Find<UI.SliceDirector>();
            for (int i = 0; i < 3 && sd != null && sd.IsOpen; i++)
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
            for (int attempt = 0; attempt < 2 && P.Held == null; attempt++)
            {
                Vector3 stand = rp + dir * 0.8f; stand.y = 0f;
                stand.x = Mathf.Clamp(stand.x, -3.1f, 6.6f); stand.z = Mathf.Clamp(stand.z, -2.25f, 2.25f);
                yield return RouteTo(stand, 0.25f);
                yield return LookAndInteract(rp, "Pick up");
                dir = -dir;
            }
        }

        private IEnumerator SellCore()
        {
            Phase = "sell";
            var outbox = Find<SellOutbox>();
            if (outbox.Count == 0) { L("outbox empty, nothing to sell"); yield break; }
            var intercom = Find<DealerIntercom>();
            yield return D.WalkTo(StandNear(new Vector3(intercom.transform.position.x, 0f, intercom.transform.position.z), 1.7f), 0.3f);
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
            yield return D.WalkTo(stand, 0.3f);
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
                yield return D.WalkTo(benchStand, 0.25f);
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
                L($"{rock.Id} {g.Mineral} {g.Tier} strikes={strikes} dmgEvents={bench.DamageEventsThisRock} note='{bench.ResultNote}' base=${g.BaseValue} dmgFrac={rock.Visual.CrystalDamageFraction():F2}");
                yield return Interact();
                yield return new WaitForSeconds(0.3f);
                yield return DismissLetters();
                // drop it in the outbox
                Vector3 tray = ZonePos(ZoneKind.SellTray);
                yield return D.WalkTo(StandNear(tray), 0.3f);
                yield return LookAndInteract(tray, "Place in the dealer outbox");
                processed++;
            }
            L("crackall processed=" + processed);
        }
    }
}
