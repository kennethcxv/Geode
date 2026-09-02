using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using GeodeEmpire.Core;

namespace GeodeEmpire.EditorTools
{
    /// <summary>
    /// Generates small, tileable procedural textures (no external content). Deterministic per seed.
    /// </summary>
    public static class ProceduralTextureFactory
    {
        public const string TextureFolder = "Assets/GeodeEmpire/Textures";

        [MenuItem("GeodeEmpire/Assets/Generate Core Textures")]
        public static void GenerateCoreMenu() => EnsureCoreTextures(true);

        public static void EnsureCoreTextures(bool force = false)
        {
            Directory.CreateDirectory(TextureFolder);
            if (force || !File.Exists(TextureFolder + "/T_Noise.png"))
                Save("T_Noise", 256, NoisePixels(256, 9001), linear: true, compress: false);
            if (force || !File.Exists(TextureFolder + "/T_Rock.png"))
                Save("T_Rock", 512, RockPixels(512, 9002), linear: false, compress: true);
            AssetDatabase.Refresh();
        }

        // ---------------------------------------------------------------------------------
        // Tileable value noise
        // ---------------------------------------------------------------------------------
        private static float Hash(int x, int y, int period, ulong seed)
        {
            x = ((x % period) + period) % period;
            y = ((y % period) + period) % period;
            ulong h = seed ^ ((ulong)x * 0x9E3779B97F4A7C15UL) ^ ((ulong)y * 0xC2B2AE3D27D4EB4FUL);
            h = SeededRandom.SplitMix64(ref h);
            return (h >> 11) * (1f / 9007199254740992f);
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);

        public static float TileableValueNoise(float x, float y, int period, ulong seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = Smooth(x - xi), fy = Smooth(y - yi);
            float a = Hash(xi, yi, period, seed), b = Hash(xi + 1, yi, period, seed);
            float c = Hash(xi, yi + 1, period, seed), d = Hash(xi + 1, yi + 1, period, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        public static float TileableFbm(float u, float v, int basePeriod, int octaves, ulong seed, float gain = 0.5f)
        {
            float sum = 0f, amp = 1f, norm = 0f;
            int period = basePeriod;
            for (int o = 0; o < octaves; o++)
            {
                sum += (TileableValueNoise(u * period, v * period, period, seed + (ulong)o * 77) - 0.5f) * amp;
                norm += amp;
                amp *= gain;
                period *= 2;
            }
            return sum / norm + 0.5f;
        }

        private static Color[] NoisePixels(int size, ulong seed)
        {
            var px = new Color[size * size];
            var rng = new SeededRandom(seed);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size, v = y / (float)size;
                    float r = TileableFbm(u, v, 4, 5, seed);
                    float g = rng.NextFloat();
                    float b = TileableFbm(u, v, 8, 4, seed + 500, 0.6f);
                    float a = TileableFbm(u, v, 16, 2, seed + 900);
                    px[y * size + x] = new Color(r, g, b, a);
                }
            }
            // soften the sparkle channel slightly so it is not pure pixel noise
            var copy = (Color[])px.Clone();
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float s = 0f;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    s += copy[((y + dy + size) % size) * size + ((x + dx + size) % size)].g;
                var p = px[y * size + x];
                p.g = Mathf.Clamp01(Mathf.Lerp(p.g, s / 9f, 0.35f));
                px[y * size + x] = p;
            }
            return px;
        }

        private static Color[] RockPixels(int size, ulong seed)
        {
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size, v = y / (float)size;
                    float grain = TileableFbm(u, v, 6, 6, seed, 0.55f);
                    float speck = TileableFbm(u, v, 48, 2, seed + 31);
                    float cracks = 1f - Mathf.Abs(TileableFbm(u, v, 3, 4, seed + 77) - 0.5f) * 2f;
                    float val = Mathf.Clamp01(grain * 0.75f + speck * 0.3f - Mathf.Pow(cracks, 8f) * 0.35f + 0.05f);
                    px[y * size + x] = new Color(val, val, val, 1f);
                }
            }
            return px;
        }

        public static string Save(string name, int size, Color[] pixels, bool linear, bool compress)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, linear);
            tex.SetPixels(pixels);
            tex.Apply();
            string path = TextureFolder + "/" + name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.sRGBTexture = !linear;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.mipmapEnabled = true;
            imp.maxTextureSize = size;
            imp.textureCompression = compress ? TextureImporterCompression.Compressed : TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
            return path;
        }
    }
}

namespace GeodeEmpire.EditorTools
{
    /// <summary>Tileable workshop surface textures: concrete, plaster, wood, metal, cardboard, straw.</summary>
    public static class WorkshopTextures
    {
        public static void EnsureAll(bool force = false)
        {
            var f = ProceduralTextureFactory.TextureFolder;
            System.IO.Directory.CreateDirectory(f);
            if (force || !System.IO.File.Exists(f + "/T_Concrete.png")) ProceduralTextureFactory.Save("T_Concrete", 512, Concrete(512, 501), false, true);
            if (force || !System.IO.File.Exists(f + "/T_Plaster.png")) ProceduralTextureFactory.Save("T_Plaster", 512, Plaster(512, 502), false, true);
            if (force || !System.IO.File.Exists(f + "/T_Wood.png")) ProceduralTextureFactory.Save("T_Wood", 512, Wood(512, 503, new Color(0.64f, 0.47f, 0.3f), new Color(0.38f, 0.26f, 0.15f)), false, true);
            if (force || !System.IO.File.Exists(f + "/T_WoodDark.png")) ProceduralTextureFactory.Save("T_WoodDark", 512, Wood(512, 504, new Color(0.36f, 0.25f, 0.16f), new Color(0.18f, 0.12f, 0.07f)), false, true);
            if (force || !System.IO.File.Exists(f + "/T_Metal.png")) ProceduralTextureFactory.Save("T_Metal", 256, Metal(256, 505), false, true);
            if (force || !System.IO.File.Exists(f + "/T_Cardboard.png")) ProceduralTextureFactory.Save("T_Cardboard", 256, Cardboard(256, 506), false, true);
            if (force || !System.IO.File.Exists(f + "/T_Straw.png")) ProceduralTextureFactory.Save("T_Straw", 256, Straw(256, 507), false, true);
            UnityEditor.AssetDatabase.Refresh();
        }

        private static float F(float u, float v, int p, int oct, ulong seed, float gain = 0.5f) => ProceduralTextureFactory.TileableFbm(u, v, p, oct, seed, gain);

        private static Color[] Concrete(int size, ulong seed)
        {
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float big = F(u, v, 1, 3, seed, 0.45f);
                float grain = F(u, v, 32, 3, seed + 3);
                float speck = F(u, v, 128, 1, seed + 9);
                float stain = Mathf.Pow(F(u, v, 2, 3, seed + 21), 4f);
                float joint = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                float jointDark = joint < 0.004f ? 0.22f : joint < 0.008f ? 0.08f : 0f;
                float val = 0.6f + (big - 0.5f) * 0.1f + (grain - 0.5f) * 0.05f + (speck > 0.8f ? 0.04f : 0f) - stain * 0.22f - jointDark;
                px[y * size + x] = new Color(val, val * 0.99f, val * 0.96f, 1f);
            }
            return px;
        }

        private static Color[] Plaster(int size, ulong seed)
        {
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float big = F(u, v, 2, 3, seed);
                float fine = F(u, v, 40, 2, seed + 5);
                float val = 0.82f + (big - 0.5f) * 0.08f + (fine - 0.5f) * 0.06f;
                px[y * size + x] = new Color(val, val * 0.98f, val * 0.94f, 1f);
            }
            return px;
        }

        private static Color[] Wood(int size, ulong seed, Color light, Color dark)
        {
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float warp = F(u, v, 2, 3, seed) - 0.5f;
                float grain = Mathf.Sin((v * 90f + warp * 9f + F(u * 0.5f, v, 12, 2, seed + 7) * 2.5f) * Mathf.PI) * 0.5f + 0.5f;
                grain = Mathf.Pow(grain, 1.6f);
                float fine = F(u, v, 96, 2, seed + 11);
                float pores = F(u * 0.25f, v, 160, 1, seed + 19);
                // plank seams every quarter tile along v
                float plank = Mathf.Repeat(v * 4f + F(u, v, 1, 1, seed + 13) * 0.02f, 1f);
                float seam = plank < 0.012f || plank > 0.988f ? 0.55f : 1f;
                var c = Color.Lerp(light, dark, grain * 0.32f + (fine - 0.5f) * 0.18f + (pores - 0.5f) * 0.12f) * seam;
                px[y * size + x] = new Color(c.r, c.g, c.b, 1f);
            }
            return px;
        }

        private static Color[] Metal(int size, ulong seed)
        {
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float scratch = F(u, v * 0.05f, 48, 2, seed);
                float spots = F(u, v, 6, 3, seed + 3);
                float val = 0.52f + (scratch - 0.5f) * 0.16f + (spots - 0.5f) * 0.1f;
                px[y * size + x] = new Color(val, val, val * 1.02f, 1f);
            }
            return px;
        }

        private static Color[] Cardboard(int size, ulong seed)
        {
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float fine = F(u, v, 48, 2, seed);
                float corr = Mathf.Sin(u * 160f) * 0.5f + 0.5f;
                float val = 0.72f + (fine - 0.5f) * 0.1f + (corr - 0.5f) * 0.03f;
                px[y * size + x] = new Color(val, val * 0.8f, val * 0.58f, 1f);
            }
            return px;
        }

        private static Color[] Straw(int size, ulong seed)
        {
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float streak = F(u * 0.1f, v, 40, 2, seed);
                float streak2 = F(u, v * 0.1f, 40, 2, seed + 17);
                float val = 0.62f + (Mathf.Max(streak, streak2) - 0.5f) * 0.45f;
                px[y * size + x] = new Color(val * 1.05f, val * 0.85f, val * 0.45f, 1f);
            }
            return px;
        }
    }
}
