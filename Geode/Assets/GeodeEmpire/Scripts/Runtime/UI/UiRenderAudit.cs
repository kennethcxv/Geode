using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace GeodeEmpire.UI
{
    /// <summary>
    /// V6 §66: measure the interface rather than look at it. Every visible element on every live panel is walked
    /// and judged against the faults §66 names — clipped, overlapping, truncated, too small to read, too small to
    /// hit, focus lost, notifications stacked. The panel is laid out into a render texture at the target
    /// resolution first, so 1920, 2560 and 3840 are real layouts and not a proxy for them.
    ///
    /// §66 also asks that the instrument be proved able to fail, so <see cref="PlantNegatives"/> deliberately
    /// breaks four things and the harness checks each one is caught.
    /// </summary>
    public static class UiRenderAudit
    {
        public sealed class Finding
        {
            public string Kind, Where, Detail;
            public override string ToString() => $"{Kind}: {Where} — {Detail}";
        }

        /// <summary>Below this a label is not readable at arm's length on a 27-inch monitor.</summary>
        public const float MinFontPx = 11f;
        /// <summary>A control smaller than this is a dart-throw with a mouse and impossible on a pad.</summary>
        public const float MinControlPx = 18f;
        /// <summary>Notifications are stacked when this many overlap.</summary>
        public const int MaxStackedNotes = 4;

        /// <param name="screenWidth">The real width the panel is drawn at, so a font size can be judged in the
        /// pixels the player actually sees rather than in layout units.</param>
        public static List<Finding> Run(float screenWidth)
        {
            var results = new List<Finding>();
            var main = Resources.Load<PanelSettings>("UI/GeodePanelSettings");
            foreach (var doc in Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                // the checkout's POS monitor and customer display are world-space screens on their own panels:
                // they are 20 cm of glass on a counter, not interface, and screen-pixel rules do not apply
                if (main != null && doc.panelSettings != main) continue;
                var root = doc.rootVisualElement;
                if (root?.panel == null) continue;
                var panelRect = root.panel.visualTree.layout;
                if (panelRect.width < 1f || panelRect.height < 1f) continue;
                var texts = new List<VisualElement>();
                Walk(root, doc.name, panelRect, panelRect, results, texts, screenWidth);
                Overlaps(texts, doc.name, results);
                Focus(root, doc.name, results);
            }
            return results;
        }

        /// <summary>Does this element cut its children off? A scroll viewport does; so does an explicit hidden overflow.</summary>
        private static bool Clips(VisualElement e)
        {
            if (e is ScrollView) return true;
            if (e.name == "unity-content-viewport") return true;
            // resolvedStyle exposes no overflow, so read the inline/USS value the element was given
            var v = e.style.overflow;
            return v.keyword == StyleKeyword.Undefined && v.value == Overflow.Hidden;
        }

        private static bool Visible(VisualElement e)
        {
            if (e.resolvedStyle.display == DisplayStyle.None || e.resolvedStyle.visibility == Visibility.Hidden) return false;
            if (e.resolvedStyle.opacity < 0.05f) return false;
            var b = e.worldBound;
            return b.width > 0.5f && b.height > 0.5f;
        }

        /// <param name="clip">The rect the element is actually allowed to draw in: the panel, or the nearest
        /// scrolling viewport above it. Content scrolled out of a list is not a layout fault, and judging it
        /// against the screen reported seventy of them on one healthy page.</param>
        private static void Walk(VisualElement e, string doc, Rect panel, Rect clip, List<Finding> results, List<VisualElement> texts, float screenWidth)
        {
            if (!Visible(e)) return;
            var b = e.worldBound;
            bool clipped = clip != panel;

            if (clipped)
            {
                // outside its own viewport: scrolled away, not broken, and nothing below it is on screen either
                if (!b.Overlaps(clip)) return;
            }
            else if (b.xMax < -0.5f || b.yMax < -0.5f || b.xMin > panel.width + 0.5f || b.yMin > panel.height + 0.5f)
                results.Add(new Finding { Kind = "off-screen", Where = Path(e, doc), Detail = $"at {b.x:F0},{b.y:F0} outside {panel.width:F0}x{panel.height:F0}" });
            else if (b.xMin < -1f || b.yMin < -1f || b.xMax > panel.width + 1f || b.yMax > panel.height + 1f)
                results.Add(new Finding { Kind = "clipped", Where = Path(e, doc), Detail = $"{b.xMin:F0}..{b.xMax:F0} x {b.yMin:F0}..{b.yMax:F0} against {panel.width:F0}x{panel.height:F0}" });

            if (e is TextElement t && !string.IsNullOrEmpty(t.text))
            {
                float size = e.resolvedStyle.fontSize;
                // the panel scales to the reference resolution, so a physical pixel size is what the player sees
                float physical = size * (panel.width > 1f && screenWidth > 1f ? screenWidth / panel.width : 1f);
                if (physical < MinFontPx)
                    results.Add(new Finding { Kind = "unreadable", Where = Path(e, doc), Detail = $"{physical:F1} px on screen ('{Clip(t.text)}')" });
                var wanted = t.MeasureTextSize(t.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined);
                bool wraps = e.resolvedStyle.whiteSpace == WhiteSpace.Normal;
                if (!wraps && wanted.x > b.width + 1.5f && b.width > 1f && b.width > 8f)
                    results.Add(new Finding { Kind = "truncated", Where = Path(e, doc), Detail = $"needs {wanted.x:F0} px, has {b.width:F0} ('{Clip(t.text)}')" });
                if (wraps && wanted.y > b.height + 1.5f && b.height > 1f && wanted.x <= b.width + 1.5f)
                    results.Add(new Finding { Kind = "truncated", Where = Path(e, doc), Detail = $"needs {wanted.y:F0} px tall, has {b.height:F0}" });
                texts.Add(e);
            }

            // anything the player is meant to click or focus has to be big enough to hit
            if ((e is Button || e is Toggle || e is Slider) && (b.width < MinControlPx || b.height < MinControlPx))
                results.Add(new Finding { Kind = "tiny-control", Where = Path(e, doc), Detail = $"{b.width:F0}x{b.height:F0} px" });

            // a scrolling viewport becomes the clip rect for everything under it
            var childClip = clip;
            if (Clips(e))
                childClip = clip == panel ? b : Rect.MinMaxRect(Mathf.Max(clip.xMin, b.xMin), Mathf.Max(clip.yMin, b.yMin), Mathf.Min(clip.xMax, b.xMax), Mathf.Min(clip.yMax, b.yMax));
            for (int i = 0; i < e.childCount; i++) Walk(e[i], doc, panel, childClip, results, texts, screenWidth);
        }

        /// <summary>Two pieces of text on top of each other, from different cards: one of them cannot be read.</summary>
        private static void Overlaps(List<VisualElement> texts, string doc, List<Finding> results)
        {
            int notes = 0;
            for (int i = 0; i < texts.Count; i++)
            {
                var a = texts[i];
                if (a.ClassListContains("notify") || (a.parent != null && a.parent.ClassListContains("notify"))) notes++;
                for (int j = i + 1; j < texts.Count; j++)
                {
                    var b = texts[j];
                    if (IsAncestor(a, b) || IsAncestor(b, a)) continue;
                    if (Card(a) == Card(b)) continue;              // one card laying its own rows out is its business
                    var ra = a.worldBound; var rb = b.worldBound;
                    if (!ra.Overlaps(rb)) continue;
                    var o = Rect.MinMaxRect(Mathf.Max(ra.xMin, rb.xMin), Mathf.Max(ra.yMin, rb.yMin), Mathf.Min(ra.xMax, rb.xMax), Mathf.Min(ra.yMax, rb.yMax));
                    float share = o.width * o.height / Mathf.Max(1f, Mathf.Min(ra.width * ra.height, rb.width * rb.height));
                    if (share < 0.25f) continue;
                    results.Add(new Finding { Kind = "overlap", Where = Path(a, doc), Detail = $"{share * 100f:F0}% under {Path(b, doc)}" });
                }
            }
            if (notes > MaxStackedNotes)
                results.Add(new Finding { Kind = "stacked-notifications", Where = doc, Detail = notes + " notification lines at once" });
        }

        /// <summary>A menu that has taken the screen must leave the focus somewhere the player can see.</summary>
        private static void Focus(VisualElement root, string doc, List<Finding> results)
        {
            var fc = root.panel?.focusController;
            var focused = fc?.focusedElement as VisualElement;
            bool menuOpen = false;
            root.Query<VisualElement>().ForEach(e => { if (Visible(e) && (e.ClassListContains("panel-dim") || e.ClassListContains("settings"))) menuOpen = true; });
            if (!menuOpen) return;
            if (focused == null) results.Add(new Finding { Kind = "focus-lost", Where = doc, Detail = "a menu is open with nothing focused" });
            else if (!Visible(focused)) results.Add(new Finding { Kind = "focus-lost", Where = doc, Detail = "focus is on a hidden element (" + Path(focused, doc) + ")" });
        }

        private static bool IsAncestor(VisualElement a, VisualElement b)
        {
            for (var p = b.parent; p != null; p = p.parent) if (p == a) return true;
            return false;
        }

        /// <summary>The card an element belongs to; two rows of one card are allowed to share space, two cards are not.</summary>
        private static VisualElement Card(VisualElement e)
        {
            for (var p = e; p != null; p = p.parent)
                if (p.ClassListContains("card") || p.ClassListContains("panel") || p.ClassListContains("settings") || p.ClassListContains("keybar")) return p;
            return null;
        }

        private static string Clip(string s) => s.Length <= 24 ? s : s.Substring(0, 23) + "…";

        private static string Path(VisualElement e, string doc)
        {
            var sb = new StringBuilder();
            var stack = new List<string>();
            for (var p = e; p != null && stack.Count < 4; p = p.parent)
            {
                string n = !string.IsNullOrEmpty(p.name) ? p.name : p.GetClasses() != null ? First(p) : p.GetType().Name;
                stack.Add(n);
            }
            sb.Append(doc);
            for (int i = stack.Count - 1; i >= 0; i--) sb.Append('/').Append(stack[i]);
            return sb.ToString();
        }

        private static string First(VisualElement e)
        {
            foreach (var c in e.GetClasses()) return "." + c;
            return e.GetType().Name;
        }

        // ---- negative controls (§66: prove the instrument can fail) --------------------------------

        private static readonly List<VisualElement> _planted = new List<VisualElement>();

        /// <summary>Break four things on purpose. Each one must show up as its own kind of finding.</summary>
        public static void PlantNegatives()
        {
            ClearNegatives();
            var hud = HudController.Instance;
            if (hud == null) return;
            var root = hud.GetComponent<UIDocument>().rootVisualElement;

            var tiny = new Label("planted: text far too small to read");
            tiny.name = "PlantedTiny";
            tiny.style.position = Position.Absolute; tiny.style.left = 300; tiny.style.top = 300; tiny.style.fontSize = 4;
            root.Add(tiny); _planted.Add(tiny);

            var clipped = new Label("planted: hanging off the left edge");
            clipped.name = "PlantedClipped";
            clipped.style.position = Position.Absolute; clipped.style.left = -160; clipped.style.top = 340; clipped.style.fontSize = 18;
            root.Add(clipped); _planted.Add(clipped);

            var narrow = new Label("planted: a label far longer than the box it was given");
            narrow.name = "PlantedTruncated";
            narrow.style.position = Position.Absolute; narrow.style.left = 300; narrow.style.top = 380;
            narrow.style.width = 40; narrow.style.overflow = Overflow.Hidden; narrow.style.fontSize = 18;
            root.Add(narrow); _planted.Add(narrow);

            var button = new Button { text = "x", name = "PlantedTinyButton" };
            button.style.position = Position.Absolute; button.style.left = 300; button.style.top = 420;
            button.style.width = 8; button.style.height = 8; button.style.paddingLeft = 0; button.style.paddingRight = 0;
            button.style.paddingTop = 0; button.style.paddingBottom = 0; button.style.marginLeft = 0; button.style.marginTop = 0;
            root.Add(button); _planted.Add(button);
        }

        public static void ClearNegatives()
        {
            foreach (var e in _planted) e?.RemoveFromHierarchy();
            _planted.Clear();
        }
    }
}
