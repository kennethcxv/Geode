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

        /// <summary>Headline for the goals card: what the whole set is working towards.</summary>
        public static string GoalHeader(GameState s) => "Reach Empire Level " + (Level(s) + 1);
    }
}
