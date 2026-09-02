using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Tests
{
    public class SpecimenGeneratorTests
    {
        private static string Fingerprint(SpecimenGeology g)
        {
            return JsonUtility.ToJson(g);
        }

        [Test]
        public void SameSeedProducesIdenticalGeology()
        {
            for (ulong seed = 1; seed < 40; seed += 3)
            {
                var a = SpecimenGenerator.Generate(seed);
                var b = SpecimenGenerator.Generate(seed);
                Assert.AreEqual(Fingerprint(a), Fingerprint(b), $"seed {seed}");
            }
        }

        [Test]
        public void SameSeedProducesIdenticalGeometry()
        {
            var g = SpecimenGenerator.Generate(4242);
            var geoA = GeodeMeshBuilder.Build(g);
            var geoB = GeodeMeshBuilder.Build(g);
            Assert.AreEqual(geoA.Bottom.Vertices.Length, geoB.Bottom.Vertices.Length);
            for (int i = 0; i < geoA.Bottom.Vertices.Length; i++) Assert.AreEqual(geoA.Bottom.Vertices[i], geoB.Bottom.Vertices[i]);
            Assert.AreEqual(geoA.Crystals.Count, geoB.Crystals.Count);
            for (int i = 0; i < geoA.Crystals.Count; i++)
            {
                Assert.AreEqual(geoA.Crystals[i].Position, geoB.Crystals[i].Position);
                Assert.AreEqual(geoA.Crystals[i].Archetype, geoB.Crystals[i].Archetype);
            }
        }

        [Test]
        public void DifferentSeedsDiffer()
        {
            var a = SpecimenGenerator.Generate(100);
            var b = SpecimenGenerator.Generate(101);
            Assert.AreNotEqual(Fingerprint(a), Fingerprint(b));
        }

        [Test]
        public void GeometryIsSaneAcrossManySeeds()
        {
            for (ulong seed = 500; seed < 560; seed++)
            {
                var g = SpecimenGenerator.Generate(seed);
                var geo = GeodeMeshBuilder.Build(g);
                Assert.Greater(geo.Crystals.Count, 0, $"seed {seed} ({g.Mineral}, {g.Cavity}) has no crystals");
                Assert.Less(geo.Crystals.Count, 900, $"seed {seed} too many crystals");
                Assert.Greater(geo.MaxRadius, 0.03f);
                Assert.Less(geo.MaxRadius, 0.3f);
                foreach (var v in geo.Bottom.Vertices) Assert.IsFalse(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z));
                foreach (var c in geo.Crystals)
                {
                    Assert.IsFalse(float.IsNaN(c.Position.x));
                    Assert.Greater(c.Scale.y, 0f);
                    Assert.LessOrEqual(c.Position.magnitude, geo.MaxRadius * 1.05f, $"seed {seed} crystal outside rock");
                }
            }
        }

        [Test]
        public void DistributionReport()
        {
            const int n = 2000;
            var minerals = new Dictionary<MineralId, int>();
            var cavities = new Dictionary<CavityArchetype, int>();
            var tiers = new Dictionary<QualityTier, List<float>>();
            int traits = 0, secondary = 0, druzy = 0;
            for (ulong seed = 1; seed <= n; seed++)
            {
                var g = SpecimenGenerator.Generate(seed * 7919UL);
                minerals[g.Mineral] = minerals.GetValueOrDefault(g.Mineral) + 1;
                cavities[g.Cavity] = cavities.GetValueOrDefault(g.Cavity) + 1;
                if (!tiers.ContainsKey(g.Tier)) tiers[g.Tier] = new List<float>();
                tiers[g.Tier].Add(g.BaseValue);
                traits += g.Traits.Count;
                if (g.HasSecondary) secondary++;
                if (g.IsDruzy) druzy++;
            }
            var sb = new StringBuilder("Specimen distribution over " + n + " seeds\n");
            foreach (var kv in minerals) sb.AppendLine($"  {kv.Key}: {kv.Value}");
            foreach (var kv in cavities) sb.AppendLine($"  cavity {kv.Key}: {kv.Value}");
            foreach (var kv in tiers)
            {
                kv.Value.Sort();
                float med = kv.Value[kv.Value.Count / 2];
                sb.AppendLine($"  tier {kv.Key}: n={kv.Value.Count} min={kv.Value[0]} median={med} max={kv.Value[kv.Value.Count - 1]}");
            }
            sb.AppendLine($"  traits/specimen={traits / (float)n:F2} secondary={secondary} druzy={druzy}");
            Debug.Log(sb.ToString());
            Assert.GreaterOrEqual(minerals.Count, 10);
            Assert.GreaterOrEqual(cavities.Count, 6);
        }
    }
}
