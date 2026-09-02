using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
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

        private DevDriver D => DevDriver.Get();
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

        private static Vector3 ZonePos(ZoneKind kind, int index = 0)
        {
            foreach (var z in FindObjectsByType<PlacementZone>(FindObjectsInactive.Exclude))
                if (z.Kind == kind && (kind != ZoneKind.DisplaySlot || z.SlotIndex == index)) return z.transform.position;
            return Vector3.zero;
        }

        private static T Find<T>() where T : Object => FindAnyObjectByType<T>();

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
                // aim at the seam facing the camera (what a player learns to do), with a little error
                var hint = bench.SeamCursorHint();
                bench.SetCursor(hint + new Vector2(Random.Range(-0.02f, 0.02f), Random.Range(-0.02f, 0.02f)));
                yield return null;
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

        /// <summary>Crack every remaining rock on the bench quickly (for economy/pacing checks).</summary>
        public void RunCrackAll(string style = "careful") { if (!Running) StartCoroutine(CrackAll(style)); }

        private IEnumerator CrackAll(string style)
        {
            Running = true;
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
                Vector3 rp = rock.transform.position;
                Vector3 stand = rp + (new Vector3(-0.2f, 0f, 0.7f)).normalized * 1.0f; stand.y = 0f;
                yield return D.WalkTo(stand, 0.3f);
                yield return LookAndInteract(rp, "Pick up");
                if (P.Held == null) { L("could not pick " + rock.Id); break; }
                yield return D.WalkTo(benchStand, 0.25f);
                yield return LookAndInteract(cradle, "Set on the cradle");
                yield return new WaitForSeconds(1.0f);
                if (!bench.Active) { L("bench did not activate"); break; }
                int strikes = 0;
                float hold = style == "careless" ? 1.0f : 0.5f;
                while (bench.Active && !bench.Opened && strikes < 60)
                {
                    bench.SetCursor(bench.SeamCursorHint() + new Vector2(Random.Range(-0.02f, 0.02f), Random.Range(-0.02f, 0.02f)));
                    yield return null;
                    yield return Strike(hold);
                    strikes++;
                    if (style != "careless" || strikes % 2 == 0) yield return Rotate(0.42f, 1);
                    while (bench.Revealing) yield return null;
                }
                yield return new WaitForSeconds(1.8f);
                var g = rock.Geology;
                L($"{rock.Id} {g.Mineral} {g.Tier} strikes={strikes} dmgEvents={bench.DamageEventsThisRock} note='{bench.ResultNote}' base=${g.BaseValue} dmgFrac={rock.Visual.CrystalDamageFraction():F2}");
                yield return Interact();
                yield return new WaitForSeconds(0.3f);
                // drop it in the outbox
                Vector3 tray = ZonePos(ZoneKind.SellTray);
                yield return D.WalkTo(StandNear(tray), 0.3f);
                yield return LookAndInteract(tray, "Place in the dealer outbox");
                processed++;
            }
            Phase = "done";
            L("crackall processed=" + processed);
            Running = false;
        }
    }
}
