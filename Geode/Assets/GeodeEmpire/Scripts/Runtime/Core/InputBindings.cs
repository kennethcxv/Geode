using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GeodeEmpire.Core
{
    /// <summary>
    /// Rebinding (V6 §62). The project-wide action asset is the source of truth; overrides live in the settings file
    /// as the Input System's own JSON, so a remap survives a reload and a new save alike.
    ///
    /// Everything that shows a key to the player goes through <see cref="GameInput.Glyph"/>, which asks the asset
    /// what the action is actually bound to. Nothing prints "E" because someone typed "E".
    /// </summary>
    public static class InputBindings
    {
        public const string KeyboardScheme = "Keyboard&Mouse";
        public const string GamepadScheme = "Gamepad";

        /// <summary>Raised when a binding changes, so every prompt and hint re-reads itself.</summary>
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Changed = null; _rebinding?.Dispose(); _rebinding = null; }

        /// <summary>The actions a player may remap, in the order the Controls page lists them.</summary>
        public sealed class Entry
        {
            public string Action;
            public string Label;
            /// <summary>A composite (Move) is shown but not rebound one control at a time: too many ways to break it.</summary>
            public bool Fixed;
        }

        public static readonly Entry[] Rebindable =
        {
            new Entry { Action = "Move", Label = "Move", Fixed = true },
            new Entry { Action = "Look", Label = "Look", Fixed = true },
            new Entry { Action = "Interact", Label = "Interact, pick up, place" },
            new Entry { Action = "Strike", Label = "Strike (hold to wind up)" },
            new Entry { Action = "Inspect", Label = "Inspect" },
            new Entry { Action = "Rotate", Label = "Rotate", Fixed = true },
            new Entry { Action = "Drop", Label = "Drop" },
            new Entry { Action = "Loupe", Label = "Loupe" },
            new Entry { Action = "Sprint", Label = "Sprint" },
            new Entry { Action = "Tablet", Label = "Tablet" },
            new Entry { Action = "Build", Label = "Build mode" },
            new Entry { Action = "Inventory", Label = "Inventory" },
            new Entry { Action = "Pause", Label = "Pause / menu" },
        };

        public static InputActionAsset Asset => InputSystem.actions;

        public static InputAction Find(string action)
        {
            var asset = Asset;
            if (asset == null) return null;
            var map = asset.FindActionMap("Player", false);
            return map?.FindAction(action, false);
        }

        /// <summary>The binding index this scheme uses for the action, or -1 when it has none.</summary>
        public static int BindingIndex(InputAction a, string scheme)
        {
            if (a == null) return -1;
            for (int i = 0; i < a.bindings.Count; i++)
            {
                var b = a.bindings[i];
                if (b.isComposite || b.isPartOfComposite) continue;
                if (b.groups != null && b.groups.Contains(scheme)) return i;
            }
            return -1;
        }

        /// <summary>What the player would press, read off the asset (overrides included).</summary>
        public static string Display(string action, string scheme)
        {
            var a = Find(action);
            if (a == null) return "";
            int i = BindingIndex(a, scheme);
            string s;
            if (i >= 0)
            {
                s = a.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontUseShortDisplayNames);
            }
            else
            {
                // a composite (Move / Look / Rotate): let the Input System describe the whole group for that scheme
                s = a.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontUseShortDisplayNames, scheme);
            }
            return Shorten(s);
        }

        /// <summary>The Input System's names are correct and long. A key cap has room for three characters.</summary>
        private static string Shorten(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            // longest first: "Left Stick" would otherwise eat "Left Stick Press"
            s = s.Replace("Left Button", "LMB").Replace("Right Button", "RMB").Replace("Middle Button", "MMB")
                 .Replace("Left Stick Press", "L3").Replace("Right Stick Press", "R3")
                 .Replace("Left Shoulder", "LB").Replace("Right Shoulder", "RB")
                 .Replace("Left Trigger", "LT").Replace("Right Trigger", "RT")
                 .Replace("Left Stick", "L-Stick").Replace("Right Stick", "R-Stick")
                 .Replace("Button South", "A").Replace("Button East", "B").Replace("Button West", "X").Replace("Button North", "Y")
                 .Replace("D-Pad/Up", "D-Up").Replace("D-Pad/Down", "D-Down").Replace("D-Pad/Left", "D-Left").Replace("D-Pad/Right", "D-Right")
                 .Replace("D-Pad Up", "D-Up").Replace("D-Pad Down", "D-Down").Replace("D-Pad Left", "D-Left").Replace("D-Pad Right", "D-Right")
                 .Replace("Escape", "Esc").Replace("Left Shift", "Shift").Replace("Right Shift", "Shift")
                 .Replace("Left Ctrl", "Ctrl").Replace("Right Ctrl", "Ctrl")
                 .Replace("Left Arrow", "←").Replace("Right Arrow", "→").Replace("Up Arrow", "↑").Replace("Down Arrow", "↓")
                 .Replace("Delta", "Mouse").Replace("Position", "Mouse").Replace("Scroll", "Wheel");
            // A composite lists every part: "W | A | S | D | Up | Left | Down | Right". The longest run of
            // single-character parts is the set worth showing ("WASD"); otherwise take at most three, or a key cap
            // turns into a sentence.
            // the Input System joins composite parts with "/" and alternatives with " | "
            var parts = s.Split(new[] { " | ", "/" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                int bestStart = -1, bestLen = 0, runStart = -1, runLen = 0;
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].Trim().Length == 1) { if (runLen == 0) runStart = i; runLen++; }
                    else runLen = 0;
                    if (runLen > bestLen) { bestLen = runLen; bestStart = runStart; }
                }
                if (bestLen >= 3)
                {
                    var run = new System.Text.StringBuilder();
                    for (int i = bestStart; i < bestStart + bestLen; i++) run.Append(parts[i].Trim());
                    s = run.ToString();
                }
                else s = string.Join("/", parts, 0, Mathf.Min(parts.Length, 3));
            }
            return s.Trim();
        }

        // ---- rebinding ---------------------------------------------------------------------------

        private static InputActionRebindingExtensions.RebindingOperation _rebinding;
        public static bool Listening => _rebinding != null;
        /// <summary>Set when a rebind takes a control another action already had, naming the action that lost it.</summary>
        public static string LastConflict { get; private set; }

        /// <summary>
        /// Listen for the next control on that scheme and bind it. The gameplay map is disabled while listening, or
        /// the very press being captured also fires the action it is replacing.
        /// </summary>
        public static void StartRebind(string action, string scheme, Action<bool> done)
        {
            Cancel();
            var a = Find(action);
            if (a == null) { done?.Invoke(false); return; }
            int i = BindingIndex(a, scheme);
            if (i < 0) { done?.Invoke(false); return; }
            LastConflict = null;
            bool wasEnabled = a.actionMap.enabled;
            a.actionMap.Disable();
            _rebinding = a.PerformInteractiveRebinding(i)
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Pointer>/position")
                .WithCancelingThrough(scheme == GamepadScheme ? "<Gamepad>/start" : "<Keyboard>/escape")
                .OnMatchWaitForAnother(0.06f);
            if (scheme == GamepadScheme) _rebinding.WithExpectedControlType(a.expectedControlType).WithControlsHavingToMatchPath("<Gamepad>");
            else _rebinding.WithControlsExcluding("<Gamepad>");
            _rebinding.OnComplete(op =>
            {
                op.Dispose(); _rebinding = null;
                if (wasEnabled) a.actionMap.Enable();
                ResolveConflicts(a, scheme, i);
                Save();
                Changed?.Invoke();
                done?.Invoke(true);
            });
            _rebinding.OnCancel(op =>
            {
                op.Dispose(); _rebinding = null;
                if (wasEnabled) a.actionMap.Enable();
                done?.Invoke(false);
            });
            _rebinding.Start();
        }

        public static void Cancel()
        {
            if (_rebinding == null) return;
            _rebinding.Cancel();
            _rebinding.Dispose();
            _rebinding = null;
        }

        /// <summary>
        /// One control, one action. If the new binding is already some other action's, that other action gives it up
        /// and says so — silently sharing a key is the worst of the three options.
        /// </summary>
        private static void ResolveConflicts(InputAction taken, string scheme, int index)
        {
            string path = taken.bindings[index].effectivePath;
            if (string.IsNullOrEmpty(path)) return;
            var map = taken.actionMap;
            foreach (var other in map.actions)
            {
                if (other == taken) continue;
                int j = BindingIndex(other, scheme);
                if (j < 0 || other.bindings[j].effectivePath != path) continue;
                other.ApplyBindingOverride(j, new InputBinding { overridePath = "" });
                LastConflict = other.name;
            }
        }

        public static void ResetAll()
        {
            var asset = Asset;
            if (asset == null) return;
            asset.RemoveAllBindingOverrides();
            LastConflict = null;
            Save();
            Changed?.Invoke();
        }

        public static void Reset(string action)
        {
            var a = Find(action);
            if (a == null) return;
            a.RemoveAllBindingOverrides();
            Save();
            Changed?.Invoke();
        }

        // ---- persistence -------------------------------------------------------------------------

        public static void Save()
        {
            var asset = Asset;
            if (asset == null) return;
            GameSettings.Current.Bindings = asset.SaveBindingOverridesAsJson();
            GameSettings.SaveCurrent();
        }

        /// <summary>Applied once the settings file is read, before anything asks for a glyph.</summary>
        public static void Apply()
        {
            var asset = Asset;
            if (asset == null) return;
            string json = GameSettings.Current.Bindings;
            asset.RemoveAllBindingOverrides();
            if (!string.IsNullOrEmpty(json)) asset.LoadBindingOverridesFromJson(json);
            Changed?.Invoke();
        }
    }
}
