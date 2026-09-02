using NUnit.Framework;
using UnityEngine;
using GeodeEmpire.Core;
using GeodeEmpire.Cracking;

namespace GeodeEmpire.Tests
{
    /// <summary>The fracture model must reward technique: seam placement, working around the ring, moderate force.</summary>
    public class StressModelTests
    {
        private static StressModel NewModel(float thickness = 0.2f, float fragility = 0.5f, float toughness = 1f)
            => new StressModel { ShellThickness = thickness, Fragility = fragility, Toughness = toughness };

        private static int StrikesToOpen(float force, float planeOffset, bool rotate, ulong seed, out int damageEvents)
        {
            var m = NewModel();
            var rng = new SeededRandom(seed);
            damageEvents = 0;
            for (int i = 0; i < 200; i++)
            {
                float az = rotate ? i * 0.62f : 0.1f;
                var r = m.Strike(new StressModel.StrikeInput { Azimuth = az, PlaneOffset = planeOffset, Force = force, AngleFactor = 0.9f }, ref rng);
                if (r.Damaged) damageEvents++;
                if (r.Opened) return i + 1;
            }
            return 200;
        }

        [Test]
        public void SeamHitsOpenFarFasterThanOffSeamHits()
        {
            int onSeam = StrikesToOpen(0.55f, 0.02f, true, 1, out _);
            int offSeam = StrikesToOpen(0.55f, 0.7f, true, 1, out _);
            Assert.Less(onSeam, 30, "medium strikes on the seam should open a rock in a reasonable number of hits");
            Assert.Greater(offSeam, onSeam * 2, "hitting far from the seam must be much less effective");
        }

        [Test]
        public void WorkingAroundTheRingBeatsHammeringOneSpot()
        {
            int around = StrikesToOpen(0.55f, 0.02f, true, 2, out _);
            int oneSpot = StrikesToOpen(0.55f, 0.02f, false, 2, out _);
            Assert.Less(around, oneSpot, "rotating the rock should open it in fewer strikes than pounding one sector");
        }

        [Test]
        public void HeavyBlowsDamageMoreThanCarefulOnes()
        {
            int heavyDamage = 0, carefulDamage = 0, lightDamage = 0;
            for (ulong s = 10; s < 40; s++)
            {
                StrikesToOpen(1.0f, 0.05f, true, s, out int d1); heavyDamage += d1;
                StrikesToOpen(0.5f, 0.05f, true, s, out int d2); carefulDamage += d2;
                StrikesToOpen(0.3f, 0.05f, true, s, out int d3); lightDamage += d3;
            }
            Debug.Log($"damage events over 30 rocks: heavy={heavyDamage} careful={carefulDamage} light={lightDamage}");
            Assert.Greater(heavyDamage, carefulDamage * 2, "full-force cracking should damage clearly more");
            Assert.LessOrEqual(lightDamage, carefulDamage, "light taps should be the safest");
        }

        [Test]
        public void OpeningIsNotAFixedClickCount()
        {
            var counts = new System.Collections.Generic.HashSet<int>();
            for (ulong s = 100; s < 112; s++) counts.Add(StrikesToOpen(0.5f + (s % 3) * 0.2f, 0.05f + (s % 4) * 0.08f, s % 2 == 0, s, out _));
            Assert.GreaterOrEqual(counts.Count, 4, "different technique must produce different strike counts");
        }

        [Test]
        public void StressRoundTripsThroughArrays()
        {
            var m = NewModel();
            var rng = new SeededRandom(7);
            m.Strike(new StressModel.StrikeInput { Azimuth = 0.3f, PlaneOffset = 0f, Force = 0.6f, AngleFactor = 1f }, ref rng);
            var saved = m.ToArray();
            var m2 = NewModel();
            m2.CopyFrom(saved);
            Assert.AreEqual(m.TotalStress(), m2.TotalStress(), 1e-5f);
        }
    }
}
