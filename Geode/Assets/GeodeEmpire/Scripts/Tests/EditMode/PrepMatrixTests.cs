using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using GeodeEmpire.Specimens;
using GeodeEmpire.Workshop;

namespace GeodeEmpire.Tests
{
    /// <summary>
    /// V5 prep variation gate: the eight rock types the design names must not prepare identically. The checklist is
    /// derived the way the stations derive it (dirt, hull stance on the ring, cradle size, chip, rind, formation, fragility).
    /// </summary>
    public class PrepMatrixTests
    {
        private static string Checklist(SpecimenGeology g, out string kind)
        {
            var geo = GeodeMeshBuilder.Build(g);
            var bottom = geo.Bottom.ToColliderMesh("b", GeodeMeshBuilder.Longitudes, GeodeMeshBuilder.Latitudes);
            var top = geo.Top.ToColliderMesh("t", GeodeMeshBuilder.Longitudes, GeodeMeshBuilder.Latitudes);
            SpecimenEntity.SupportProfileOf(bottom, top, Quaternion.identity, Preparation.RingContactHeight, out float h, out float w);
            bool oversized = g.SizeClass == SizeClass.Oversized;
            float seat = Preparation.Stability(h, w, geo.MaxRadius, 0.085f, oversized, false);
            Object.DestroyImmediate(bottom); Object.DestroyImmediate(top);
            var steps = new List<string>();
            if (g.Dirt > 0.35f) steps.Add("wash");
            steps.Add("inspect");
            if (oversized) steps.Add("heavy cradle");
            if (seat < 0.8f) steps.Add("seat (" + Preparation.SeatWord(seat) + ")");
            if (Preparation.ChipSector(g) >= 0) steps.Add("start at the chip");
            if (g.Cavity == CavityArchetype.Nodule) steps.Add("saw: centre cut");
            else if (g.ShellThickness < 0.12f) steps.Add("thin shell: careful taps");
            else if (g.ShellThickness > 0.3f) steps.Add("thick shell: firm blows or saw");
            if (g.Family.Fragility > 0.6f) steps.Add("fragile crystals: light force");
            if (g.Texture == ExteriorTexture.Weathered) steps.Add("weathered rind: square the chisel");
            kind = $"{g.SizeClass} {g.Exterior} {g.Cavity} {g.Family.Name} shell={g.ShellThickness:F2} dirt={g.Dirt:F2} frag={g.Family.Fragility:F2}";
            return string.Join(" -> ", steps);
        }

        private static SpecimenGeology Find(System.Func<SpecimenGeology, bool> pick)
        {
            for (ulong seed = 1; seed < 20000; seed++) { var g = SpecimenGenerator.Generate(seed); if (pick(g)) return g; }
            return null;
        }

        [Test]
        public void EightRockTypesPrepareDifferently()
        {
            var picks = new (string name, System.Func<SpecimenGeology, bool> pick)[]
            {
                ("small round geode", g => g.SizeClass == SizeClass.Small && g.Exterior == ExteriorArchetype.Rounded && g.Dirt < 0.35f && g.Cavity != CavityArchetype.Nodule),
                ("medium angular rock", g => g.SizeClass == SizeClass.Medium && g.Exterior == ExteriorArchetype.Angular),
                ("large rough", g => g.SizeClass == SizeClass.Large && g.Exterior == ExteriorArchetype.Lumpy && g.Dirt > 0.35f),
                ("oversized rough", g => g.SizeClass == SizeClass.Oversized),
                ("fragile crystal-rich", g => g.Family.Fragility > 0.6f && g.CrystalDensity > 0.6f && g.Cavity != CavityArchetype.Nodule),
                ("banded nodule", g => g.Cavity == CavityArchetype.Nodule && g.Dirt > 0.35f),
                ("thin-shell geode", g => g.ShellThickness < 0.12f && g.Dirt < 0.35f && g.Family.Fragility <= 0.6f),
                ("thick-shell geode", g => g.ShellThickness > 0.3f && g.Cavity != CavityArchetype.Nodule && g.Family.Fragility <= 0.6f),
            };
            var sb = new StringBuilder("Prep matrix:\n");
            var lists = new HashSet<string>();
            foreach (var (name, pick) in picks)
            {
                var g = Find(pick);
                Assert.That(g, Is.Not.Null, name);
                string list = Checklist(g, out string kind);
                lists.Add(list);
                sb.AppendLine($"  {name,-22} [{g.SeedString}] {kind}\n      {list}");
            }
            Debug.Log(sb.ToString());
            Assert.That(lists.Count, Is.GreaterThanOrEqualTo(6), "prep sequences should differ across the eight rock types");
        }
    }
}
