using System.Collections.Generic;
using System.Text;
using UnityEngine;
using GeodeEmpire.Specimens;
using GeodeEmpire.Workshop;

namespace GeodeEmpire.Core
{
    /// <summary>
    /// Dev/QA: measures real collider interpenetration between staged objects (specimens, crates, lids) and whatever
    /// they touch, with Physics.ComputePenetration. Visual inspection misses a rock sunk a centimetre into a tray;
    /// this does not. Call Report() from a scripted playtest or an eval and read the summary.
    /// </summary>
    public static class CollisionAudit
    {
        public struct Overlap
        {
            public string A, B;
            public float Depth;
            public override string ToString() => $"{A} <> {B}: {Depth * 1000f:F1} mm";
        }

        /// <summary>Penetrations deeper than this are reported; shallower ones are contact noise.</summary>
        public const float Tolerance = 0.004f;

        private static readonly Collider[] _hits = new Collider[64];

        public static List<Overlap> Measure()
        {
            var results = new List<Overlap>();
            var seen = new HashSet<(Collider, Collider)>();
            var subjects = new List<Collider>();
            foreach (var e in Object.FindObjectsByType<SpecimenEntity>(FindObjectsInactive.Exclude))
                foreach (var c in e.GetComponentsInChildren<Collider>()) if (c.enabled && !c.isTrigger) subjects.Add(c);
            foreach (var cr in Object.FindObjectsByType<CrateEntity>(FindObjectsInactive.Exclude))
                foreach (var c in cr.GetComponentsInChildren<Collider>()) if (c.enabled && !c.isTrigger) subjects.Add(c);

            foreach (var a in subjects)
            {
                var b = a.bounds;
                int n = Physics.OverlapBoxNonAlloc(b.center, b.extents + Vector3.one * 0.01f, _hits, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    var o = _hits[i];
                    if (o == a || o.isTrigger || !o.enabled) continue;
                    if (o.transform.IsChildOf(a.transform.root) && a.transform.IsChildOf(o.transform.root) && SameEntity(a, o)) continue;
                    if (seen.Contains((o, a))) continue;
                    seen.Add((a, o));
                    if (!Physics.ComputePenetration(a, a.transform.position, a.transform.rotation, o, o.transform.position, o.transform.rotation, out _, out float depth)) continue;
                    if (depth > Tolerance) results.Add(new Overlap { A = Name(a), B = Name(o), Depth = depth });
                }
            }
            results.Sort((x, y) => y.Depth.CompareTo(x.Depth));
            return results;
        }

        private static bool SameEntity(Collider a, Collider b)
        {
            var ea = a.GetComponentInParent<SpecimenEntity>(); var eb = b.GetComponentInParent<SpecimenEntity>();
            if (ea != null && ea == eb) return true;
            var ca = a.GetComponentInParent<CrateEntity>(); var cb = b.GetComponentInParent<CrateEntity>();
            return ca != null && ca == cb;
        }

        private static string Name(Collider c)
        {
            var e = c.GetComponentInParent<SpecimenEntity>();
            if (e != null) return "rock " + e.Id + (e.Record.IsOpened ? "(open)" : "");
            var cr = c.GetComponentInParent<CrateEntity>();
            if (cr != null) return "crate " + cr.Record.Id + (c.transform.name == "Lid" ? " lid" : "");
            return c.transform.root.name + "/" + c.name;
        }

        /// <summary>Human-readable summary: worst overlaps first. Empty list means nothing interpenetrates beyond tolerance.</summary>
        public static string Report(string tag = "")
        {
            var list = Measure();
            var sb = new StringBuilder();
            sb.Append($"[CollisionAudit{(string.IsNullOrEmpty(tag) ? "" : " " + tag)}] overlaps>{Tolerance * 1000f:F0}mm: {list.Count}");
            int shown = 0;
            foreach (var o in list) { if (shown++ >= 12) { sb.Append("\n  ..."); break; } sb.Append("\n  ").Append(o); }
            return sb.ToString();
        }
    }
}
