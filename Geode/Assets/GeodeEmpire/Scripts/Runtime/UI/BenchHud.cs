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
        private Label _title, _cracks, _hint, _resultName, _resultNote, _resultPrompt;
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
            UiKit.Label(_panel, "Force", "muted");
            var fbg = UiKit.Box(_panel, "meter-bg");
            _forceFill = UiKit.Box(fbg, "meter-fill", "meter-fill-force");
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
            _forceFill.style.width = Length.Percent(_bench.Charge * 100f);
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
            else if (_bench.Charge > 0.75f) state = 2;
            else if (_bench.Charge > 0.02f) state = 3;
            else state = 4;
            if (state != _lastHintState || (state == 4 && GameInput.Scheme != _lastScheme))
            {
                _lastHintState = state; _lastScheme = GameInput.Scheme;
                _hint.text = state switch
                {
                    1 => "Aim on the rock",
                    2 => "Heavy blow: fast, but crystals near the seam may break",
                    3 => "Release to strike",
                    5 => "Slipped! Aim squarely at the shell.",
                    6 => "That segment is already cracked. Work around the ring.",
                    _ => $"Hold {GameInput.Glyph("Strike")} to wind up  •  {GameInput.Glyph("Rotate")} rotate  •  {GameInput.Glyph("Back")} leave",
                };
            }
        }
    }
}
