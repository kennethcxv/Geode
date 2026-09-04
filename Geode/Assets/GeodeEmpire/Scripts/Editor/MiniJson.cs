using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GeodeEmpire.EditorTools
{
    /// <summary>
    /// A small recursive-descent JSON reader for editor-time asset manifests. JsonUtility cannot express the
    /// dictionaries the Blender kit manifest uses, and this keeps the import step free of a package dependency.
    /// Values are string, double, bool, null, List&lt;object&gt; or Dictionary&lt;string, object&gt;.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string text) { int i = 0; var v = ParseValue(text, ref i); return v; }

        private static void Skip(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

        private static object ParseValue(string s, ref int i)
        {
            Skip(s, ref i);
            if (i >= s.Length) return null;
            char c = s[i];
            if (c == '{') return ParseObject(s, ref i);
            if (c == '[') return ParseArray(s, ref i);
            if (c == '"') return ParseString(s, ref i);
            if (s.Length - i >= 4 && s.Substring(i, 4) == "true") { i += 4; return true; }
            if (s.Length - i >= 5 && s.Substring(i, 5) == "false") { i += 5; return false; }
            if (s.Length - i >= 4 && s.Substring(i, 4) == "null") { i += 4; return null; }
            int start = i;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '-' || s[i] == '+' || s[i] == '.' || s[i] == 'e' || s[i] == 'E')) i++;
            return double.Parse(s.Substring(start, i - start), CultureInfo.InvariantCulture);
        }

        private static Dictionary<string, object> ParseObject(string s, ref int i)
        {
            var d = new Dictionary<string, object>();
            i++;   // {
            while (true)
            {
                Skip(s, ref i);
                if (i >= s.Length) break;
                if (s[i] == '}') { i++; break; }
                if (s[i] == ',') { i++; continue; }
                string key = ParseString(s, ref i);
                Skip(s, ref i);
                if (i < s.Length && s[i] == ':') i++;
                d[key] = ParseValue(s, ref i);
            }
            return d;
        }

        private static List<object> ParseArray(string s, ref int i)
        {
            var list = new List<object>();
            i++;   // [
            while (true)
            {
                Skip(s, ref i);
                if (i >= s.Length) break;
                if (s[i] == ']') { i++; break; }
                if (s[i] == ',') { i++; continue; }
                list.Add(ParseValue(s, ref i));
            }
            return list;
        }

        private static string ParseString(string s, ref int i)
        {
            var sb = new StringBuilder();
            i++;   // "
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') break;
                if (c != '\\') { sb.Append(c); continue; }
                char e = s[i++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u': sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber)); i += 4; break;
                    default: sb.Append(e); break;
                }
            }
            return sb.ToString();
        }

        // ---- typed accessors -------------------------------------------------------------------------------
        public static Dictionary<string, object> Obj(object v) => v as Dictionary<string, object>;
        public static List<object> Arr(object v) => v as List<object>;
        public static string Str(object v) => v as string;
        public static float Num(object v, float fallback = 0f) => v is double d ? (float)d : fallback;
        public static int Int(object v, int fallback = 0) => v is double d ? (int)d : fallback;
        public static bool Bool(object v, bool fallback = false) => v is bool b ? b : fallback;

        public static object Get(Dictionary<string, object> d, string key) => d != null && d.TryGetValue(key, out var v) ? v : null;
        public static Dictionary<string, object> GetObj(Dictionary<string, object> d, string key) => Obj(Get(d, key));
        public static List<object> GetArr(Dictionary<string, object> d, string key) => Arr(Get(d, key));
        public static string GetStr(Dictionary<string, object> d, string key, string fallback = null) => Str(Get(d, key)) ?? fallback;
        public static float GetNum(Dictionary<string, object> d, string key, float fallback = 0f) => Num(Get(d, key), fallback);
        public static int GetInt(Dictionary<string, object> d, string key, int fallback = 0) => Int(Get(d, key), fallback);
    }
}
