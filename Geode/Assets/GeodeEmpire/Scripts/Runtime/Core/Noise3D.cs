using UnityEngine;

namespace GeodeEmpire.Core
{
    /// <summary>Seeded 3D Perlin gradient noise with fBm helpers. Output of Sample is in [-1,1].</summary>
    public sealed class Noise3D
    {
        private readonly int[] _perm = new int[512];

        public Noise3D(ulong seed)
        {
            var rng = new SeededRandom(seed);
            var p = new int[256];
            for (int i = 0; i < 256; i++) p[i] = i;
            for (int i = 255; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                (p[i], p[j]) = (p[j], p[i]);
            }
            for (int i = 0; i < 512; i++) _perm[i] = p[i & 255];
        }

        private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

        private static float Grad(int hash, float x, float y, float z)
        {
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        public float Sample(Vector3 p) => Sample(p.x, p.y, p.z);

        public float Sample(float x, float y, float z)
        {
            int X = Mathf.FloorToInt(x) & 255;
            int Y = Mathf.FloorToInt(y) & 255;
            int Z = Mathf.FloorToInt(z) & 255;
            x -= Mathf.Floor(x);
            y -= Mathf.Floor(y);
            z -= Mathf.Floor(z);
            float u = Fade(x), v = Fade(y), w = Fade(z);
            int A = _perm[X] + Y, AA = _perm[A] + Z, AB = _perm[A + 1] + Z;
            int B = _perm[X + 1] + Y, BA = _perm[B] + Z, BB = _perm[B + 1] + Z;
            float res = Mathf.Lerp(
                Mathf.Lerp(Mathf.Lerp(Grad(_perm[AA], x, y, z), Grad(_perm[BA], x - 1, y, z), u),
                           Mathf.Lerp(Grad(_perm[AB], x, y - 1, z), Grad(_perm[BB], x - 1, y - 1, z), u), v),
                Mathf.Lerp(Mathf.Lerp(Grad(_perm[AA + 1], x, y, z - 1), Grad(_perm[BA + 1], x - 1, y, z - 1), u),
                           Mathf.Lerp(Grad(_perm[AB + 1], x, y - 1, z - 1), Grad(_perm[BB + 1], x - 1, y - 1, z - 1), u), v), w);
            return Mathf.Clamp(res * 1.1f, -1f, 1f);
        }

        /// <summary>Fractal Brownian motion, normalised to roughly [-1,1].</summary>
        public float Fbm(Vector3 p, int octaves, float lacunarity = 2f, float gain = 0.5f)
        {
            float sum = 0f, amp = 1f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Sample(p) * amp;
                norm += amp;
                amp *= gain;
                p *= lacunarity;
            }
            return sum / norm;
        }

        /// <summary>Ridged noise in [0,1]: sharp creases, good for angular rock.</summary>
        public float Ridged(Vector3 p, int octaves, float lacunarity = 2f, float gain = 0.5f)
        {
            float sum = 0f, amp = 1f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                float n = 1f - Mathf.Abs(Sample(p));
                sum += n * n * amp;
                norm += amp;
                amp *= gain;
                p *= lacunarity;
            }
            return sum / norm;
        }
    }
}
