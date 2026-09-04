using UnityEngine;
using UnityEngine.UIElements;
using GeodeEmpire.Core;
using GeodeEmpire.Cracking;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.UI
{
    /// <summary>Bench-view overlay: aim reticle, force meter, crack progress, result card.</summary>
    public sealed class BenchHud : MonoBehaviour
    {
        private CrackingBench _bench;
        private VisualElement _root, _reticle, _panel, _result;
        private Label _title, _cracks, _hint, _resultName, _resultNote, _resultPrompt, _zone, _seat, _tool;
        private string _lastSeat = "", _lastTool = "";
        private int _lastZone = -1;
        private float _flash;
        private int _lastCracks = -1, _lastRingPct = -1;
        private int _lastHintState = -1;
        private ControlScheme _lastScheme;

        private void Start()
        {
            _bench = FindAnyObjectByType<CrackingBench>();
            var hud = HudController.Instance;
            if (_bench == null || hud == null) { enabled = false; return; }
            _root = hud.GetComponent<UIDocument>().rootVisualElement;

            _reticle = UiKit.Box(_root, "crosshair-ring");
            _reticle.style.width = 30; _reticle.style.height = 30; _reticle.style.borderTopLeftRadius = 15; _reticle.style.borderTopRightRadius = 15;
            _reticle.style.borderBottomLeftRadius = 15; _reticle.style.borderBottomRightRadius = 15;
            _reticle.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            _reticle.style.display = DisplayStyle.None;

            _panel = UiKit.Box(_root, "card", "bench-panel");
            _title = UiKit.Label(_panel, "CRACKING BENCH", "bench-title");
            // physical-first (V5 §55): the raised hammer, the whoosh and the seam overlay carry force and progress; the panel
            // only names the force zone and counts the ring
            var forceRow = UiKit.Box(_panel, "row");
            forceRow.style.justifyContent = Justify.SpaceBetween;
            UiKit.Label(forceRow, "Force", "muted");
            _zone = UiKit.Label(forceRow, "", "accent", "medium");
            _cracks = UiKit.Label(_panel, "", "");
            _seat = UiKit.Label(_panel, "", "muted");
            _tool = UiKit.Label(_panel, "", "muted");
            _hint = UiKit.Label(_panel, "", "bench-hint");
            _hint.style.whiteSpace = WhiteSpace.Normal;
            _panel.style.display = DisplayStyle.None;

            _result = UiKit.Box(_root, "card");
            _result.style.position = Position.Absolute;
            _result.style.left = Length.Percent(50); _result.style.top = Length.Percent(80);
            _result.style.translate = new Translate(Length.Percent(-50), 0);
            _result.style.alignItems = Align.Center;
            _resultName = UiKit.Label(_result, "", "appraisal-name", "bold");
            _resultNote = UiKit.Label(_result, "", "appraisal-line", "accent");
            _resultPrompt = UiKit.Label(_result, "", "muted");
            _result.style.display = DisplayStyle.None;

            _bench.Entered += OnEntered;
            _bench.Exited += OnExited;
            _bench.Struck += OnStruck;
            _bench.Revealed += OnRevealed;
        }

        private void OnDestroy()
        {
            if (_bench == null) return;
            _bench.Entered -= OnEntered;
            _bench.Exited -= OnExited;
            _bench.Struck -= OnStruck;
            _bench.Revealed -= OnRevealed;
        }

        private void OnEntered()
        {
            _lastCracks = -1; _lastHintState = -1;
            HudController.Instance.SetFreeRoamVisible(false);
            _panel.style.display = DisplayStyle.Flex;
            _reticle.style.display = DisplayStyle.Flex;
            _result.style.display = DisplayStyle.None;
        }

        private void OnExited()
        {
            HudController.Instance.SetFreeRoamVisible(true);
            _panel.style.display = DisplayStyle.None;
            _reticle.style.display = DisplayStyle.None;
            _result.style.display = DisplayStyle.None;
        }

        private void OnStruck(StressModel.StrikeResult r)
        {
            _flash = 1f;
        }

        private static string ThicknessWord(float t) => t > 1.12f ? "runs thick" : t < 0.9f ? "runs thin" : "is even";

        private void OnRevealed(SpecimenEntity e)
        {
            _reticle.style.display = DisplayStyle.None;
            _panel.style.display = DisplayStyle.None;
            _result.style.display = DisplayStyle.Flex;
            _resultName.text = e.Record.DisplayName;
            _resultNote.text = _bench.ResultNote;
            _resultPrompt.text = $"[{GameInput.Glyph("Interact")}] Take specimen     [{GameInput.Glyph("Back")}] Leave it";
        }

        private void Update()
        {
            if (_bench == null || !_bench.Active) return;
            if (_bench.Opened || _bench.Revealing)
            {
                _reticle.style.display = DisplayStyle.None;
                return;
            }
            var c = _bench.Cursor;
            _reticle.style.left = Length.Percent(c.x * 100f);
            _reticle.style.top = Length.Percent((1f - c.y) * 100f);
            _reticle.style.opacity = _bench.AimValid ? 1f : 0.35f;
            // the reticle tells placement: white on the seam, amber off it
            var rc = _bench.AimValid ? Color.Lerp(new Color(1f, 0.72f, 0.3f), Color.white, _bench.Placement) : new Color(1f, 1f, 1f, 0.5f);
            _reticle.style.borderTopColor = rc; _reticle.style.borderBottomColor = rc; _reticle.style.borderLeftColor = rc; _reticle.style.borderRightColor = rc;
            int zone = _bench.Charge <= 0.02f ? 0 : _bench.Charge < CrackingBench.ForceTap ? 1 : _bench.Charge < CrackingBench.ForceCareful ? 2 : _bench.Charge < CrackingBench.ForceFirm ? 3 : 4;
            if (zone != _lastZone) { _lastZone = zone; _zone.text = zone == 0 ? "" : CrackingBench.ForceZoneName(_bench.Charge).ToUpper(); }
            _flash = Mathf.Max(0f, _flash - Time.deltaTime * 3f);
            bool lamp = _bench.HasLamp;
            string seat = "Seat: " + Workshop.Preparation.SeatWord(_bench.Stability) + (_bench.ClampClosed ? "  •  clamped" : _bench.ClampOwned ? "  •  clamp open" : "") + (_bench.Cleanliness < 0.5f ? "  •  clay hides the seam" : "");
            string tool = _bench.ToolName + (_bench.HasLamp && _bench.AimValid && _bench.Rock != null ? "  •  shell " + ThicknessWord(_bench.Rock.Geology.SectorThicknessAt(_bench.AimSector)) + " here" : "");
            if (tool != _lastTool) { _lastTool = tool; _tool.text = tool; }
            if (seat != _lastSeat) { _lastSeat = seat; _seat.text = seat; _seat.style.color = _bench.Stability < StressModel.UnstableBelow ? new Color(1f, 0.55f, 0.35f) : _bench.Stability < 0.8f ? new Color(1f, 0.85f, 0.5f) : new Color(0.75f, 0.75f, 0.72f); }
            int cracks = _bench.Model.CrackedCount();
            int ringPct = lamp ? Mathf.RoundToInt(_bench.Model.Progress() * 10f) * 10 : -1;
            if (cracks != _lastCracks || ringPct != _lastRingPct)
            {
                _lastCracks = cracks; _lastRingPct = ringPct;
                _cracks.text = (cracks == 0 ? "No cracks yet" : $"{cracks} of {StressModel.Sectors} seam segments cracked") + (lamp && cracks > 0 ? $"  •  ring {ringPct}% worked" : "");
            }
            // the hint only changes with state, never per frame: build the string when the state does
            int state;
            var lr = _bench.LastResult;
            if (lr.Slipped && _flash > 0.5f) state = lr.Wobbled ? 10 : 5;
            else if (lr.Damaged && !lr.Opened && _flash > 0.5f) state = 20 + (int)lr.DamageCause;
            else if (lr.WeakBite && _flash > 0.5f) state = 40 + (int)lr.BiteCause;
            else if (_bench.LastResult.SurfaceChip && _flash > 0.5f) state = 8;
            else if (_bench.LastResult.Lucky && _flash > 0.5f) state = 9;
            else if (_bench.LastResult.Overstrike && _flash > 0.5f) state = 6;
            else if (_bench.LastResult.Wobbled && _flash > 0.5f) state = _bench.Rock != null && _bench.Rock.Geology.SizeClass == SizeClass.Oversized && !_bench.HasHeavyCradle ? 10 : 13;
            else if (!_bench.AimValid) state = 1;
            else if (_bench.Charge >= CrackingBench.ForceFirm) state = 2;
            else if (_bench.Charge > 0.02f) state = 3;
            else if (_bench.Cleanliness < 0.5f) state = 12;
            else if (_bench.ClampOwned && !_bench.ClampClosed) state = 14;
            else if (_bench.ChipSector >= 0 && _bench.AimSector == _bench.ChipSector && !_bench.Model.IsCracked(_bench.ChipSector)) state = 11;
            else if (_bench.Placement < 0.5f) state = 7;
            else state = 4;
            if (state != _lastHintState || ((state == 4 || state == 7) && GameInput.Scheme != _lastScheme))
            {
                _lastHintState = state; _lastScheme = GameInput.Scheme;
                _hint.text = state switch
                {
                    1 => "Aim on the rock",
                    2 => "Heavy blow: fast, but crystals near the seam may break",
                    3 => "Release to strike",
                    5 => "Slipped! Aim squarely at the shell.",
                    6 => "That segment is already cracked. Work around the ring.",
                    7 => $"Off the seam: the shell only splits along its natural ring  •  {GameInput.Glyph("Rotate")} rotate",
                    8 => "The chisel skated and took a flake off. Set it squarely and strike again.",
                    9 => "The crack ran along a weak line: that segment gave more than the blow deserved.",
                    10 => "Too big for this cradle: it rocks under every blow. A heavy cradle would hold it.",
                    11 => "A natural chip on the seam: the shell is already started here.",
                    12 => "Caked in clay: the seam is hidden. Wash it first, or work round the middle by eye.",
                    13 => $"It shifted on the cradle and the blow lost energy. Seat it firmer: {GameInput.Glyph("Move")} tilts the rock.",
                    14 => $"Close the bench clamp [{GameInput.Glyph("Interact")}] once the rock sits how you want it: it holds the shell firm.",
                    // damage, and why
                    20 + (int)StressModel.Cause.Heavy => "Something broke inside: that blow was too heavy for this shell.",
                    20 + (int)StressModel.Cause.OffSeam => "Something broke inside: off the seam, the shock goes into the cavity instead of the ring.",
                    20 + (int)StressModel.Cause.ThinShell => "Something broke inside: the shell is thin here, the chisel went through to the crystals.",
                    20 + (int)StressModel.Cause.Overstrike => "Something broke inside: hammering an open crack drives the shock into the crystals.",
                    20 + (int)StressModel.Cause.Wedge => "Something broke inside: the wedge drives too deep into a thin shell.",
                    20 + (int)StressModel.Cause.Unstable => "Something broke inside: the rock shifted under the blow.",
                    20 + (int)StressModel.Cause.None => "Something broke inside.",
                    // a weak bite, and why
                    40 + (int)StressModel.Cause.OffSeam => $"Weak bite: off the seam. The shell only splits along its ring  •  {GameInput.Glyph("Rotate")} rotate",
                    40 + (int)StressModel.Cause.Glancing => "Weak bite: the chisel stood at a glancing angle. Set it square to the shell.",
                    40 + (int)StressModel.Cause.Unstable => $"Weak bite: the rock moved and took the energy. Seat it firmer ({GameInput.Glyph("Move")} tilt).",
                    40 + (int)StressModel.Cause.Clay => "Weak bite: the clay cushions the blow. A wash helps.",
                    40 + (int)StressModel.Cause.ThickShell => "Weak bite: the shell runs thick here. Work the thinner side of the ring, or hit firmer.",
                    40 + (int)StressModel.Cause.Light => "Weak bite: too light for this matrix. Wind up a little more.",
                    40 + (int)StressModel.Cause.None => "Weak bite: the shell barely took it.",
                    _ => $"Hold {GameInput.Glyph("Strike")} to wind up  •  {GameInput.Glyph("Rotate")} rotate  •  {GameInput.Glyph("Move")} tilt  •  {GameInput.Glyph("Back")} leave",
                };
            }
        }
    }
}
