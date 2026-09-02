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
