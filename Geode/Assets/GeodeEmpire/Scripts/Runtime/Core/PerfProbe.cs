using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace GeodeEmpire.Core
{
    /// <summary>
    /// A stopwatch you can leave in the code. §10.1 forbids guessing at the crack hitch, so the reveal path is
    /// marked up with named spans and the frames around it are recorded; a capture window is opened by whatever
    /// is being measured and read back as a report. Off by default: when no window is open a span costs one
    /// boolean test, so the marks can stay in the shipping path.
    /// </summary>
    public static class PerfProbe
    {
        /// <summary>A frame that takes longer than this is a hitch the player can feel (two 60 Hz frames).</summary>
        public const float HitchMs = 33f;

        public static bool Capturing { get; private set; }

        private struct Mark { public string Name; public double Ms; public int Frame; }

        private static readonly List<Mark> _marks = new List<Mark>(64);
        private static readonly List<float> _frames = new List<float>(600);
        private static readonly Stopwatch _clock = new Stopwatch();
        private static string _label = "";
        private static int _startFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Capturing = false; _marks.Clear(); _frames.Clear(); _clock.Reset(); _label = ""; }

        public static void Begin(string label)
        {
            _marks.Clear();
            _frames.Clear();
            _label = label;
            _startFrame = Time.frameCount;
            Capturing = true;
            _clock.Restart();
        }

        /// <summary>Called from a driver each frame while a window is open; records the real frame delta.</summary>
        public static void Frame(float unscaledDeltaMs)
        {
            if (!Capturing) return;
            if (_frames.Count < 600) _frames.Add(unscaledDeltaMs);
        }

        /// <summary>Wrap a phase: <c>using (PerfProbe.Span("mesh")) { ... }</c>. Free when not capturing.</summary>
        public static Span Measure(string name) => new Span(name);

        public readonly struct Span : IDisposable
        {
            private readonly string _name;
            private readonly long _t0;
            public Span(string name)
            {
                if (!Capturing) { _name = null; _t0 = 0; return; }
                _name = name;
                _t0 = _clock.ElapsedTicks;
            }
            public void Dispose()
            {
                if (_name == null) return;
                double ms = (_clock.ElapsedTicks - _t0) * 1000.0 / Stopwatch.Frequency;
                _marks.Add(new Mark { Name = _name, Ms = ms, Frame = Time.frameCount });
            }
        }

        /// <summary>Record a span whose cost was measured elsewhere.</summary>
        public static void Add(string name, double ms)
        {
            if (!Capturing) return;
            _marks.Add(new Mark { Name = name, Ms = ms, Frame = Time.frameCount });
        }

        public static string End()
        {
            if (!Capturing) return "(no capture)";
            Capturing = false;
            var sb = new StringBuilder();
            sb.Append("PERF ").Append(_label).Append('\n');
            // the spans, worst first, with the frame each landed on so a stack of them in one frame is visible
            var sorted = new List<Mark>(_marks);
            sorted.Sort((a, b) => b.Ms.CompareTo(a.Ms));
            double total = 0;
            foreach (var m in sorted) total += m.Ms;
            sb.Append("  spans (").Append(_marks.Count).Append(", ").Append(total.ToString("F1")).Append(" ms total)\n");
            for (int i = 0; i < sorted.Count && i < 22; i++)
                sb.Append("    ").Append(sorted[i].Ms.ToString("F2").PadLeft(8)).Append(" ms  f+")
                  .Append(sorted[i].Frame - _startFrame).Append("  ").Append(sorted[i].Name).Append('\n');
            // the frames, which is what the player actually feels
            if (_frames.Count > 0)
            {
                float worst = 0f, sum = 0f; int worstIdx = 0, hitches = 0;
                for (int i = 0; i < _frames.Count; i++)
                {
                    sum += _frames[i];
                    if (_frames[i] > worst) { worst = _frames[i]; worstIdx = i; }
                    if (_frames[i] > HitchMs) hitches++;
                }
                sb.Append("  frames ").Append(_frames.Count)
                  .Append("  mean ").Append((sum / _frames.Count).ToString("F1"))
                  .Append(" ms  worst ").Append(worst.ToString("F1")).Append(" ms @").Append(worstIdx)
                  .Append("  over ").Append(HitchMs.ToString("F0")).Append(" ms: ").Append(hitches).Append('\n');
                sb.Append("  worst-frame window:");
                for (int i = Mathf.Max(0, worstIdx - 3); i < Mathf.Min(_frames.Count, worstIdx + 4); i++)
                    sb.Append(' ').Append(_frames[i].ToString("F1"));
                sb.Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>The worst frame in the window so far, for an assertion that does not need the whole report.</summary>
        public static float WorstFrameMs()
        {
            float worst = 0f;
            for (int i = 0; i < _frames.Count; i++) if (_frames[i] > worst) worst = _frames[i];
            return worst;
        }

        public static int HitchCount(float overMs)
        {
            int n = 0;
            for (int i = 0; i < _frames.Count; i++) if (_frames[i] > overMs) n++;
            return n;
        }
    }
}
