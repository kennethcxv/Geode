using System.Collections.Generic;
using UnityEngine;

namespace GeodeEmpire.UI
{
    /// <summary>
    /// World-space text built from the font atlas by hand: one quad per glyph on a MeshFilter/MeshRenderer, readable
    /// from the transform's -Z side like a TextMesh. Replaces TextMesh, which in this project drew every label twice
    /// (a second, larger, displaced copy). Static fonts only: the atlas must not rebuild under a material asset.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class WorldLabel : MonoBehaviour
    {
        public Font Font;
        public float LineHeight = 0.03f;      // metres per line
        public float LineSpacing = 1f;
        public Color Color = Color.white;
        public TextAnchor Anchor = TextAnchor.MiddleCenter;

        [SerializeField] private string _text = "";
        private Mesh _mesh;                    // never saved: rebuilt from _text on load (a scene-embedded mesh corrupts player builds)
        private static readonly List<Vector3> Verts = new List<Vector3>(256);
        private static readonly List<Vector2> Uvs = new List<Vector2>(256);
        private static readonly List<Color32> Cols = new List<Color32>(256);
        private static readonly List<int> Tris = new List<int>(384);

        public string Text
        {
            get => _text;
            set { value ??= ""; if (_text == value) return; _text = value; Rebuild(); }
        }

        public static WorldLabel Create(Transform parent, Font font, Material material, float lineHeight, Color color, string name = "Label")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material != null ? material : (font != null ? font.material : null);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            var wl = go.AddComponent<WorldLabel>();
            wl.Font = font;
            wl.LineHeight = lineHeight;
            wl.Color = color;
            return wl;
        }

        public void SetColor(Color c) { Color = c; Rebuild(); }

        private void Awake() { if (_mesh == null) Rebuild(); }
        private void OnValidate() { if (_mesh != null) Rebuild(); }

        private void OnDestroy()
        {
            if (_mesh == null) return;
            if (Application.isPlaying) Destroy(_mesh); else DestroyImmediate(_mesh);
        }

        /// <summary>Width in metres of the widest line, from the font metrics alone (usable in the Editor, no mesh needed).</summary>
        public float MeasureWidth()
        {
            if (Font == null || string.IsNullOrEmpty(_text)) return 0f;
            float k = LineHeight / Mathf.Max(1f, Font.lineHeight);
            float best = 0f;
            foreach (var line in _text.Split('\n'))
            {
                float w = 0f;
                for (int i = 0; i < line.Length; i++) if (Font.GetCharacterInfo(line[i], out var ci)) w += ci.advance * k;
                best = Mathf.Max(best, w);
            }
            return best;
        }

        public void Rebuild()
        {
            // meshes exist only while playing: a mesh referenced from a saved scene corrupts the player build
            if (!Application.isPlaying) return;
            if (_mesh == null)
            {
                _mesh = new Mesh { name = "WorldLabel", hideFlags = HideFlags.HideAndDontSave };
                _mesh.MarkDynamic();
                GetComponent<MeshFilter>().sharedMesh = _mesh;
            }
            Verts.Clear(); Uvs.Clear(); Cols.Clear(); Tris.Clear();
            if (Font == null || string.IsNullOrEmpty(_text)) { _mesh.Clear(); return; }
            float px = Mathf.Max(1f, Font.lineHeight);        // atlas pixels per line
            float k = LineHeight / px;                          // metres per atlas pixel
            string[] lines = _text.Split('\n');
            float step = LineHeight * LineSpacing;
            float total = step * lines.Length;
            float top = Anchor == TextAnchor.UpperCenter || Anchor == TextAnchor.UpperLeft || Anchor == TextAnchor.UpperRight ? 0f
                      : Anchor == TextAnchor.LowerCenter || Anchor == TextAnchor.LowerLeft || Anchor == TextAnchor.LowerRight ? total : total * 0.5f;
            var col = (Color32)Color;
            for (int li = 0; li < lines.Length; li++)
            {
                string line = lines[li];
                float width = 0f;
                for (int i = 0; i < line.Length; i++) if (Font.GetCharacterInfo(line[i], out var ci)) width += ci.advance * k;
                float x = Anchor == TextAnchor.UpperLeft || Anchor == TextAnchor.MiddleLeft || Anchor == TextAnchor.LowerLeft ? 0f
                        : Anchor == TextAnchor.UpperRight || Anchor == TextAnchor.MiddleRight || Anchor == TextAnchor.LowerRight ? -width : -width * 0.5f;
                float baseline = top - step * li - LineHeight * 0.8f;   // ascent ≈ 80% of the line
                for (int i = 0; i < line.Length; i++)
                {
                    if (!Font.GetCharacterInfo(line[i], out var ci)) continue;
                    float x0 = x + ci.minX * k, x1 = x + ci.maxX * k;
                    float y0 = baseline + ci.minY * k, y1 = baseline + ci.maxY * k;
                    int b = Verts.Count;
                    Verts.Add(new Vector3(x0, y0, 0f)); Verts.Add(new Vector3(x1, y0, 0f)); Verts.Add(new Vector3(x0, y1, 0f)); Verts.Add(new Vector3(x1, y1, 0f));
                    Uvs.Add(ci.uvBottomLeft); Uvs.Add(ci.uvBottomRight); Uvs.Add(ci.uvTopLeft); Uvs.Add(ci.uvTopRight);
                    Cols.Add(col); Cols.Add(col); Cols.Add(col); Cols.Add(col);
                    // front faces -Z (the side a camera at -Z sees), same convention as Unity's quad and TextMesh
                    Tris.Add(b); Tris.Add(b + 3); Tris.Add(b + 1);
                    Tris.Add(b + 3); Tris.Add(b); Tris.Add(b + 2);
                    x += ci.advance * k;
                }
            }
            _mesh.Clear();
            _mesh.SetVertices(Verts);
            _mesh.SetUVs(0, Uvs);
            _mesh.SetColors(Cols);
            _mesh.SetTriangles(Tris, 0);
            _mesh.RecalculateBounds();
        }
    }
}
