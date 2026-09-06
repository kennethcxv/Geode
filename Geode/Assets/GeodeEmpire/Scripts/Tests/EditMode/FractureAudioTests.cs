using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GeodeEmpire.Audio;

namespace GeodeEmpire.Tests
{
    /// <summary>
    /// §9.2/§9.3: the break must actually differ by size, material and tool. These assert the layer plan rather
    /// than "an audio call happened", so a future refactor that quietly flattens the balance back to one sample
    /// for every rock fails here instead of shipping.
    /// </summary>
    public class FractureAudioTests
    {
        private const float Small = FractureAudio.SmallRadius, Large = FractureAudio.LargeRadius;
        private const float Soft = 0.6f, Tough = 1.4f;

        private static FractureLayer Layer(List<FractureLayer> plan, string cue)
        {
            foreach (var l in plan) if (l.Cue == cue) return l;
            Assert.Fail("plan has no '" + cue + "' layer: " + string.Join(", ", plan));
            return default;
        }
        private static bool Has(List<FractureLayer> plan, string cue)
        {
            foreach (var l in plan) if (l.Cue == cue) return true;
            return false;
        }

        [Test]
        public void Break_is_layered_in_time_not_one_shot()
        {
            var plan = FractureAudio.Plan(0.07f, 1f, FractureAudio.Tool.Hammer, false);
            Assert.GreaterOrEqual(plan.Count, 5, "§9.1 wants tool impact, onset, split, debris and settling");
            // the layers must be spread over the break, not stacked on frame zero
            float first = float.MaxValue, last = float.MinValue;
            foreach (var l in plan) { first = Mathf.Min(first, l.Delay); last = Mathf.Max(last, l.Delay); }
            Assert.AreEqual(0f, first, 1e-4f, "something has to land on the blow itself");
            Assert.Greater(last, 0.2f, "the settling layer must trail the break");
        }

        [Test]
        public void Bigger_rocks_break_lower_and_louder()
        {
            var small = FractureAudio.Plan(Small, 1f, FractureAudio.Tool.Hammer, false);
            var large = FractureAudio.Plan(Large, 1f, FractureAudio.Tool.Hammer, false);
            Assert.Less(Layer(large, "stone_split").Pitch, Layer(small, "stone_split").Pitch, "a large rock splits lower");
            Assert.Greater(Layer(large, "stone_split").Volume, Layer(small, "stone_split").Volume, "and with more mass behind it");
            Assert.Less(Layer(large, "debris_settle").Pitch, Layer(small, "debris_settle").Pitch);
        }

        [Test]
        public void Tougher_shells_snap_brighter()
        {
            var soft = FractureAudio.Plan(0.07f, Soft, FractureAudio.Tool.Hammer, false);
            var tough = FractureAudio.Plan(0.07f, Tough, FractureAudio.Tool.Hammer, false);
            Assert.Greater(Layer(tough, "crack_onset").Pitch, Layer(soft, "crack_onset").Pitch);
            Assert.Greater(Layer(tough, "fragments").Pitch, Layer(soft, "fragments").Pitch);
        }

        [Test]
        public void Only_a_tough_shell_rings_on_after_the_break()
        {
            Assert.IsTrue(Has(FractureAudio.Plan(0.07f, Tough, FractureAudio.Tool.Hammer, false), "tension"));
            Assert.IsFalse(Has(FractureAudio.Plan(0.07f, Soft, FractureAudio.Tool.Hammer, false), "tension"));
            Assert.IsFalse(Has(FractureAudio.Plan(0.07f, Tough, FractureAudio.Tool.Saw, false), "tension"),
                "a saw parts a face; it does not leave a shell ringing");
        }

        [Test]
        public void The_three_tools_are_balanced_differently()
        {
            var hammer = FractureAudio.Plan(0.07f, 1f, FractureAudio.Tool.Hammer, false);
            var cracker = FractureAudio.Plan(0.07f, 1f, FractureAudio.Tool.Cracker, false);
            var saw = FractureAudio.Plan(0.07f, 1f, FractureAudio.Tool.Saw, false);
            // the press has no metal transient in front of it, and the saw barely any
            Assert.Less(Layer(cracker, "crack_onset").Volume, Layer(hammer, "crack_onset").Volume);
            Assert.Less(Layer(saw, "crack_onset").Volume, Layer(cracker, "crack_onset").Volume);
            // but the press puts more mass through the split than the hammer does
            Assert.Greater(Layer(cracker, "stone_split").Volume, Layer(hammer, "stone_split").Volume);
            // and a cut throws far less off the rock than a break
            Assert.Less(Layer(saw, "fragments").Volume, Layer(hammer, "fragments").Volume * 0.5f);
        }

        [Test]
        public void A_shattered_shell_throws_more_debris()
        {
            var clean = FractureAudio.Plan(0.07f, 1f, FractureAudio.Tool.Hammer, false);
            var burst = FractureAudio.Plan(0.07f, 1f, FractureAudio.Tool.Hammer, false, shattered: true);
            Assert.Greater(Layer(burst, "fragments").Volume, Layer(clean, "fragments").Volume);
            Assert.Greater(Layer(burst, "debris_settle").Volume, Layer(clean, "debris_settle").Volume);
        }

        [Test]
        public void Every_cue_the_plan_names_exists_in_the_bank()
        {
            var seen = new HashSet<string>();
            foreach (var tool in new[] { FractureAudio.Tool.Hammer, FractureAudio.Tool.Cracker, FractureAudio.Tool.Saw })
            foreach (var t in new[] { Soft, Tough })
            foreach (var l in FractureAudio.Plan(0.07f, t, tool, false)) seen.Add(l.Cue);
            foreach (var cue in seen)
                Assert.IsNotNull(WorkshopAudio.GetClip(cue), "no clip generated for '" + cue + "'");
        }

        /// <summary>§8.2: the bad hit has to be a different sound, not a quieter one.</summary>
        [Test]
        public void The_dead_hit_is_spectrally_duller_than_a_good_one()
        {
            float live = Brightness(WorkshopAudio.GetClip("tap_medium"));
            float dead = Brightness(WorkshopAudio.GetClip("tap_dead"));
            Assert.Greater(live, dead * 1.5f,
                $"tap_dead must be audibly duller, not just quieter (bright: live {live:0.000} vs dead {dead:0.000})");
        }

        [Test]
        public void The_onset_is_short_and_bright_and_the_split_is_long_and_low()
        {
            var onset = WorkshopAudio.GetClip("crack_onset");
            var split = WorkshopAudio.GetClip("stone_split");
            Assert.Less(onset.length, 0.2f, "the onset is the first fibre giving, not the break");
            Assert.Greater(split.length, 0.5f, "the split carries the body of the break");
            Assert.Greater(Brightness(onset), Brightness(split) * 1.5f);
        }

        /// <summary>
        /// Fraction of a clip's energy above about 2.5 kHz — the band the ear finds shrill. A one-pole high pass
        /// and two sums; no FFT needed for a comparison this coarse.
        ///
        /// Zero-crossing rate was the first attempt and it was the wrong measure: it scores broadband noise as
        /// bright whatever its centre frequency, so a 220 Hz error tone with a bit of grit in it read as more
        /// piercing than a hammer on stone. "Piercing" is about where the energy sits, not how often the signal
        /// changes sign.
        /// </summary>
        private static float Brightness(AudioClip clip)
        {
            Assert.IsNotNull(clip);
            var d = new float[clip.samples * clip.channels];
            clip.GetData(d, 0);
            const float alpha = 0.737f;                 // one-pole high pass, ~2.5 kHz at 44.1 kHz
            float y = 0f, hi = 0f, all = 0f;
            for (int i = 1; i < d.Length; i++)
            {
                y = alpha * (y + d[i] - d[i - 1]);
                hi += y * y;
                all += d[i] * d[i];
            }
            return all > 1e-9f ? hi / all : 0f;
        }
    
        /// <summary>
        /// §21 asks for the set to be volume-matched and for UI sounds not to be piercing. Both are measurable:
        /// peak level across the bank, and brightness on the cues that play into the player's ear rather than
        /// into the room. This also catches a cue that is referenced by name but never generated — those play
        /// silence, and nothing else in the project would notice.
        /// </summary>
        [Test]
        public void Every_cue_the_game_asks_for_exists_and_the_set_is_level_matched()
        {
            // every name the runtime plays, gathered here so a typo or a deleted generator fails loudly
            string[] cues =
            {
                "swing", "chisel_ring", "tension", "tap_light", "tap_medium", "tap_heavy", "tap_dead", "creak",
                "tick", "crack_final", "crack_onset", "stone_split", "debris_settle", "fragments", "rock_place",
                "rock_pickup", "crate_open", "wood_knock", "crystal_chime", "crystal_break", "discovery", "slip",
                "thud", "loupe_up", "loupe_down", "shop_bell", "counter_bell", "register_beep", "register",
                "knock_0", "knock_1", "knock_2", "scrub", "scrub_dry", "sponge", "scrape", "splash", "clamp",
                "cut_through", "slab_place", "bill_notice",
                "ui_click", "ui_buy", "ui_sell", "ui_error",
            };
            var peaks = new Dictionary<string, float>();
            foreach (var cue in cues)
            {
                var clip = WorkshopAudio.GetClip(cue);
                Assert.IsNotNull(clip, $"'{cue}' is played by the game but generates no clip — it plays silence");
                peaks[cue] = Peak(clip);
            }

            // nothing may be inaudible, and nothing may be three times the level of the quietest thing in the set
            float lo = float.MaxValue, hi = 0f; string loName = "", hiName = "";
            foreach (var kv in peaks)
            {
                if (kv.Value < lo) { lo = kv.Value; loName = kv.Key; }
                if (kv.Value > hi) { hi = kv.Value; hiName = kv.Key; }
            }
            Assert.Greater(lo, 0.05f, $"'{loName}' peaks at {lo:0.000}: effectively silent");
            Assert.LessOrEqual(hi, 1.0001f, $"'{hiName}' clips at {hi:0.000}");
            Assert.Less(hi / lo, 8f, $"the set is not level matched: '{hiName}' {hi:0.00} against '{loName}' {lo:0.00}");
        }

        /// <summary>§21: "avoid piercing UI sounds" — the cues that go straight to the ear stay off the top end.</summary>
        [Test]
        public void The_interface_does_not_shriek()
        {
            float room = Brightness(WorkshopAudio.GetClip("tap_medium"));
            foreach (var ui in new[] { "ui_click", "ui_buy", "ui_sell", "ui_error", "bill_notice", "register_beep" })
            {
                var clip = WorkshopAudio.GetClip(ui);
                Assert.IsNotNull(clip, ui);
                Assert.Less(Brightness(clip), room * 1.6f,
                    $"'{ui}' is brighter than a hammer tap on stone — that is the piercing UI sound §21 forbids");
                Assert.Less(Peak(clip), 0.92f, $"'{ui}' is louder than the world it interrupts");
            }
        }

        private static float Peak(AudioClip clip)
        {
            var d = new float[clip.samples * clip.channels];
            clip.GetData(d, 0);
            float p = 0f;
            for (int i = 0; i < d.Length; i++) p = Mathf.Max(p, Mathf.Abs(d[i]));
            return p;
        }
}
}
