using UnityEngine;
using UnityEngine.UIElements;
using GeodeEmpire.Core;
using GeodeEmpire.Cracking;

namespace GeodeEmpire.UI
{
    /// <summary>Cracker-view overlay: the seat (how level the seam sits under the chain), the chain, the gauge in words, a hint.</summary>
    public sealed class CrackerHud : MonoBehaviour
    {
        private CrackerStation _st;
        private VisualElement _root, _panel, _result;
        private Label _mode, _seat, _gauge, _hint, _resultName, _resultNote, _resultPrompt;
        private int _lastHint = -1;
        private ControlScheme _lastScheme;

        private void Start()
        {
            // the station sits under the Stage-2 root, inactive until the expansion is bought: bind to it whenever it appears
            _st = FindAnyObjectByType<CrackerStation>(FindObjectsInactive.Include);
            var hud = HudController.Instance;
            if (_st == null || hud == null) { enabled = false; return; }
            _root = hud.GetComponent<UIDocument>().rootVisualElement;
            _panel = UiKit.Box(_root, "card", "bench-panel");
            UiKit.Label(_panel, "GEODE CRACKER", "bench-title");
            _mode = UiKit.Label(_panel, "", "appraisal-line", "accent");
            _seat = UiKit.Label(_panel, "", "");
            _gauge = UiKit.Label(_panel, "", "muted");
            _hint = UiKit.Label(_panel, "", "bench-hint");
            _hint.style.whiteSpace = WhiteSpace.Normal;
            _panel.style.display = DisplayStyle.None;
            _result = UiKit.Box(_root, "card");
            _result.style.position = Position.Absolute;
            _result.style.left = Length.Percent(50); _result.style.top = Length.Percent(78);
            _result.style.translate = new Translate(Length.Percent(-50), 0);
            _result.style.alignItems = Align.Center;
            _resultName = UiKit.Label(_result, "", "appraisal-name", "bold");
            _resultNote = UiKit.Label(_result, "", "appraisal-line", "accent");
            _resultPrompt = UiKit.Label(_result, "", "muted");
            _result.style.display = DisplayStyle.None;
            _st.Entered += OnEntered; _st.Exited += OnExited; _st.Revealed += OnRevealed;
        }

        private void OnDestroy()
        {
            if (_st == null) return;
            _st.Entered -= OnEntered; _st.Exited -= OnExited; _st.Revealed -= OnRevealed;
        }

        private void OnEntered() { _lastHint = -1; HudController.Instance.SetFreeRoamVisible(false); _panel.style.display = DisplayStyle.Flex; _result.style.display = DisplayStyle.None; }
        private void OnExited() { HudController.Instance.SetFreeRoamVisible(true); _panel.style.display = DisplayStyle.None; _result.style.display = DisplayStyle.None; }

        private void OnRevealed()
        {
            if (!_st.Active || _st.Rock == null) return;
            _panel.style.display = DisplayStyle.None;
            _result.style.display = DisplayStyle.Flex;
            _resultName.text = _st.Rock.Record.DisplayName;
            _resultNote.text = _st.ResultNote;
            _resultPrompt.text = $"[{GameInput.Glyph("Interact")}] Take specimen     [{GameInput.Glyph("Back")}] Leave it";
        }

        private void Update()
        {
            if (_st == null || !_st.Active || _st.State == CrackerStation.Phase.Done || _st.State == CrackerStation.Phase.Splitting) return;
            var ph = _st.State;
            _mode.text = ph == CrackerStation.Phase.Seat ? "Seat the rock" : ph == CrackerStation.Phase.Tighten ? "Take up the slack" : "Squeeze";
            float tilt = _st.TiltAngle;
            _seat.text = $"Seam under the chain: {_st.AlignmentWord} ({tilt:F0}°)";
            _seat.style.color = tilt < 8f ? new Color(0.75f, 0.75f, 0.72f) : tilt < 20f ? new Color(1f, 0.85f, 0.5f) : new Color(1f, 0.55f, 0.35f);
            _gauge.text = ph == CrackerStation.Phase.Pressure ? $"Gauge: {_st.Pressure * 10f:F1}" + (_st.Pressure / Mathf.Max(0.01f, _st.SplitPressure) > 0.8f ? "  •  the shell is groaning" : "") : ph == CrackerStation.Phase.Tighten ? $"Chain: {Mathf.RoundToInt(_st.Tighten * 100f)}% taken up" : "Chain: slack";
            int hint = !string.IsNullOrEmpty(_st.Note) ? 9 : ph == CrackerStation.Phase.Seat ? (tilt >= 8f ? 2 : 1) : ph == CrackerStation.Phase.Tighten ? 3 : 4;
            if (hint != _lastHint || GameInput.Scheme != _lastScheme)
            {
                _lastHint = hint; _lastScheme = GameInput.Scheme;
                _hint.text = hint switch
                {
                    1 => $"{GameInput.Glyph("Rotate")} turn   {GameInput.Glyph("Move")} tilt until the seam sits level   [{GameInput.Glyph("Interact")}] lay the chain   [{GameInput.Glyph("Back")}] leave",
                    2 => $"The seam is off level: the chain will ride up the shell under load. {GameInput.Glyph("Move")} tilts the rock.",
                    3 => $"Hold [{GameInput.Glyph("Interact")}] to pump the lever and take up the slack.",
                    4 => $"Hold {GameInput.Glyph("Strike")} to squeeze. Listen: creaks, then ticks, then it lets go all the way round. Past the limit a thin shell shatters.",
                    9 => _st.Note,
                    _ => "",
                };
            }
        }
    }
}
