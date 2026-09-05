using UnityEngine;
using UnityEngine.UIElements;
using GeodeEmpire.Core;
using GeodeEmpire.Workshop;

namespace GeodeEmpire.UI
{
    /// <summary>
    /// The basin overlay: where the bristles are, how much water is left in them, and how much clay is still on
    /// the rock. Everything else the player needs is on the rock itself — which patches are still dark — so this
    /// stays a thin strip rather than a progress bar standing in for the work.
    /// </summary>
    public sealed class WashHud : MonoBehaviour
    {
        private WashStation _wash;
        private VisualElement _root, _brushDot, _panel, _wetTrack, _wetFill;
        private Label _title, _left, _note, _hint;
        private string _lastNote = "", _lastLeft = "", _lastHint = "";
        private ControlScheme _lastScheme;

        private void Start()
        {
            _wash = FindAnyObjectByType<WashStation>();
            var hud = HudController.Instance;
            if (_wash == null || hud == null) { enabled = false; return; }
            _root = hud.GetComponent<UIDocument>().rootVisualElement;

            _brushDot = UiKit.Box(_root, "crosshair-ring");
            _brushDot.style.width = 26; _brushDot.style.height = 26;
            _brushDot.style.borderTopLeftRadius = 13; _brushDot.style.borderTopRightRadius = 13;
            _brushDot.style.borderBottomLeftRadius = 13; _brushDot.style.borderBottomRightRadius = 13;
            _brushDot.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            _brushDot.style.display = DisplayStyle.None;

            _panel = UiKit.Box(_root, "card", "bench-panel");
            _title = UiKit.Label(_panel, "WASH BASIN", "bench-title");
            var wetRow = UiKit.Box(_panel, "row");
            wetRow.style.justifyContent = Justify.SpaceBetween;
            wetRow.style.alignItems = Align.Center;
            UiKit.Label(wetRow, "Brush", "muted");
            _wetTrack = UiKit.Box(wetRow, "xp-track");
            _wetTrack.style.width = 96;
            _wetFill = UiKit.Box(_wetTrack, "xp-fill");
            _left = UiKit.Label(_panel, "", "");
            _note = UiKit.Label(_panel, "", "muted");
            _note.style.whiteSpace = WhiteSpace.Normal;
            _hint = UiKit.Label(_panel, "", "bench-hint");
            _hint.style.whiteSpace = WhiteSpace.Normal;
            _panel.style.display = DisplayStyle.None;

            _wash.Entered += OnEntered;
            _wash.Exited += OnExited;
        }

        private void OnDestroy()
        {
            if (_wash == null) return;
            _wash.Entered -= OnEntered;
            _wash.Exited -= OnExited;
        }

        private void OnEntered()
        {
            _panel.style.display = DisplayStyle.Flex;
            _brushDot.style.display = DisplayStyle.Flex;
            _lastHint = "";
        }

        private void OnExited()
        {
            _panel.style.display = DisplayStyle.None;
            _brushDot.style.display = DisplayStyle.None;
        }

        private void Update()
        {
            if (_wash == null || !_wash.Active) return;
            var c = _wash.Cursor;
            _brushDot.style.left = Length.Percent(c.x * 100f);
            _brushDot.style.top = Length.Percent((1f - c.y) * 100f);
            _brushDot.style.opacity = _wash.BrushOnRock ? 1f : 0.4f;
            // white on the rock, blue over the water: the two things the brush can be doing
            var col = _wash.BrushOnRock ? Color.white : new Color(0.55f, 0.78f, 1f, 0.8f);
            _brushDot.style.borderTopColor = col; _brushDot.style.borderBottomColor = col;
            _brushDot.style.borderLeftColor = col; _brushDot.style.borderRightColor = col;

            _wetFill.style.width = Length.Percent(Mathf.Clamp01(_wash.BrushWet) * 100f);
            _wetFill.style.backgroundColor = _wash.BrushWet < 0.15f
                ? new Color(0.85f, 0.5f, 0.25f) : new Color(0.42f, 0.68f, 0.95f);

            string left = _wash.DirtyRegions == 0 ? "Clean"
                : _wash.DirtyRegions + (_wash.DirtyRegions == 1 ? " patch of clay left" : " patches of clay left");
            if (left != _lastLeft) { _lastLeft = left; _left.text = left; }
            if (_wash.Note != _lastNote) { _lastNote = _wash.Note; _note.text = _wash.Note; }

            if (_lastHint == "" || GameInput.Scheme != _lastScheme)
            {
                _lastScheme = GameInput.Scheme;
                _lastHint = $"{GameInput.Glyph("Look")} move the brush   {GameInput.Glyph("Interact")} scrub   " +
                            $"{GameInput.Glyph("Rotate")} turn the rock   {GameInput.Glyph("Strike")} tip it over   " +
                            $"{GameInput.Glyph("Back")} done";
                _hint.text = _lastHint;
            }
        }
    }
}
