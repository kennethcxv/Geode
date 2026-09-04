using UnityEngine;
using UnityEngine.UIElements;

namespace GeodeEmpire.UI
{
    /// <summary>Small helpers for building consistent UI Toolkit trees in code.</summary>
    public static class UiKit
    {
        public static Label Label(VisualElement parent, string text, params string[] classes)
        {
            var l = new Label(text);
            foreach (var c in classes) if (!string.IsNullOrEmpty(c)) l.AddToClassList(c);
            l.pickingMode = PickingMode.Ignore;
            parent?.Add(l);
            return l;
        }

        public static VisualElement Box(VisualElement parent, params string[] classes)
        {
            var v = new VisualElement();
            foreach (var c in classes) if (!string.IsNullOrEmpty(c)) v.AddToClassList(c);
            parent?.Add(v);
            return v;
        }

        public static Button Button(VisualElement parent, string text, System.Action onClick, params string[] classes)
        {
            var b = new Button(onClick) { text = text };
            b.AddToClassList("btn");
            foreach (var c in classes) if (!string.IsNullOrEmpty(c)) b.AddToClassList(c);
            parent?.Add(b);
            return b;
        }

        /// <summary>Dev/test: describe the element that currently has keyboard/controller focus on a panel.</summary>
        public static string FocusedText(VisualElement anyElementOnPanel)
        {
            var f = anyElementOnPanel?.panel?.focusController?.focusedElement as VisualElement;
            if (f == null) return "<none>";
            if (f is Button b) return "Button:" + b.text;
            if (f is Slider s) return "Slider:" + s.value.ToString("F2");
            if (f is Toggle t) return "Toggle:" + t.value;
            if (f is DropdownField d) return "Dropdown:" + d.value;
            return f.GetType().Name + (string.IsNullOrEmpty(f.name) ? "" : ":" + f.name);
        }

        /// <summary>A key cap and what it does, the pairing the whole HUD uses for controls.</summary>
        public static VisualElement KeyHint(VisualElement parent, string key, string label)
        {
            var row = Box(parent, "keyhint");
            Label(row, key, "keycap");
            if (!string.IsNullOrEmpty(label)) Label(row, label, "keyhint-label");
            return row;
        }

        /// <summary>A key/value line inside a detail card.</summary>
        public static Label Kv(VisualElement parent, string key, string value, params string[] valueClasses)
        {
            var row = Box(parent, "kv");
            Label(row, key, "kv-key");
            var v = Label(row, value, "kv-val");
            foreach (var c in valueClasses) if (!string.IsNullOrEmpty(c)) v.AddToClassList(c);
            return v;
        }

        public static VisualElement Rule(VisualElement parent) => Box(parent, "rule");

        /// <summary>Rarity chip. Tier runs 0 common .. 4 legendary.</summary>
        public static Label Rarity(VisualElement parent, int tier, string text = null)
        {
            string[] names = { "COMMON", "UNCOMMON", "RARE", "EPIC", "LEGENDARY" };
            string[] classes = { "rarity-common", "rarity-uncommon", "rarity-rare", "rarity-epic", "rarity-legendary" };
            int i = Mathf.Clamp(tier, 0, 4);
            var l = Label(parent, text ?? names[i], "rarity", classes[i]);
            return l;
        }

        public static string Money(float v)
        {
            return v < 0 ? "-$" + Mathf.Abs(v).ToString("N0") : "$" + v.ToString("N0");
        }
    }
}
