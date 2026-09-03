using NUnit.Framework;
using UnityEngine;
using GeodeEmpire.Core;
using GeodeEmpire.Cracking;
using GeodeEmpire.Specimens;
using GeodeEmpire.Workshop;

namespace GeodeEmpire.Tests
{
    /// <summary>V5 specimen-specific preparation: seat quality from the hull, clay hiding the seam, chips as starters.</summary>
    public class PreparationTests
    {
        [Test]
        public void SeatQualityFollowsTheStance()
        {
            // a sphere in the sandbag ring is firm; a tall rock on a corner rocks; a boulder on the small ring never sits well
            Assert.That(Preparation.Stability(0.15f, 0.06f, 0.075f, 0.085f, false, false), Is.GreaterThanOrEqualTo(0.9f));
            Assert.That(Preparation.Stability(0.15f, 0.015f, 0.075f, 0.085f, false, false), Is.LessThan(StressModel.UnstableBelow));
            Assert.That(Preparation.Stability(0.3f, 0.12f, 0.15f, 0.085f, true, false), Is.LessThanOrEqualTo(0.2f));
            Assert.That(Preparation.Stability(0.3f, 0.12f, 0.15f, 0.14f, false, false), Is.GreaterThanOrEqualTo(0.9f));   // the heavy cradle takes it
            Assert.That(Preparation.Stability(0.15f, 0.015f, 0.075f, 0.085f, false, true), Is.GreaterThanOrEqualTo(0.85f));  // the clamp holds it
            Assert.That(Preparation.Stability(0.05f, 0.005f, 0.03f, 0.085f, false, false), Is.GreaterThanOrEqualTo(0.9f));   // a pebble drops into the hollow
        }

        [Test]
        public void HullProfileTellsFlatFromOnEdge()
        {
            // a flattened rock lying flat stands wider than the same rock stood on its edge
            SpecimenGeology g = null;
            for (ulong seed = 1; seed < 4000; seed++) { var c = SpecimenGenerator.Generate(seed); if (c.Exterior == ExteriorArchetype.Flattened && c.SizeClass == SizeClass.Medium) { g = c; break; } }
            Assert.That(g, Is.Not.Null);
            var geo = GeodeMeshBuilder.Build(g);
            var bottom = geo.Bottom.ToColliderMesh("b", GeodeMeshBuilder.Longitudes, GeodeMeshBuilder.Latitudes);
            var top = geo.Top.ToColliderMesh("t", GeodeMeshBuilder.Longitudes, GeodeMeshBuilder.Latitudes);
            SpecimenEntity.SupportProfileOf(bottom, top, Quaternion.identity, Preparation.RingContactHeight, out float hFlat, out float wFlat);
            SpecimenEntity.SupportProfileOf(bottom, top, Quaternion.Euler(90f, 0f, 0f), Preparation.RingContactHeight, out float hEdge, out float wEdge);
            float sFlat = Preparation.Stability(hFlat, wFlat, geo.MaxRadius, 0.085f, false, false);
            float sEdge = Preparation.Stability(hEdge, wEdge, geo.MaxRadius, 0.085f, false, false);
            Debug.Log($"flattened seed {g.SeedString}: flat h={hFlat:F3} w={wFlat:F3} s={sFlat:F2}   on edge h={hEdge:F3} w={wEdge:F3} s={sEdge:F2}");
            Assert.That(hEdge, Is.GreaterThan(hFlat * 1.15f));
            Assert.That(sFlat, Is.GreaterThanOrEqualTo(0.8f));
            Assert.That(sEdge, Is.LessThan(sFlat));
            Object.DestroyImmediate(bottom); Object.DestroyImmediate(top);
        }

        [Test]
        public void FirmSeatsNeverWobbleAndRockingSeatsDo()
        {
            var rng = new SeededRandom(7UL);
            var firm = new StressModel { Stability = 1f };
            var rocking = new StressModel { Stability = 0.1f };
            int wobbledFirm = 0, wobbledRocking = 0;
            for (int i = 0; i < 40; i++)
            {
                var input = new StressModel.StrikeInput { Azimuth = i * 0.37f, PlaneOffset = 0.05f, Force = 0.5f, AngleFactor = 1f };
                if (firm.Strike(input, ref rng).Wobbled) wobbledFirm++;
                if (rocking.Strike(input, ref rng).Wobbled) wobbledRocking++;
            }
            Assert.That(wobbledFirm, Is.EqualTo(0));
            Assert.That(wobbledRocking, Is.EqualTo(40));
        }

        [Test]
        public void ChipsOnTheSeamAreStartersAndClayHidesTheSeam()
        {
            int withChip = 0, total = 0;
            for (ulong seed = 1; seed < 3000; seed++)
            {
                var g = SpecimenGenerator.Generate(seed);
                int sector = Preparation.ChipSector(g);
                if (g.HasNaturalChip && Mathf.Abs(g.ChipLatitude) <= 0.3f) { Assert.That(sector, Is.InRange(0, StressModel.Sectors - 1)); withChip++; }
                else Assert.That(sector, Is.EqualTo(-1));
                total++;
            }
            Assert.That(withChip, Is.GreaterThan(total / 40));   // a few percent of rocks come with a start
            Assert.That(Preparation.Cleanliness(0f), Is.EqualTo(1f));
            Assert.That(Preparation.Cleanliness(0.25f), Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(Preparation.Cleanliness(0.9f), Is.EqualTo(0f));
            Assert.That(Preparation.SeatWord(0.95f), Is.EqualTo("firm"));
            Assert.That(Preparation.SeatWord(0.5f), Is.EqualTo("uneven"));
            Assert.That(Preparation.SeatWord(0.1f), Is.EqualTo("rocking"));
        }
    }
}
