using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using GeodeEmpire.Audio;
using GeodeEmpire.Core;

namespace GeodeEmpire.UI
{
    /// <summary>
    /// The settings UI. Five tabs; every control is bound straight to a GameSettings field and applied live. Built for
    /// a controller as much as a mouse: shoulder buttons or left/right on a tab switch tabs, sliders step 5% per press,
    /// choice controls cycle with left/right, and nothing opens a popup. Display changes get a timed revert.
    /// </summary>
    public sealed class SettingsPage
    {
        public static readonly string[] TabNames = { "Gameplay", "Controls", "Camera", "Graphics", "Audio" };
        public const float ConfirmSeconds = 12f;

        public VisualElement Root { get; }
        public int Tab { get; private set; }
        public bool ConfirmOpen => _confirm.style.display == DisplayStyle.Flex;

        private readonly List<Button> _tabs = new List<Button>();
        private readonly VisualElement _tabRow, _footer;
        private readonly ScrollView _body;
        private readonly VisualElement _confirm;
        private readonly Label _confirmText;
        private readonly Button _confirmKeep;
        private readonly Action _back;
        private readonly List<VisualElement> _controls = new List<VisualElement>();
        private int _focusRow = -1;
        private float _confirmLeft;
        private int _prevDisplayMode, _prevW, _prevH;

        private static GameSettings S => GameSettings.Current;

        public SettingsPage(VisualElement parent, Action onBack)
        {
            _back = onBack;
            Root = UiKit.Box(parent, "settings");
            UiKit.Label(Root, "SETTINGS", "panel-title", "bold");
            UiKit.Label(Root, "Changes apply immediately and are kept when you leave.", "panel-subtitle");
            var tabRow = _tabRow = UiKit.Box(Root, "tab-row");
            for (int i = 0; i < TabNames.Length; i++)
            {
                int idx = i;
                var b = new Button(() => Select(idx)) { text = TabNames[i] };
                b.AddToClassList("tab");
                b.RegisterCallback<FocusInEvent>(_ => { if (Tab != idx) Select(idx, false); });
                b.RegisterCallback<NavigationMoveEvent>(e =>
                {
                    if (e.direction == NavigationMoveEvent.Direction.Left || e.direction == NavigationMoveEvent.Direction.Right)
                    {
                        Select((Tab + (e.direction == NavigationMoveEvent.Direction.Right ? 1 : TabNames.Length - 1)) % TabNames.Length);
                    }
                    else if (e.direction == NavigationMoveEvent.Direction.Down) FocusFirstControl();
                    else return;
                    e.StopPropagation();
                    b.panel?.focusController?.IgnoreEvent(e);
                });
                tabRow.Add(b);
                _tabs.Add(b);
            }
            _body = new ScrollView(ScrollViewMode.Vertical);
            _body.AddToClassList("settings-scroll");
            _body.verticalScrollerVisibility = ScrollerVisibility.Auto;
            _body.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            // the scrollbar must never take controller focus away from the controls
            _body.verticalScroller.focusable = false;
            _body.verticalScroller.slider.focusable = false;
            _body.verticalScroller.lowButton.focusable = false;
            _body.verticalScroller.highButton.focusable = false;
            Root.Add(_body);

            var footer = _footer = UiKit.Box(Root, "settings-footer");
            var left = UiKit.Box(footer, "row");
            var resetSection = UiKit.Button(left, "Reset section", () => { S.ResetSection(Tab); if (Tab == 3) ApplyDisplayWithConfirm(); AfterChange(true); }, "btn-ghost");
            resetSection.style.marginRight = 10;
            UiKit.Button(left, "Reset all", () => { S.ResetAll(); ApplyDisplayWithConfirm(); AfterChange(true); }, "btn-ghost");
            UiKit.Button(footer, "Back", () => _back?.Invoke(), "btn-ghost");

            _confirm = UiKit.Box(Root, "confirm-card");
            UiKit.Label(_confirm, "KEEP DISPLAY SETTINGS?", "section");
            _confirmText = UiKit.Label(_confirm, "", "panel-subtitle");
            var cr = UiKit.Box(_confirm, "row");
            _confirmKeep = UiKit.Button(cr, "Keep", () => CloseConfirm(true), "btn-primary");
            _confirmKeep.style.marginRight = 10;
            var revert = UiKit.Button(cr, "Revert", () => CloseConfirm(false), "btn-ghost");
            // while the question is up, focus only moves between its two answers
            foreach (var b in new[] { _confirmKeep, revert })
            {
                var other = b == _confirmKeep ? revert : _confirmKeep;
                b.RegisterCallback<NavigationMoveEvent>(e =>
                {
                    if (e.direction == NavigationMoveEvent.Direction.Left || e.direction == NavigationMoveEvent.Direction.Right) other.Focus();
                    e.StopPropagation();
                    b.panel?.focusController?.IgnoreEvent(e);
                });
            }
            _confirm.style.display = DisplayStyle.None;
            RememberDisplay();
            Select(0, false);
        }

        // ---- tabs & focus -----------------------------------------------------------------

        public void Select(int i, bool focusTab = true)
        {
            Tab = (i + TabNames.Length) % TabNames.Length;
            for (int k = 0; k < _tabs.Count; k++) _tabs[k].EnableInClassList("tab-active", k == Tab);
            Rebuild();
            if (focusTab) _tabs[Tab].Focus();
        }

        public void SwitchTab(int delta)
        {
            Select(Tab + delta, false);
            WorkshopAudio.Play2D("ui_click", 0.25f, 1.1f);
            FocusFirstControl();
        }

        public void FocusFirstControl()
        {
            var target = _controls.Count > 0 ? _controls[0] : (VisualElement)_tabs[Tab];
            target.Focus();
            Root.schedule.Execute(() => target.Focus());
        }

        private void Rebuild()
        {
            _body.Clear();
            _controls.Clear();
            switch (Tab)
            {
                case 0: BuildGameplay(); break;
                case 1: BuildControls(); break;
                case 2: BuildCamera(); break;
                case 3: BuildGraphics(); break;
                default: BuildAudio(); break;
            }
            _body.scrollOffset = Vector2.zero;
        }

        /// <summary>Apply, and when a control changed something other rows display (preset, reset) rebuild with focus kept on the same row.</summary>
        private void AfterChange(bool rebuild)
        {
            S.Apply();
            if (!rebuild) return;
            int row = _focusRow;
            Rebuild();
            if (row >= 0 && row < _controls.Count)
            {
                var c = _controls[row];
                c.Focus();
                Root.schedule.Execute(() => c.Focus());
            }
        }

        // ---- display confirm --------------------------------------------------------------

        private void RememberDisplay() { _prevDisplayMode = S.DisplayMode; _prevW = S.ResolutionWidth; _prevH = S.ResolutionHeight; }

        private void ApplyDisplayWithConfirm()
        {
            if (S.DisplayMode == _prevDisplayMode && S.ResolutionWidth == _prevW && S.ResolutionHeight == _prevH) return;
            S.ApplyDisplay();
            _confirmLeft = ConfirmSeconds;
            _confirm.style.display = DisplayStyle.Flex;
            _body.SetEnabled(false); _tabRow.SetEnabled(false); _footer.SetEnabled(false);
            UpdateConfirmText();
            _confirmKeep.Focus();
            Root.schedule.Execute(() => _confirmKeep.Focus());
        }

        private void UpdateConfirmText()
        {
            var res = S.EffectiveResolution();
            _confirmText.text = $"{GameSettings.DisplayModeNames[S.DisplayMode]}  {res.x} × {res.y}   ·   reverting in {Mathf.CeilToInt(_confirmLeft)} s";
        }

        private void CloseConfirm(bool keep)
        {
            if (!ConfirmOpen) return;
            if (!keep)
            {
                S.DisplayMode = _prevDisplayMode; S.ResolutionWidth = _prevW; S.ResolutionHeight = _prevH;
                S.ApplyDisplay();
            }
            RememberDisplay();
            _confirm.style.display = DisplayStyle.None;
            _body.SetEnabled(true); _tabRow.SetEnabled(true); _footer.SetEnabled(true);
            S.Save();
            if (!keep && Tab == 3) AfterChange(true);
            else FocusRow(_focusRow);
        }

        private void FocusRow(int row)
        {
            if (row < 0 || row >= _controls.Count) { FocusFirstControl(); return; }
            var c = _controls[row];
            c.Focus();
            Root.schedule.Execute(() => c.Focus());
        }

        /// <summary>Per-frame from the owner (unscaled time: the pause menu runs at timeScale 0).</summary>
        public void Tick(float unscaledDt)
        {
            if (!ConfirmOpen) return;
            _confirmLeft -= unscaledDt;
            UpdateConfirmText();
            if (_confirmLeft <= 0f) CloseConfirm(false);
        }

        /// <summary>Back/cancel pressed while the page is up. Returns true when it was consumed here.</summary>
        public bool HandleCancel()
        {
            if (ConfirmOpen) { CloseConfirm(false); return true; }
            return false;
        }

        // ---- rows -------------------------------------------------------------------------

        private VisualElement Row(string label, string desc)
        {
            var row = UiKit.Box(_body, "slider-row");
            var text = UiKit.Box(row, "setting-text");
            UiKit.Label(text, label, "slider-label");
            if (!string.IsNullOrEmpty(desc)) UiKit.Label(text, desc, "setting-desc");
            return row;
        }

        private void Track(VisualElement control)
        {
            int idx = _controls.Count;
            _controls.Add(control);
            control.RegisterCallback<FocusInEvent>(_ => { _focusRow = idx; _body.ScrollTo(control); });
        }

        private static string Pct(float v) => Mathf.RoundToInt(v * 100f) + "%";
        private static string Mult(float v) => v.ToString("0.00") + "×";
        private static string Deg(float v) => Mathf.RoundToInt(v) + "°";

        private void Slider(string label, string desc, float min, float max, float value, Func<float, string> fmt, Action<float> onChange, bool rebuild = false)
        {
            var row = Row(label, desc);
            var sl = new Slider(min, max) { value = value };
            float step = (max - min) / 20f;
            sl.RegisterCallback<NavigationMoveEvent>(e =>
            {
                if (e.direction != NavigationMoveEvent.Direction.Left && e.direction != NavigationMoveEvent.Direction.Right) return;
                sl.value = Mathf.Clamp(sl.value + (e.direction == NavigationMoveEvent.Direction.Right ? step : -step), min, max);
                e.StopPropagation();
                sl.panel?.focusController?.IgnoreEvent(e);
            });
            row.Add(sl);
            var val = UiKit.Label(row, fmt(value), "setting-value");
            sl.RegisterValueChangedCallback(e => { val.text = fmt(e.newValue); onChange(e.newValue); AfterChange(rebuild); });
            Track(sl);
        }

        private void Toggle(string label, string desc, bool value, Action<bool> onChange, bool rebuild = false)
        {
            var row = Row(label, desc);
            var t = new Toggle { value = value };
            t.RegisterValueChangedCallback(e => { onChange(e.newValue); WorkshopAudio.Play2D("ui_click", 0.25f, 1.1f); AfterChange(rebuild); });
            row.Add(t);
            Track(t);
        }

        private void Choice(string label, string desc, string[] options, int index, Action<int> onChange, bool rebuild = false)
        {
            var row = Row(label, desc);
            var box = UiKit.Box(row, "selector");
            int cur = Mathf.Clamp(index, 0, options.Length - 1);
            Button value = null;
            void Cycle(int d)
            {
                cur = (cur + d + options.Length) % options.Length;
                value.text = options[cur];
                WorkshopAudio.Play2D("ui_click", 0.25f, 1.1f);
                onChange(cur);
                AfterChange(rebuild);
            }
            var l = new Button(() => Cycle(-1)) { text = "‹", focusable = false };
            l.AddToClassList("btn"); l.AddToClassList("selector-arrow"); box.Add(l);
            value = new Button(() => Cycle(1)) { text = options[cur] };
            value.AddToClassList("btn"); value.AddToClassList("selector-value"); box.Add(value);
            var r = new Button(() => Cycle(1)) { text = "›", focusable = false };
            r.AddToClassList("btn"); r.AddToClassList("selector-arrow"); box.Add(r);
            value.RegisterCallback<NavigationMoveEvent>(e =>
            {
                if (e.direction != NavigationMoveEvent.Direction.Left && e.direction != NavigationMoveEvent.Direction.Right) return;
                Cycle(e.direction == NavigationMoveEvent.Direction.Right ? 1 : -1);
                e.StopPropagation();
                value.panel?.focusController?.IgnoreEvent(e);
            });
            Track(value);
        }

        // ---- tabs -------------------------------------------------------------------------

        private void BuildGameplay()
        {
            Toggle("Tutorial hints", "Step-by-step guidance through the first crate, crack and sale", S.ShowTutorial, v => S.ShowTutorial = v);
            Slider("Camera shake", "Kick when the hammer lands or a crate drops", 0f, 1.5f, S.CameraShake, Pct, v => S.CameraShake = v);
            Slider("Controller vibration", "Rumble on strikes and the final split", 0f, 1f, S.Vibration, Pct, v => S.Vibration = v);
        }

        private void BuildControls()
        {
            Slider("Mouse sensitivity", null, 0.2f, 3f, S.MouseSensitivity, Mult, v => S.MouseSensitivity = v);
            Slider("Controller look sensitivity", null, 0.2f, 3f, S.GamepadSensitivity, Mult, v => S.GamepadSensitivity = v);
            Toggle("Invert look Y", "Push up to look down, mouse and controller", S.InvertY, v => S.InvertY = v);
            Slider("Stick deadzone", "Ignore small stick movement and drift", 0.05f, 0.4f, S.StickDeadzone, Pct, v => S.StickDeadzone = v);
            var card = UiKit.Box(_body, "card", "bindings-card");
            UiKit.Label(card, "BINDINGS", "section");
            string[][] rows =
            {
                new[] { "Move / look", "WASD, mouse", "Left stick, right stick" },
                new[] { "Interact, pick up, place", "E", "A" },
                new[] { "Strike (hold to wind up)", "Left mouse", "RT" },
                new[] { "Inspect", "Right mouse", "LT" },
                new[] { "Rotate", "Q / R", "LB / RB" },
                new[] { "Drop", "G", "X" },
                new[] { "Loupe", "F", "Y" },
                new[] { "Tablet", "Tab", "Select" },
                new[] { "Pause", "Esc", "Start" },
            };
            foreach (var r in rows)
            {
                var line = UiKit.Box(card, "binding-row");
                UiKit.Label(line, r[0], "binding-action");
                UiKit.Label(line, r[1], "binding-key");
                UiKit.Label(line, r[2], "binding-key");
            }
        }

        private void BuildCamera()
        {
            Slider("Field of view", "Vertical, in the workshop; stations pull in closer", 55f, 95f, S.FieldOfView, Deg, v => S.FieldOfView = Mathf.Round(v));
            Slider("Head bob", null, 0f, 1f, S.HeadBobAmount, Pct, v => S.HeadBobAmount = v);
            Toggle("Reduced motion", "Turns off head bob and camera shake", S.ReducedMotion, v => S.ReducedMotion = v);
            Slider("Interface scale", "Size of every menu, prompt and card", 0.8f, 1.4f, S.UiScale, Mult, v => S.UiScale = v);
            Toggle("Show crosshair", null, S.CrosshairVisible, v => S.CrosshairVisible = v);
        }

        private void BuildGraphics()
        {
            var modeNames = new List<string>();
            var modeIds = new List<int>();
            for (int i = 0; i < GameSettings.DisplayModeNames.Length; i++)
            {
                if (i == 1 && !GameSettings.ExclusiveFullscreenSupported) continue;
                modeNames.Add(GameSettings.DisplayModeNames[i]); modeIds.Add(i);
            }
            int modeIndex = Mathf.Max(0, modeIds.IndexOf(S.DisplayMode));
            Choice("Display mode", null, modeNames.ToArray(), modeIndex, i => { S.DisplayMode = modeIds[i]; ApplyDisplayWithConfirm(); });

            var res = GameSettings.ResolutionOptions();
            var resNames = new List<string> { "Native" };
            foreach (var r in res) resNames.Add($"{r.x} × {r.y}");
            int resIndex = 0;
            for (int i = 0; i < res.Count; i++) if (res[i].x == S.ResolutionWidth && res[i].y == S.ResolutionHeight) resIndex = i + 1;
            Choice("Resolution", null, resNames.ToArray(), resIndex, i =>
            {
                if (i == 0) { S.ResolutionWidth = 0; S.ResolutionHeight = 0; }
                else { S.ResolutionWidth = res[i - 1].x; S.ResolutionHeight = res[i - 1].y; }
                ApplyDisplayWithConfirm();
            });

            Choice("Quality preset", "Shadows, anti-aliasing and render scale at once", GameSettings.QualityNames, S.QualityPreset, i => S.ApplyPreset(i), true);
            Choice("Shadows", null, GameSettings.ShadowNames, S.ShadowQuality, i => { S.ShadowQuality = i; S.RefreshPresetFromParts(); }, true);
            Choice("Anti-aliasing", null, GameSettings.AntiAliasingNames, S.AntiAliasing, i => { S.AntiAliasing = i; S.RefreshPresetFromParts(); }, true);
            Slider("Render scale", "Internal resolution; below 100% is faster, softer", 0.7f, 1.3f, S.RenderScale, Pct, v => { S.RenderScale = Mathf.Round(v * 20f) / 20f; S.RefreshPresetFromParts(); });
            Toggle("Post-processing", "Colour grading, tone mapping and vignette", S.PostProcessing, v => S.PostProcessing = v);
            Slider("Brightness", null, 0.6f, 1.4f, S.Brightness, Pct, v => S.Brightness = v);
            Toggle("VSync", null, S.VSync, v => S.VSync = v);
            var fpsNames = new string[GameSettings.FrameRateOptions.Length];
            for (int i = 0; i < fpsNames.Length; i++) fpsNames[i] = GameSettings.FrameRateOptions[i] == 0 ? "Uncapped" : GameSettings.FrameRateOptions[i] + " fps";
            Choice("Frame-rate limit", null, fpsNames, Mathf.Max(0, Array.IndexOf(GameSettings.FrameRateOptions, S.FrameRateLimit)), i => S.FrameRateLimit = GameSettings.FrameRateOptions[i]);
        }

        private void BuildAudio()
        {
            Slider("Master volume", null, 0f, 1f, S.MasterVolume, Pct, v => S.MasterVolume = v);
            Slider("Effects", "Hammer, chisel, crates, the shop", 0f, 1f, S.SfxVolume, Pct, v => S.SfxVolume = v);
            Slider("Ambience", "Room tone and the street outside", 0f, 1f, S.AmbienceVolume, Pct, v => S.AmbienceVolume = v);
            Slider("Interface", "Menus, tablet and prompts", 0f, 1f, S.UiVolume, Pct, v => S.UiVolume = v);
        }
    }
}
