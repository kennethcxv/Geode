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

        public static string Money(float v)
        {
            return v < 0 ? "-$" + Mathf.Abs(v).ToString("N0") : "$" + v.ToString("N0");
        }
    }
}
