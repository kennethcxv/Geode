using UnityEngine;
using GeodeEmpire.Save;

namespace GeodeEmpire.Core
{
    /// <summary>One of the three standing goals shown under the brand badge.</summary>
    public struct Goal
    {
        public string Label;
        public float Have, Need;
        public bool Money;
        public bool Done => Have >= Need;
        public string Progress => Money
            ? "$" + Mathf.FloorToInt(Have).ToString("N0") + "/$" + Mathf.FloorToInt(Need).ToString("N0")
            : Mathf.FloorToInt(Have).ToString("N0") + "/" + Mathf.FloorToInt(Need).ToString("N0");
    }

    /// <summary>
    /// Day, shop clock, empire level and the standing goals. Every figure here is read out of what the save
    /// already records, so nothing new has to be persisted or kept in sync: the HUD is a view of the career,
    /// not a second copy of it.
    /// </summary>
    public static class Progression
    {
        /// <summary>A shop day is twenty minutes at the bench, opening at eight and closing at eight.</summary>
        public const float DaySeconds = 1200f;
        private const float OpenHour = 8f, CloseHour = 20f;

        public static int Day(GameState s) => s == null ? 1 : 1 + Mathf.FloorToInt(s.Stats.PlayTimeSeconds / DaySeconds);

        /// <summary>Where the current day has got to, 0..1.</summary>
        public static float DayFraction(GameState s)
        {
            if (s == null) return 0f;
            return Mathf.Repeat(s.Stats.PlayTimeSeconds, DaySeconds) / DaySeconds;
        }

        public static string Clock(GameState s)
        {
            float hour = Mathf.Lerp(OpenHour, CloseHour, DayFraction(s));
            int h = Mathf.FloorToInt(hour);
            int m = Mathf.FloorToInt((hour - h) * 60f);
            string suffix = h >= 12 ? "PM" : "AM";
            int h12 = h % 12; if (h12 == 0) h12 = 12;
            return h12 + ":" + m.ToString("00") + " " + suffix;
        }

        /// <summary>Lifetime experience: money turned over, rock opened, pieces finished and minerals first met.</summary>
        public static int Xp(GameState s)
        {
            if (s == null) return 0;
            var st = s.Stats;
            float xp = st.MoneyEarned * 0.5f
                     + st.SpecimensOpened * 25
                     + (st.RetailSales + st.SpecimensSold) * 20
                     + st.PiecesPolished * 40
                     + st.SlabsCut * 30
                     + s.Encyclopedia.Count * 100;
            return Mathf.FloorToInt(xp);
        }

        /// <summary>What the step from <paramref name="level"/> to the next one costs.</summary>
        public static int Span(int level) => 500 + 500 * level;

        /// <summary>Current level, experience into it, and what the next level asks for.</summary>
        public static (int level, int into, int span) LevelProgress(GameState s)
        {
            int xp = Xp(s), level = 1;
            while (xp >= Span(level) && level < 40) { xp -= Span(level); level++; }
            return (level, xp, Span(level));
        }

        public static int Level(GameState s) => LevelProgress(s).level;

        /// <summary>The three standing goals for the current level, scaled so they stay just out of reach.</summary>
        public static Goal[] Goals(GameState s)
        {
            if (s == null) return new Goal[0];
            var st = s.Stats;
            int level = Level(s);
            int openTarget = 5 * level * (level + 1) / 2;               // 5, 15, 30, 50 …
            int sellTarget = 4 * level * (level + 1) / 2;
            float earnTarget = 1000f * level * (level + 1) / 2f;
            return new[]
            {
                new Goal { Label = "Crack " + openTarget + " Geodes", Have = st.SpecimensOpened, Need = openTarget },
                new Goal { Label = "Sell " + sellTarget + " Specimens", Have = st.RetailSales + st.SpecimensSold, Need = sellTarget },
                new Goal { Label = "Earn $" + earnTarget.ToString("N0"), Have = st.MoneyEarned, Need = earnTarget, Money = true },
            };
        }

        /// <summary>One line, not a paragraph: the card is 250 px wide and §65 says not to let it dominate.</summary>
        private static string Clip(string text, int max)
        {
            if (string.IsNullOrEmpty(text)) return "";
            int stop = text.IndexOf(". ", System.StringComparison.Ordinal);
            if (stop > 0 && stop + 1 <= max) return text.Substring(0, stop + 1);
            if (text.Length <= max) return text;
            int cut = text.LastIndexOf(' ', Mathf.Min(max, text.Length - 1));
            if (cut < max / 2) cut = max;
            return text.Substring(0, cut).TrimEnd(',', ';', ':') + "…";
        }

        /// <summary>Headline for the goals card: what the whole set is working towards.</summary>
        public static string GoalHeader(GameState s) => "Reach Empire Level " + (Level(s) + 1);

        /// <summary>
        /// The same answer as <see cref="NextUnlock"/> in one short line, for the HUD. §8.1: the objective card is
        /// a summary, and three lines of prose about what a loupe does is what made it half the screen tall.
        /// </summary>
        public static string NextUnlockShort(GameState s)
        {
            if (s == null) return "";
            string opening = OpeningAction(s);
            if (opening != null) return opening;
            Economy.UpgradeDefinition best = null;
            foreach (var u in Economy.UpgradeCatalog.All)
            {
                if (u.Consumable || s.HasUpgrade(u.Id)) continue;
                if (!string.IsNullOrEmpty(u.Requires) && !s.HasUpgrade(u.Requires)) continue;
                if (best == null || u.Price < best.Price) best = u;
            }
            if (best == null) return "";
            if (s.Cash >= best.Price) return best.Name + " \u2014 affordable";
            return best.Name + " \u2014 $" + Mathf.CeilToInt(best.Price - s.Cash).ToString("N0") + " to go";
        }

        /// <summary>
        /// V6 §65 in full, for the tablet: the next piece of kit the business cannot afford or has not earned yet,
        /// named with what stands between here and there.
        /// </summary>
        public static string NextUnlock(GameState s)
        {
            if (s == null) return "";
            string opening = OpeningAction(s);
            if (opening != null) return opening;
            // the cheapest upgrade that is available and unowned: the one thing to look at
            Economy.UpgradeDefinition best = null;
            foreach (var u in Economy.UpgradeCatalog.All)
            {
                if (u.Consumable || s.HasUpgrade(u.Id)) continue;
                if (!string.IsNullOrEmpty(u.Requires) && !s.HasUpgrade(u.Requires)) continue;
                if (best == null || u.Price < best.Price) best = u;
            }
            // if it is already affordable, say so: that is something to do now, not something to work towards
            if (best != null && s.Cash >= best.Price) return $"{best.Name} — you can afford it. {Clip(best.Effect, 66)}";
            // otherwise the cheapest supplier still behind a condition: it changes what the player opens
            Economy.SupplierDefinition sup = null;
            foreach (var d in Economy.SupplierCatalog.All)
            {
                if (string.IsNullOrEmpty(d.UnlockHint)) continue;        // the starting quarry
                if (s.UnlockedSuppliers.Contains(d.Id)) continue;
                if (sup == null || d.Price < sup.Price) sup = d;
            }
            if (sup != null) return sup.Name + " — " + Clip(sup.UnlockHint, 76);
            if (best == null) return "";
            float shortfall = best.Price - s.Cash;
            return $"{best.Name} — $" + Mathf.CeilToInt(shortfall).ToString("N0") + " to go. " + Clip(best.Effect, 60);
        }

        private static string OpeningAction(GameState s)
        {
            if (s.Stats.CratesPurchased == 0) return "Order your first local crate";
            if (s.Stats.SpecimensOpened == 0) return "Open a rock at the cracking bench";
            if (s.Stats.SpecimensSold + s.Stats.RetailSales == 0) return "Sell your first opened specimen";
            return null;
        }
    }
}
