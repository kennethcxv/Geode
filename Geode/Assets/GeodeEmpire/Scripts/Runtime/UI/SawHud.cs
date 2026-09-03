using UnityEngine;
using UnityEngine.UIElements;
using GeodeEmpire.Core;
using GeodeEmpire.Lapidary;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.UI
{
    /// <summary>Saw-view overlay: the plan (yaw, roll, across), the cost of the cut, the blade load and progress, the result.</summary>
    public sealed class SawHud : MonoBehaviour
    {
        private SawStation _saw;
        private VisualElement _root, _panel, _loadFill, _progressFill, _loadRow, _progressRow, _result;
        private Label _mode, _plan, _estimate, _blade, _hint, _resultName, _resultNote, _resultPrompt, _loadLabel;
        private int _lastHint = -1;
        private ControlScheme _lastScheme;

        private void Start()
        {
            _saw = FindAnyObjectByType<SawStation>();
            var hud = HudController.Instance;
            if (_saw == null || hud == null) { enabled = false; return; }
            _root = hud.GetComponent<UIDocument>().rootVisualElement;
            _panel = UiKit.Box(_root, "card", "bench-panel");
            UiKit.Label(_panel, "TRIM SAW", "bench-title");
            _mode = UiKit.Label(_panel, "", "appraisal-line", "accent");
            _plan = UiKit.Label(_panel, "", "");
            _estimate = UiKit.Label(_panel, "", "muted");
            _blade = UiKit.Label(_panel, "", "muted");
            _loadRow = UiKit.Box(_panel);
            var lr = UiKit.Box(_loadRow, "row"); lr.style.justifyContent = Justify.SpaceBetween;
            UiKit.Label(lr, "Blade load", "muted");
            _loadLabel = UiKit.Label(lr, "", "muted");
            var lbg = UiKit.Box(_loadRow, "meter-bg");
            _loadFill = UiKit.Box(lbg, "meter-fill", "meter-fill-force");
            var red = UiKit.Box(lbg);
            red.style.position = Position.Absolute; red.style.left = Length.Percent(70f); red.style.top = 0; red.style.bottom = 0; red.style.right = 0;
            red.style.backgroundColor = new Color(0.9f, 0.35f, 0.3f, 0.18f); red.pickingMode = PickingMode.Ignore;
            _progressRow = UiKit.Box(_panel);
            UiKit.Label(_progressRow, "Cut", "muted");
            var pbg = UiKit.Box(_progressRow, "meter-bg");
            _progressFill = UiKit.Box(pbg, "meter-fill");
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

            _saw.Entered += OnEntered;
            _saw.Exited += OnExited;
            _saw.Finished += OnFinished;
        }

        private void OnDestroy()
        {
            if (_saw == null) return;
            _saw.Entered -= OnEntered; _saw.Exited -= OnExited; _saw.Finished -= OnFinished;
        }

        private void OnEntered()
        {
            _lastHint = -1;
            HudController.Instance.SetFreeRoamVisible(false);
            _panel.style.display = DisplayStyle.Flex;
            _result.style.display = DisplayStyle.None;
        }

        private void OnExited()
        {
            HudController.Instance.SetFreeRoamVisible(true);
            _panel.style.display = DisplayStyle.None;
            _result.style.display = DisplayStyle.None;
        }

        private void OnFinished()
        {
            _panel.style.display = DisplayStyle.None;
            _result.style.display = DisplayStyle.Flex;
            var a = _saw.PieceA; var b = _saw.PieceB;
            _resultName.text = a != null ? a.Record.DisplayName + (b != null ? "  +  " + b.Record.DisplayName : "") : "";
            _resultNote.text = _saw.ResultNote;
            _resultPrompt.text = $"[{GameInput.Glyph("Interact")}] Take the better piece     [{GameInput.Glyph("Back")}] Leave them";
        }

        private void Update()
        {
            if (_saw == null || !_saw.Active || _saw.State == SawStation.Phase.Done) return;
            bool orient = _saw.State == SawStation.Phase.Orient;
            _mode.text = orient ? (_saw.CanRotate ? "Set the cut" : "Set the cut (parallel to the face)") : _saw.Feeding ? "Feeding" : "Clamped  •  motor running";
            _plan.text = orient || true ? $"Turn {_saw.Yaw:F0}°   Tilt {_saw.Roll:F0}°   Across {_saw.Offset * 1000f:+0;-0;0} mm" : "";
            _saw.Estimate(out float secs, out float wear, out float cost);
            _estimate.text = orient ? $"About {secs:F0} s  •  blade wear {Mathf.RoundToInt(wear * 100f)}% (≈ {UiKit.Money(cost)})" : $"{Mathf.RoundToInt(_saw.Progress * 100f)}% through";
            float bw = _saw.BladeWear;
            _blade.text = $"Blade {Mathf.RoundToInt((1f - bw) * 100f)}% left" + (_saw.BladeDull ? "  •  dull" : "") + (_saw.ThinBlade ? "  •  thin kerf" : "") + (_saw.Coolant ? "  •  coolant" : "");
            _loadRow.style.display = orient ? DisplayStyle.None : DisplayStyle.Flex;
            _progressRow.style.display = orient ? DisplayStyle.None : DisplayStyle.Flex;
            if (!orient)
            {
                float load = Mathf.Clamp01(_saw.Load / 1.4f);
                _loadFill.style.width = Length.Percent(load * 100f);
                _loadFill.style.backgroundColor = _saw.Overload > 0.05f ? new Color(0.92f, 0.36f, 0.3f) : new Color(0.91f, 0.59f, 0.35f);
                _loadLabel.text = _saw.Overload > 0.05f ? "BOGGING" : _saw.Load > 0.5f ? "cutting" : "";
                _progressFill.style.width = Length.Percent(_saw.Progress * 100f);
            }
            int hint = orient ? (_saw.CanRotate ? 1 : 2) : _saw.Overload > 0.05f ? 4 : _saw.Feeding ? 3 : 5;
            if (hint != _lastHint || GameInput.Scheme != _lastScheme)
            {
                _lastHint = hint; _lastScheme = GameInput.Scheme;
                bool pad = GameInput.UsingGamepad;
                _hint.text = hint switch
                {
                    1 => $"{GameInput.Glyph("Rotate")} turn   {(pad ? "RS ↕" : "Mouse ↕")} tilt   {(pad ? "LS ↔" : "A / D")} slide across   [{GameInput.Glyph("Interact")}] clamp and start   [{GameInput.Glyph("Back")}] leave",
                    2 => $"A piece rides flat on the jaw: {(pad ? "LS ↔" : "A / D")} to set the depth   [{GameInput.Glyph("Interact")}] clamp and start   [{GameInput.Glyph("Back")}] leave",
                    3 => "Feeding: ease off through thick stone if the load runs into the red",
                    4 => "The blade is bogging: let go a moment, it clears the slurry and the load drops",
                    _ => $"Hold {GameInput.Glyph("Strike")} to feed the carriage   ({GameInput.Glyph("Sprint")} with it for a fast feed)",
                };
            }
        }
    }
}
