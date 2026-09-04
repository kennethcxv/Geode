using UnityEngine;
using GeodeEmpire.Save;

namespace GeodeEmpire.Economy
{
    /// <summary>
    /// Standing in the trade, tiers 0-5, read from what the career has actually done: pieces sold and kept, customers
    /// served, clean opens, sawn and polished work, requests filled, families found. No experience points; the same
    /// milestones that unlock sources, buyers, Stage 3 and the exhibition.
    /// </summary>
    public static class Reputation
    {
        public static readonly string[] Words = { "Unknown", "Local", "Known", "Respected", "Sought after", "Renowned" };

        public static int Score(GameState s)
        {
            var st = s.Stats;
            int score = 0;
            score += Mathf.Min(20, st.SpecimensSold / 3);                          // trade volume
            score += Mathf.Min(20, st.CustomersServed);                            // the showroom
            score += Mathf.Min(15, Mathf.RoundToInt(s.CollectionValue() / 300f));  // the cabinet
            score += Mathf.Min(10, st.CleanOpens / 2);                             // craft: clean opens
            score += Mathf.Min(10, st.SawCuts / 2 + st.PiecesPolished);            // craft: lapidary
            score += Mathf.Min(10, st.CommissionsFilled * 3);                      // buyers who came back
            score += Mathf.Min(10, s.Encyclopedia.Count);                          // families known
            score += Mathf.Min(5, s.Prestige);
            return score;
        }

        public static int Tier(GameState s)
        {
            int sc = Score(s);
            return sc >= 80 ? 5 : sc >= 60 ? 4 : sc >= 42 ? 3 : sc >= 25 ? 2 : sc >= 10 ? 1 : 0;
        }

        public static string Word(GameState s) => Words[Mathf.Clamp(Tier(s), 0, Words.Length - 1)];

        /// <summary>What the next tier wants, in words, for the tablet.</summary>
        public static string NextStep(GameState s)
        {
            int t = Tier(s);
            if (t >= 5) return "The trade knows your name.";
            int[] need = { 10, 25, 42, 60, 80 };
            return $"{need[t] - Score(s)} more standing to be {Words[t + 1].ToLowerInvariant()}: sell, serve customers, open clean, cut and polish, fill requests, find families.";
        }
    }
}
