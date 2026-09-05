using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Tests.EditMode
{
    /// <summary>
    /// The region model under spatial cleaning and look-to-discover inspection. These are the rules that make
    /// "one side clean while the other is filthy" true, so they are worth pinning down.
    /// </summary>
    public sealed class SurfaceTests
    {
        [Test]
        public void EveryRegionRoundTripsThroughItsOwnDirection()
        {
            for (int r = 0; r < SpecimenSurface.Regions; r++)
                Assert.AreEqual(r, SpecimenSurface.RegionOf(SpecimenSurface.DirectionOf(r)),
                    $"region {r} did not map back to itself");
        }

        [Test]
        public void OppositeSidesAreDifferentRegions()
        {
            for (int r = 0; r < SpecimenSurface.Regions; r++)
            {
                int opposite = SpecimenSurface.Index(SpecimenSurface.LongitudeOf(r) + SpecimenSurface.Longitudes / 2,
                                                     SpecimenSurface.BandOf(r));
                Assert.AreNotEqual(r, opposite);
                Assert.AreEqual(0f, SpecimenSurface.Falloff(r, opposite), "a brush must not reach round the far side");
            }
        }

        [Test]
        public void FalloffOnlyTouchesNeighbours()
        {
            int centre = SpecimenSurface.Index(3, 1);
            int reached = 0;
            for (int r = 0; r < SpecimenSurface.Regions; r++) if (SpecimenSurface.Falloff(centre, r) > 0f) reached++;
            Assert.AreEqual(1f, SpecimenSurface.Falloff(centre, centre));
            // itself, two along, one above, one below, and the four diagonals
            Assert.AreEqual(9, reached, "a brush stroke should reach its own patch and the ring around it, no further");
        }

        [Test]
        public void CleaningOnePatchLeavesTheRest()
        {
            var c = new SpecimenCondition();
            c.EnsureRegions();
            int r = SpecimenSurface.Index(2, 1);
            c.SetCleanAt(r, 1f);
            Assert.AreEqual(1f, c.CleanAt(r), 0.01f);
            int opposite = SpecimenSurface.Index(6, 1);
            Assert.AreEqual(0f, c.CleanAt(opposite), 0.01f, "the far side must still be dirty");
            Assert.Less(c.Cleaned, 0.1f, "one patch of twenty-four barely moves the whole-rock figure");
        }

        [Test]
        public void DirtiestRegionNamesThePatchYouMissed()
        {
            var c = new SpecimenCondition();
            c.EnsureRegions();
            for (int r = 0; r < SpecimenSurface.Regions; r++) c.SetCleanAt(r, 1f);
            int missed = SpecimenSurface.Index(5, 0);
            c.SetCleanAt(missed, 0.1f);
            var (region, dirt) = c.DirtiestRegion();
            Assert.AreEqual(missed, region);
            Assert.Greater(dirt, 0.8f);
        }

        [Test]
        public void AnUntrackedRockFallsBackToItsWholeRockFigure()
        {
            // §23: a career saved before cleaning was spatial must load exactly as clean as it was
            var c = new SpecimenCondition { Cleaned = 0.6f };
            Assert.AreEqual(0.6f, c.CleanAt(0), 0.001f);
            Assert.AreEqual(0.6f, c.CleanAt(SpecimenSurface.Regions - 1), 0.001f);
            c.EnsureRegions();
            Assert.AreEqual(0.6f, c.CleanAt(0), 0.01f, "starting to track patches must not change how clean it is");
            Assert.AreEqual(0.6f, c.Cleaned, 0.01f);
        }

        [Test]
        public void CluesAreDeterministicAndPlaced()
        {
            for (ulong seed = 1; seed < 40; seed++)
            {
                var g = SpecimenGenerator.Generate(seed * 7919UL);
                var a = SpecimenSurface.Clues(g);
                var b = SpecimenSurface.Clues(g);
                Assert.AreEqual(a.Count, b.Count, "the same rock must read the same way twice");
                for (int i = 0; i < a.Count; i++)
                {
                    Assert.AreEqual(a[i].Region, b[i].Region);
                    Assert.AreEqual(a[i].Kind, b[i].Kind);
                    Assert.IsTrue(a[i].Region >= 0 && a[i].Region < SpecimenSurface.Regions, "clue outside the shell");
                    Assert.AreNotEqual(ClueKind.None, a[i].Kind);
                }
            }
        }

        [Test]
        public void EveryRockHasSomethingToFind()
        {
            int empty = 0;
            for (ulong seed = 1; seed < 120; seed++)
                if (SpecimenSurface.Clues(SpecimenGenerator.Generate(seed * 104729UL)).Count == 0) empty++;
            Assert.AreEqual(0, empty, "a rock with no clues at all makes inspection pointless");
        }

        [Test]
        public void ReadingNeverNamesTheMineral()
        {
            // §5.6: exterior inspection reports evidence, never the answer
            for (ulong seed = 1; seed < 60; seed++)
            {
                var g = SpecimenGenerator.Generate(seed * 31337UL);
                string verdict = SpecimenSurface.Reading(SpecimenSurface.Clues(g));
                Assert.IsFalse(verdict.Contains(g.Family.Name), "the shell must not give the family away: " + verdict);
                if (g.HasSecondary) Assert.IsFalse(verdict.Contains(g.SecondaryFamily.Name), verdict);
            }
        }

        [Test]
        public void ClueStateOnlyEverMovesForward()
        {
            var c = new SpecimenCondition();
            c.SetClue(3, ClueState.Logged);
            c.SetClue(3, ClueState.Seen);
            Assert.AreEqual(ClueState.Logged, c.ClueAt(3), "a clue already read must not be un-read");
        }

        [Test]
        public void ConditionClonesCarryTheirPatchesAndObservations()
        {
            var c = new SpecimenCondition();
            c.EnsureRegions();
            c.SetCleanAt(4, 0.75f);
            c.SetClue(2, ClueState.Logged);
            var copy = c.Clone();
            copy.SetCleanAt(4, 0f);
            Assert.AreEqual(0.75f, c.CleanAt(4), 0.01f, "the clone must not share the original's array");
            Assert.AreEqual(ClueState.Logged, copy.ClueAt(2));
        }
    }
}
