using UnityEngine;
using UnityEngine.UIElements;
using GeodeEmpire.Core;
using GeodeEmpire.Lapidary;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.UI
{
    /// <summary>
    /// Saw-view overlay, physical-first: the plan (turn, tilt, across), what the cut will cost, the blade and the
    /// coolant valve, then a short line while cutting. The load is read off the machine's meter, not a HUD bar.
    /// </summary>
    public sealed class SawHud : MonoBehaviour
    {
        private SawStation _saw;
        private VisualElement _root, _panel, _result;
        private Label _mode, _plan, _estimate, _blade, _coolant, _progress, _hint, _resultName, _resultNote, _resultPrompt;
        private int _lastHint = -1;
        private ControlScheme _lastScheme;
        private string _lastCoolant = "", _lastBlade = "";

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
            _coolant = UiKit.Label(_panel, "", "muted");
            _progress = UiKit.Label(_panel, "", "muted");
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
            _lastHint = -1; _lastCoolant = ""; _lastBlade = "";
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
            if (!_saw.Active) return;
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
            bool pad = GameInput.UsingGamepad;
            bool tooTall = orient && !_saw.FitsUnderArbor;
            _mode.text = orient ? (tooTall ? "Too tall in this pose" : _saw.CanRotate ? "Set the cut" : "Set the cut (parallel to the face)")
                : _saw.Feeding ? "Feeding" : "Clamped  •  motor running";
            _plan.text = $"Turn {_saw.Yaw:F0}°   Tilt {_saw.Roll:F0}°   Across {_saw.Offset * 1000f:+0;-0;0} mm" + (orient ? $"   •   {_saw.RockHeight * 100f:F0} cm high (the arbor passes {_saw.MaxPassHeight * 100f:F0})" : "");
            _saw.Estimate(out float secs, out float wear, out float cost);
            _estimate.text = orient ? $"About {secs:F0} s  •  blade wear {Mathf.RoundToInt(wear * 100f)}% (≈ {UiKit.Money(cost)})" : "";
            _estimate.style.display = orient ? DisplayStyle.Flex : DisplayStyle.None;
            float bw = _saw.BladeWear;
            string bladeText = $"Blade {Mathf.RoundToInt((1f - bw) * 100f)}% left" + (_saw.BladeDull ? "  •  dull" : "") + (_saw.ThinForCut ? "  •  thin kerf" : "  •  standard kerf") + (_saw.ThinBladeOwned && orient ? $"   [{GameInput.Glyph("Loupe")}] swap" : "");
            if (bladeText != _lastBlade) { _lastBlade = bladeText; _blade.text = bladeText; }
            string coolantText = $"Coolant valve: {_saw.CoolantWord}   [{GameInput.Glyph("Drop")}] {(_saw.CoolantOpen ? "close" : "open")}";
            if (coolantText != _lastCoolant) { _lastCoolant = coolantText; _coolant.text = coolantText; _coolant.style.color = _saw.CoolantOpen ? new Color(0.75f, 0.75f, 0.72f) : new Color(1f, 0.7f, 0.45f); }
            bool cutting = _saw.State == SawStation.Phase.Cutting;
            _progress.style.display = cutting ? DisplayStyle.Flex : DisplayStyle.None;
            if (cutting) _progress.text = $"{Mathf.RoundToInt(_saw.Progress * 100f)}% through" + (_saw.Overload > 0.05f ? "  •  the meter is in the red" : _saw.Load > 0.5f ? "  •  cutting" : "") + (_saw.Grip < 0.85f ? "  •  loose in the jaws" : "");
            int hint = orient ? (tooTall ? 7 : _saw.CanRotate ? (!_saw.CoolantOpen ? 6 : 1) : 2)
                : _saw.Overload > 0.05f ? 4 : !_saw.CoolantOpen && _saw.Feeding ? 9 : _saw.Feeding ? 3 : 5;
            if (hint != _lastHint || GameInput.Scheme != _lastScheme)
            {
                _lastHint = hint; _lastScheme = GameInput.Scheme;
                _hint.text = hint switch
                {
                    1 => $"{GameInput.Glyph("Rotate")} turn   {(pad ? "RS ↕" : "Mouse ↕")} tilt   {(pad ? "LS ↔" : "A / D")} slide across   [{GameInput.Glyph("Interact")}] clamp and start   [{GameInput.Glyph("Back")}] leave",
                    2 => $"A piece stands on edge, face to the blade: {(pad ? "LS ↔" : "A / D")} to set the depth   [{GameInput.Glyph("Interact")}] clamp and start   [{GameInput.Glyph("Back")}] leave",
                    3 => "Feeding: ease off through thick stone when the meter climbs into the red",
                    4 => "The blade is bogging: let go a moment, it clears the slurry and the meter drops",
                    6 => $"The coolant valve is closed: a dry cut chips and eats the blade. [{GameInput.Glyph("Drop")}] opens it.   {GameInput.Glyph("Rotate")} turn   {(pad ? "LS ↔" : "A / D")} across   [{GameInput.Glyph("Interact")}] clamp",
                    7 => $"The rock would hit the arbor: tilt it flatter ({(pad ? "RS ↕" : "Mouse ↕")}) or turn it ({GameInput.Glyph("Rotate")}) until it passes under. Anything taller waits for a bigger saw.",
                    9 => "Cutting dry: the blade is heating and chipping. Open the valve.",
                    _ => $"Hold {GameInput.Glyph("Strike")} to feed the carriage   ({GameInput.Glyph("Sprint")} with it for a fast feed)",
                };
            }
        }
    }
}
