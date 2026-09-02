using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeodeEmpire.Core
{
    /// <summary>
    /// Deterministic xoroshiro128+ PRNG. Value type: copies diverge, so pass by ref
    /// (or keep a single field) when several callers must share one stream.
    /// Never depends on UnityEngine.Random or wall-clock time.
    /// </summary>
    [Serializable]
    public struct SeededRandom
    {
        private ulong _s0;
        private ulong _s1;

        public SeededRandom(ulong seed)
        {
            ulong x = seed;
            _s0 = SplitMix64(ref x);
            _s1 = SplitMix64(ref x);
            if (_s0 == 0 && _s1 == 0) _s1 = 0x9E3779B97F4A7C15UL;
        }

        public static ulong SplitMix64(ref ulong x)
        {
            x += 0x9E3779B97F4A7C15UL;
            ulong z = x;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        private static ulong RotL(ulong x, int k) => (x << k) | (x >> (64 - k));

        public ulong NextULong()
        {
            ulong a = _s0, b = _s1;
            ulong r = a + b;
            b ^= a;
            _s0 = RotL(a, 24) ^ b ^ (b << 16);
            _s1 = RotL(b, 37);
            return r;
        }

        /// <summary>Uniform float in [0,1).</summary>
        public float NextFloat() => (float)((NextULong() >> 11) * (1.0 / 9007199254740992.0));

        public double NextDouble() => (NextULong() >> 11) * (1.0 / 9007199254740992.0);

        public float Range(float min, float max) => min + (max - min) * NextFloat();

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return minInclusive + (int)(NextULong() % (ulong)(maxExclusive - minInclusive));
        }

        public bool Chance(float probability) => NextFloat() < probability;

        public int Sign() => Chance(0.5f) ? 1 : -1;

        /// <summary>Standard normal sample (Box-Muller).</summary>
        public float Gaussian()
        {
            float u1 = 1f - NextFloat();
            float u2 = NextFloat();
            return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
        }

        public float Gaussian(float mean, float stdDev) => mean + Gaussian() * stdDev;

        /// <summary>Gaussian clamped to [min,max].</summary>
        public float GaussianClamped(float mean, float stdDev, float min, float max)
            => Mathf.Clamp(Gaussian(mean, stdDev), min, max);

        public int PickWeighted(IReadOnlyList<float> weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Count; i++) total += Mathf.Max(0f, weights[i]);
            if (total <= 0f) return 0;
            float r = NextFloat() * total;
            for (int i = 0; i < weights.Count; i++)
            {
                r -= Mathf.Max(0f, weights[i]);
                if (r < 0f) return i;
            }
            return weights.Count - 1;
        }

        public T Pick<T>(IReadOnlyList<T> items) => items[Range(0, items.Count)];

        public Vector3 OnUnitSphere()
        {
            float y = Range(-1f, 1f);
            float t = Range(0f, Mathf.PI * 2f);
            float r = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            return new Vector3(r * Mathf.Cos(t), y, r * Mathf.Sin(t));
        }

        public Vector2 InsideUnitCircle()
        {
            float a = Range(0f, Mathf.PI * 2f);
            float r = Mathf.Sqrt(NextFloat());
            return new Vector2(r * Mathf.Cos(a), r * Mathf.Sin(a));
        }

        /// <summary>Derive an independent child stream. Advances this stream once.</summary>
        public SeededRandom Branch(uint salt)
        {
            ulong x = NextULong() ^ (salt * 0x9E3779B97F4A7C15UL);
            return new SeededRandom(x);
        }

        public static ulong HashString(string s)
        {
            ulong h = 14695981039346656037UL;
            if (s == null) return h;
            foreach (char c in s)
            {
                h ^= c;
                h *= 1099511628211UL;
            }
            return h;
        }

        public static ulong Combine(ulong a, ulong b)
        {
            ulong x = a ^ (b + 0x9E3779B97F4A7C15UL + (a << 6) + (a >> 2));
            return SplitMix64(ref x);
        }
    }
}
