using System;
using UnityEngine;

namespace GeodeEmpire.Checkout
{
    /// <summary>
    /// A count of physical pieces, one slot per denomination. Ported from Golf's register stacks.
    /// MONEY IS INTEGER CENTS internally: a drawer holds hundreds of dimes and 0.1f * 300 is 30.000002, which makes a
    /// till that balances on paper fail to balance in code. Cents in, dollars out at the edge.
    /// </summary>
    [Serializable]
    public sealed class MoneyStack
    {
        public int[] Counts = new int[Money.Denoms.Length];

        public MoneyStack() { }
        public MoneyStack(MoneyStack other) { Array.Copy(other.Counts, Counts, Counts.Length); }

        public int this[int index] { get => Counts[index]; set => Counts[index] = Mathf.Max(0, value); }

        public int CountOf(float denom) { int i = Money.IndexOf(denom); return i < 0 ? 0 : Counts[i]; }
        public int TotalCents { get { int c = 0; for (int i = 0; i < Counts.Length; i++) c += Counts[i] * Money.DenomCents[i]; return c; } }
        public float Total => Money.Dollars(TotalCents);
        public int Pieces { get { int n = 0; for (int i = 0; i < Counts.Length; i++) n += Counts[i]; return n; } }
        public bool Empty => Pieces == 0;

        public MoneyStack Copy() => new MoneyStack(this);

        public void Add(float denom, int n = 1) { int i = Money.IndexOf(denom); if (i >= 0) Counts[i] += n; }
        public void AddIndex(int index, int n = 1) { if (index >= 0 && index < Counts.Length) Counts[index] += n; }

        /// <summary>One piece at a time: this is a hand reaching into a till, not a transfer.</summary>
        public bool Take(float denom, int n = 1)
        {
            int i = Money.IndexOf(denom);
            if (i < 0 || Counts[i] < n) return false;
            Counts[i] -= n;
            return true;
        }

        public void AddAll(MoneyStack other) { for (int i = 0; i < Counts.Length; i++) Counts[i] += other.Counts[i]; }
        public void Clear() { Array.Clear(Counts, 0, Counts.Length); }

        public bool Same(MoneyStack other)
        {
            if (other == null) return false;
            for (int i = 0; i < Counts.Length; i++) if (Counts[i] != other.Counts[i]) return false;
            return true;
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < Counts.Length; i++) if (Counts[i] > 0) sb.Append($"{Money.Label(Money.Denoms[i])}x{Counts[i]} ");
            return sb.Length > 0 ? sb.ToString().TrimEnd() : "(empty)";
        }
    }

    /// <summary>
    /// The currency the till works in, ported verbatim from Golf's src/sim/register.js. The quarter is canonical; the
    /// drawer's five note slots and five coin wells are the physical contract the cash_drawer kit was authored to.
    /// </summary>
    public static class Money
    {
        public static readonly float[] Bills = { 50f, 20f, 10f, 5f, 1f };
        public static readonly float[] Coins = { 0.5f, 0.25f, 0.1f, 0.05f, 0.01f };
        public static readonly float[] Denoms = { 50f, 20f, 10f, 5f, 1f, 0.5f, 0.25f, 0.1f, 0.05f, 0.01f };
        public static readonly int[] DenomCents = { 5000, 2000, 1000, 500, 100, 50, 25, 10, 5, 1 };

        public static int Cents(float v) => Mathf.RoundToInt(v * 100f);
        public static float Dollars(int cents) => cents / 100f;
        public static float Round(float v) => Dollars(Cents(v));
        public static bool IsBill(float denom) => denom >= 1f;

        public static int IndexOf(float denom)
        {
            int c = Cents(denom);
            for (int i = 0; i < DenomCents.Length; i++) if (DenomCents[i] == c) return i;
            return -1;
        }

        public static string Label(float denom) => denom < 1f ? $"{Mathf.RoundToInt(denom * 100f)}c" : $"${denom:0}";

        /// <summary>The opening float. A till that cannot break a fifty stalls the queue, so this is a real bank.</summary>
        public static MoneyStack NewDrawer()
        {
            var s = new MoneyStack();
            s.Add(50f, 2); s.Add(20f, 5); s.Add(10f, 8); s.Add(5f, 10); s.Add(1f, 25);
            s.Add(0.5f, 16); s.Add(0.25f, 25); s.Add(0.1f, 20); s.Add(0.05f, 20); s.Add(0.01f, 50);
            return s;
        }

        /// <summary>Greedy over the unlimited canonical set: the fewest pieces for an amount.</summary>
        public static MoneyStack MakeChange(float amount)
        {
            int left = Cents(amount);
            var s = new MoneyStack();
            for (int i = 0; i < DenomCents.Length && left > 0; i++)
            {
                int n = left / DenomCents[i];
                if (n <= 0) continue;
                s[i] = n;
                left -= n * DenomCents[i];
            }
            return s;
        }

        /// <summary>
        /// Change the till can ACTUALLY produce, bounded by what is really in each slot. Dynamic programming, not
        /// greedy: a temporarily empty slot can make a greedy choice fail even when exact change exists. Returns null
        /// when the drawer genuinely cannot make it.
        /// </summary>
        public static MoneyStack MakeChangeFrom(MoneyStack drawer, float amount)
        {
            int target = Cents(amount);
            if (target < 0) return null;
            if (target == 0) return new MoneyStack();
            var bestPieces = new int[target + 1];
            var bestStack = new MoneyStack[target + 1];
            for (int i = 1; i <= target; i++) bestPieces[i] = int.MaxValue;
            bestStack[0] = new MoneyStack();
            for (int d = 0; d < Denoms.Length; d++)
            {
                int have = drawer != null ? drawer[d] : 0;
                if (have <= 0) continue;
                int dc = DenomCents[d];
                var beforePieces = (int[])bestPieces.Clone();
                var beforeStack = (MoneyStack[])bestStack.Clone();
                for (int value = 0; value <= target; value++)
                {
                    if (beforeStack[value] == null) continue;
                    int max = Mathf.Min(have, (target - value) / dc);
                    for (int count = 1; count <= max; count++)
                    {
                        int next = value + count * dc;
                        int pieces = beforePieces[value] + count;
                        if (bestStack[next] != null && bestPieces[next] <= pieces) continue;
                        var s = beforeStack[value].Copy();
                        s[d] = count;
                        bestStack[next] = s;
                        bestPieces[next] = pieces;
                    }
                }
            }
            return bestStack[target];
        }

        /// <summary>
        /// Cents a customer would plausibly have loose: quarters and dimes, nickels only to finish a five. Anything
        /// needing a penny is not a gesture anybody makes at a counter, it is counting out shrapnel.
        /// </summary>
        public static bool PayableInLargeCoins(int cents)
        {
            int left = cents % 25;
            left %= 10;
            return left % 5 == 0;
        }

        /// <summary>
        /// What a person actually pulls out of a wallet. Two behaviours: notes for the dollars and coins for the cents
        /// (so the change comes back in whole dollars), or round up to the next clean note. Golf's G5/F4 findings.
        /// </summary>
        public static MoneyStack CustomerTender(float due, System.Random rng)
        {
            if (Cents(due) <= 0) return new MoneyStack();
            float step = due > 100f ? 50f : due > 40f ? 20f : due > 15f ? 10f : 5f;
            int oddCents = Cents(due) % 100;
            float amount = Mathf.Ceil(Round(due) / step) * step;
            double roll = rng != null ? rng.NextDouble() : 1.0;
            if (oddCents > 0 && PayableInLargeCoins(oddCents) && roll < 0.55)
                amount = Round(Mathf.Ceil(Round(due - oddCents / 100f) / step) * step + oddCents / 100f);
            else if (oddCents > 0 && oddCents % 25 == 0 && roll < 0.35)
                amount = Round(amount + oddCents / 100f);
            return MakeChange(amount);
        }

        /// <summary>
        /// Pre-cent saves and the retired 20c piece: rebalance without creating or destroying value. A 20c piece
        /// becomes two dimes (identical money); sparse drawers are left alone rather than invented into.
        /// </summary>
        public static MoneyStack MigrateDrawer(MoneyStack drawer)
        {
            if (drawer == null) return NewDrawer();
            var outStack = drawer.Copy();
            int opening = outStack.TotalCents;
            void ExchangeInto(float denom, int minimum)
            {
                int index = IndexOf(denom);
                int have = outStack[index];
                int needed = minimum - have;
                if (needed <= 0) return;
                var source = outStack.Copy();
                source[index] = 0;
                var plan = MakeChangeFrom(source, denom * needed);
                if (plan == null) return;
                for (int i = 0; i < plan.Counts.Length; i++) outStack[i] -= plan[i];
                outStack[index] = have + needed;
            }
            ExchangeInto(0.01f, 50);
            ExchangeInto(0.5f, 16);
            return outStack.TotalCents == opening ? outStack : drawer.Copy();
        }
    }
}
