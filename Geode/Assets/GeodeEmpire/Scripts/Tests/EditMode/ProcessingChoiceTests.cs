using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using GeodeEmpire.Core;
using GeodeEmpire.Cracking;
using GeodeEmpire.Specimens;

namespace GeodeEmpire.Tests
{
    /// <summary>
    /// Hammer vs saw on the same deterministic rocks: no processing method may dominate. The hammer is simulated
    /// with the stress model (careful ring work vs reckless pounding) and the saw with the piece geometry (centre cut
    /// vs a poor off-centre tilted cut). Values are the appraisal's; time and cost are the station's own numbers.
    /// </summary>
    public class ProcessingChoiceTests
    {
        private struct Outcome { public float Value, Seconds, Cost; public string Note; }

        private static Outcome Hammer(SpecimenGeology g, bool careful, ulong seed)
        {
            var fam = g.Family;
            var m = new StressModel { Toughness = fam.ShellToughness, ShellThickness = g.ShellThickness, Fragility = fam.Fragility, Radius = g.Size * 1.15f, SeamQuality = g.SeamQuality, SectorThickness = g.SectorThickness };
            var rng = new SeededRandom(seed);
            var geo = GeodeMeshBuilder.Build(g);
            var cond = new SpecimenCondition();
            cond.EnsureSize(geo.Crystals.Count);
            int strikes = 0, sector = 0;
            float shell = 0f;
            while (strikes < 80)
            {
                var s = new StressModel.StrikeInput { Azimuth = sector / (float)StressModel.Sectors * Mathf.PI * 2f, PlaneOffset = careful ? 0.05f : 0.2f, Force = careful ? 0.5f : 1f, AngleFactor = careful ? 0.9f : 0.6f };
                var r = m.Strike(s, ref rng);
                strikes++;
                if (r.Damaged)
                {
                    // a patch of the carpet near the strike
                    int hit = 0;
                    foreach (var c in geo.Crystals)
                    {
                        float d = Mathf.Abs(Mathf.DeltaAngle(c.Azimuth * Mathf.Rad2Deg, s.Azimuth * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                        if (d > 0.6f || c.Latitude > 0.75f) continue;
                        if (hit++ >= 1 + Mathf.RoundToInt(r.DamageSeverity * (2f + geo.Crystals.Count * 0.035f))) break;
                        byte cur = cond.DamageAt(c.Index);
                        byte next = r.DamageSeverity > 0.85f && hit == 1 ? CrystalDamage.Missing : r.DamageSeverity > 0.6f ? CrystalDamage.Broken : CrystalDamage.Chipped;
                        cond.CrystalDamage[c.Index] = (byte)Mathf.Max(cur, next);
                    }
                    shell = Mathf.Clamp01(shell + r.DamageSeverity * 0.12f);
                }
                if (r.Opened) break;
                if (careful) sector = (sector + 2) % StressModel.Sectors;   // works round the ring
                else if (strikes % 3 == 0) sector = (sector + 1) % StressModel.Sectors;
            }
            float total = 0f, lost = 0f;
            foreach (var c in geo.Crystals)
            {
                float w = c.Height * c.Height * (c.Centerpiece ? 4f : 1f);
                total += w;
                byte d = cond.DamageAt(c.Index);
                lost += w * (d == CrystalDamage.Chipped ? 0.3f : d == CrystalDamage.Broken ? 0.7f : d >= CrystalDamage.Missing ? 1f : 0f);
            }
            float dmg = total > 0f ? lost / total : 0f;
            return new Outcome { Value = Valuation.DamagedValue(g, dmg, shell), Seconds = strikes * (careful ? 4.2f : 2.6f) + 8f, Cost = 0f, Note = $"{strikes} strikes dmg={dmg:F2}" };
        }

        private static Outcome Saw(SpecimenGeology g, bool good)
        {
            var lobe = g.LobeCenters[0];
            Vector3 n = good ? Vector3.up : new Vector3(0.3f, 1f, 0.2f).normalized;
            float centre = Vector3.Dot(lobe, n);
            float h = good ? centre : centre + g.Size * 0.3f;
            var a = PieceShape.Below(n, h - 0.0015f);
            var b = PieceShape.Above(n, h + 0.0015f);
            float value = 0f;
            foreach (var shape in new[] { a, b })
            {
                var geo = GeodeMeshBuilder.BuildPiece(g, shape);
                value += Valuation.PieceValue(g, shape, geo.RetainedCrystalFraction, geo.CavityOpening, geo.CutSymmetry, geo.FaceAreaFraction, 0f, 0f, 0f);
            }
            float seconds = 12f + (2f * (0.125f + g.Size * 1.15f) + 0.04f) / 0.02f;   // setup + travel at nominal feed
            float wear = 0.045f * g.Family.ShellToughness * (g.Size * 1.15f / 0.065f);
            return new Outcome { Value = value, Seconds = seconds, Cost = wear * 45f, Note = good ? "centre" : "off-centre tilted" };
        }

        [Test]
        public void HammerVsSawReport()
        {
            var sb = new StringBuilder("Hammer vs saw on the same rocks (value $, seconds, blade cost)\n");
            int n = 0, sawWins = 0, hammerWins = 0;
            float sumHc = 0f, sumHr = 0f, sumSg = 0f, sumSp = 0f, sumBase = 0f;
            var perFamily = new Dictionary<MineralId, float[]>();
            for (ulong seed = 20000; seed < 20800 && n < 120; seed++)
            {
                var g = SpecimenGenerator.Generate(seed);
                if (g.SizeClass == SizeClass.Oversized || g.Tier < QualityTier.Decent) continue;
                var hc = Hammer(g, true, seed * 3);
                var hr = Hammer(g, false, seed * 5);
                var sg = Saw(g, true);
                var sp = Saw(g, false);
                n++;
                sumBase += g.BaseValue; sumHc += hc.Value; sumHr += hr.Value; sumSg += sg.Value; sumSp += sp.Value;
                if (sg.Value > hc.Value * 1.05f) sawWins++; else if (hc.Value > sg.Value * 1.05f) hammerWins++;
                if (!perFamily.TryGetValue(g.Mineral, out var acc)) perFamily[g.Mineral] = acc = new float[4];
                acc[0] += hc.Value; acc[1] += sg.Value; acc[2] += 1f; acc[3] += g.BaseValue;
                if (n <= 8) sb.AppendLine($"  {g.Mineral,-11} {g.Cavity,-13} {g.Tier,-11} base=${g.BaseValue,5}  hammer careful ${hc.Value,5} {hc.Seconds,4:F0}s ({hc.Note})  reckless ${hr.Value,5} ({hr.Note})  saw centre ${sg.Value,5} {sg.Seconds,3:F0}s ${sg.Cost:F1}  saw poor ${sp.Value,5}");
            }
            sb.AppendLine($"  over {n} rocks: base ${sumBase / n:F0}  hammer careful ${sumHc / n:F0}  reckless ${sumHr / n:F0}  saw centre ${sumSg / n:F0}  saw poor ${sumSp / n:F0}  (saw better on {sawWins}, hammer better on {hammerWins})");
            foreach (var kv in perFamily) sb.AppendLine($"    {kv.Key,-11} n={kv.Value[2]:F0}  base ${kv.Value[3] / kv.Value[2]:F0}  hammer ${kv.Value[0] / kv.Value[2]:F0}  saw ${kv.Value[1] / kv.Value[2]:F0}");
            Debug.Log(sb.ToString());
            Assert.Greater(sumHc, sumHr, "careful hammering must beat reckless hammering");
            Assert.Greater(sumSg, sumSp, "a centre cut must beat a poor cut");
            // neither method dominates: each wins on a real share of rocks
            // "at least a tenth" (crystal placement noise moves these by a rock or two between geometry revisions)
            Assert.GreaterOrEqual(sawWins, n / 10, "the saw should be the better call on at least a tenth of decent rocks");
            Assert.GreaterOrEqual(hammerWins, n / 10, "the hammer should be the better call on at least a tenth of decent rocks");
        }
    }
}
