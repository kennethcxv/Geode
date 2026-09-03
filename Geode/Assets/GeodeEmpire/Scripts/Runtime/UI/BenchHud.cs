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
        private VisualElement _root, _reticle, _panel, _forceFill, _progressFill, _progressRow, _result;
        private Label _title, _cracks, _hint, _resultName, _resultNote, _resultPrompt, _zone;
        private int _lastZone = -1;
        private float _flash;
        private int _lastCracks = -1;
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
            var forceRow = UiKit.Box(_panel, "row");
            forceRow.style.justifyContent = Justify.SpaceBetween;
            UiKit.Label(forceRow, "Force", "muted");
            _zone = UiKit.Label(forceRow, "", "muted");
            var fbg = UiKit.Box(_panel, "meter-bg");
            _forceFill = UiKit.Box(fbg, "meter-fill", "meter-fill-force");
            // zone ticks on the meter: tap | careful | firm | heavy
            foreach (float z in new[] { CrackingBench.ForceTap, CrackingBench.ForceCareful, CrackingBench.ForceFirm })
            {
                var tick = UiKit.Box(fbg);
                tick.style.position = Position.Absolute; tick.style.left = Length.Percent(z * 100f); tick.style.top = 0; tick.style.bottom = 0;
                tick.style.width = 2; tick.style.backgroundColor = new Color(1f, 1f, 1f, 0.35f);
                tick.pickingMode = PickingMode.Ignore;
            }
            _progressRow = UiKit.Box(_panel);
            UiKit.Label(_progressRow, "Fracture ring", "muted");
            var pbg = UiKit.Box(_progressRow, "meter-bg");
            _progressFill = UiKit.Box(pbg, "meter-fill");
            _cracks = UiKit.Label(_panel, "", "");
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
            _forceFill.style.width = Length.Percent(_bench.Charge * 100f);
            int zone = _bench.Charge <= 0.02f ? 0 : _bench.Charge < CrackingBench.ForceTap ? 1 : _bench.Charge < CrackingBench.ForceCareful ? 2 : _bench.Charge < CrackingBench.ForceFirm ? 3 : 4;
            if (zone != _lastZone) { _lastZone = zone; _zone.text = zone == 0 ? "" : CrackingBench.ForceZoneName(_bench.Charge).ToUpper(); }
            _flash = Mathf.Max(0f, _flash - Time.deltaTime * 3f);
            bool lamp = _bench.HasLamp;
            _progressRow.style.display = lamp ? DisplayStyle.Flex : DisplayStyle.None;
            if (lamp) _progressFill.style.width = Length.Percent(_bench.Model.Progress() * 100f);
            int cracks = _bench.Model.CrackedCount();
            if (cracks != _lastCracks)
            {
                _lastCracks = cracks;
                _cracks.text = cracks == 0 ? "No cracks yet" : $"{cracks} of {StressModel.Sectors} seam segments cracked";
            }
            // the hint only changes with state, never per frame: build the string when the state does
            int state;
            if (_bench.LastResult.Slipped && _flash > 0.5f) state = 5;
            else if (_bench.LastResult.Overstrike && _flash > 0.5f) state = 6;
            else if (!_bench.AimValid) state = 1;
            else if (_bench.Charge >= CrackingBench.ForceFirm) state = 2;
            else if (_bench.Charge > 0.02f) state = 3;
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
                    _ => $"Hold {GameInput.Glyph("Strike")} to wind up  •  {GameInput.Glyph("Rotate")} rotate  •  {GameInput.Glyph("Back")} leave",
                };
            }
        }
    }
}
