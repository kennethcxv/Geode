using System.Collections.Generic;
using UnityEngine;
using GeodeEmpire.Core;

namespace GeodeEmpire.Audio
{
    /// <summary>
    /// Procedurally synthesised placeholder audio bank + pooled one-shot player. Every clip is generated
    /// deterministically at startup (no binary assets), and can later be swapped for bespoke recordings by name.
    /// </summary>
    public static class WorkshopAudio
    {
        private static readonly Dictionary<string, AudioClip[]> _bank = new Dictionary<string, AudioClip[]>();
        private static readonly List<AudioSource> _pool = new List<AudioSource>();
        private static GameObject _root;
        private static bool _built;
        private static int _poolIndex;
        private const int SampleRate = 44100;

        public static float SfxVolume => GameSettings.Current.SfxVolume;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _built = false; _root = null; _pool.Clear(); _bank.Clear(); _poolIndex = 0;
        }

        public static void EnsureBuilt()
        {
            if (_built && _root != null) return;
            // (re)build: statics survive scene loads but the pool objects may have been destroyed
            _built = true;
            _pool.Clear();
            _bank.Clear();
            _root = new GameObject("_WorkshopAudio");
            Object.DontDestroyOnLoad(_root);
            for (int i = 0; i < 16; i++)
            {
                var go = new GameObject("Voice" + i);
                go.transform.SetParent(_root.transform, false);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.spatialBlend = 1f;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.minDistance = 0.6f;
                src.maxDistance = 9f;
                src.dopplerLevel = 0f;
                _pool.Add(src);
            }
            Build();
        }

        public static void Play(string name, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            EnsureBuilt();
            if (!_bank.TryGetValue(name, out var clips) || clips.Length == 0) return;
            var clip = clips[Random.Range(0, clips.Length)];
            var src = _pool[_poolIndex++ % _pool.Count];
            if (src == null) { _root = null; EnsureBuilt(); src = _pool[_poolIndex++ % _pool.Count]; }
            src.transform.position = position;
            src.pitch = pitch * Random.Range(0.96f, 1.04f);
            src.volume = volume * SfxVolume;
            src.spatialBlend = 1f;
            src.clip = clip;
            src.Play();
        }

        public static void Play2D(string name, float volume = 1f, float pitch = 1f)
        {
            EnsureBuilt();
            if (!_bank.TryGetValue(name, out var clips) || clips.Length == 0) return;
            var clip = clips[Random.Range(0, clips.Length)];
            var src = _pool[_poolIndex++ % _pool.Count];
            if (src == null) { _root = null; EnsureBuilt(); src = _pool[_poolIndex++ % _pool.Count]; }
            src.pitch = pitch;
            src.volume = volume * SfxVolume;
            src.spatialBlend = 0f;
            src.clip = clip;
            src.Play();
        }

        // ------------------------------------------------------------------------------------
        // Synthesis
        // ------------------------------------------------------------------------------------
        private static void Build()
        {
            _bank["tap_light"] = Variants(3, i => Impact(0.12f, 1800f + i * 200f, 0.6f, 0.02f, 0.25f, 5.0f, seed: 10 + (ulong)i));
            _bank["tap_medium"] = Variants(3, i => Impact(0.18f, 1200f + i * 120f, 0.9f, 0.05f, 0.45f, 3.5f, seed: 20 + (ulong)i));
            _bank["tap_heavy"] = Variants(3, i => Impact(0.26f, 700f + i * 80f, 1.0f, 0.12f, 0.7f, 2.4f, seed: 30 + (ulong)i));
            _bank["creak"] = Variants(2, i => Creak(0.35f, seed: 40 + (ulong)i));
            _bank["tick"] = Variants(3, i => Impact(0.06f, 3200f + i * 400f, 0.35f, 0f, 0.5f, 9f, seed: 50 + (ulong)i));
            _bank["crack_final"] = Variants(2, i => FinalCrack(seed: 60 + (ulong)i));
            _bank["fragments"] = Variants(2, i => Fragments(seed: 70 + (ulong)i));
            _bank["rock_place"] = Variants(3, i => Impact(0.14f, 420f + i * 60f, 0.55f, 0.1f, 0.5f, 2.2f, seed: 80 + (ulong)i));
            _bank["rock_pickup"] = Variants(2, i => Impact(0.09f, 900f, 0.25f, 0.02f, 0.5f, 6f, seed: 90 + (ulong)i));
            _bank["crate_open"] = Variants(1, i => Creak(0.6f, seed: 100, low: true));
            _bank["wood_knock"] = Variants(2, i => Impact(0.16f, 300f + i * 40f, 0.6f, 0.15f, 0.45f, 2.0f, seed: 110 + (ulong)i));
            _bank["crystal_chime"] = Variants(3, i => Chime(2400f + i * 500f, 0.5f, seed: 120 + (ulong)i));
            _bank["crystal_break"] = Variants(2, i => CrystalBreak(seed: 130 + (ulong)i));
            _bank["ui_click"] = Variants(1, i => Chime(1500f, 0.08f, seed: 140, noise: 0.2f));
            _bank["ui_buy"] = Variants(1, i => Chime(880f, 0.35f, seed: 150, second: 1320f));
            _bank["ui_sell"] = Variants(1, i => Chime(1046f, 0.4f, seed: 160, second: 1568f));
            _bank["ui_error"] = Variants(1, i => Chime(220f, 0.25f, seed: 170, noise: 0.35f));
            _bank["discovery"] = Variants(1, i => Discovery(seed: 180));
            _bank["slip"] = Variants(2, i => Slip(seed: 190 + (ulong)i));
            _bank["thud"] = Variants(2, i => Impact(0.4f, 120f + i * 20f, 1.0f, 0.5f, 0.6f, 1.2f, seed: 200 + (ulong)i));
            _bank["ambience"] = new[] { Ambience(seed: 300) };
        }

        private static AudioClip[] Variants(int n, System.Func<int, float[]> gen)
        {
            var arr = new AudioClip[n];
            for (int i = 0; i < n; i++)
            {
                var data = gen(i);
                var clip = AudioClip.Create("synth", data.Length, 1, SampleRate, false);
                clip.SetData(data, 0);
                arr[i] = clip;
            }
            return arr;
        }

        private static float[] Impact(float duration, float freq, float amp, float lowMix, float noiseMix, float decayRate, ulong seed)
        {
            int n = (int)(duration * SampleRate);
            var d = new float[n];
            var rng = new SeededRandom(seed);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * decayRate * 12f);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t * (1f + 0.3f * Mathf.Exp(-t * 60f))) * (1f - noiseMix);
                float low = Mathf.Sin(2f * Mathf.PI * 90f * t) * Mathf.Exp(-t * 18f) * lowMix;
                float noise = (rng.NextFloat() * 2f - 1f);
                lp += (noise - lp) * 0.35f;
                float click = i < 40 ? (1f - i / 40f) * 0.8f : 0f;
                d[i] = Mathf.Clamp((tone + lp * noiseMix * 1.5f + low + click * noise) * env * amp, -1f, 1f);
            }
            return d;
        }

        private static float[] Creak(float duration, ulong seed, bool low = false)
        {
            int n = (int)(duration * SampleRate);
            var d = new float[n];
            var rng = new SeededRandom(seed);
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float f = (low ? 140f : 320f) + Mathf.Sin(t * 23f) * 60f + rng.NextFloat() * 30f;
                phase += f / SampleRate;
                float grain = Mathf.Sign(Mathf.Sin(phase * 2f * Mathf.PI)) * 0.3f + Mathf.Sin(phase * 2f * Mathf.PI * 3f) * 0.2f;
                float env = Mathf.Sin(t / duration * Mathf.PI);
                d[i] = grain * env * 0.5f * (0.7f + 0.3f * rng.NextFloat());
            }
            return d;
        }

        private static float[] FinalCrack(ulong seed)
        {
            float duration = 0.9f;
            int n = (int)(duration * SampleRate);
            var d = new float[n];
            var rng = new SeededRandom(seed);
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float noise = rng.NextFloat() * 2f - 1f;
                lp += (noise - lp) * 0.6f;      // bright transient
                lp2 += (noise - lp2) * 0.06f;   // body
                float transient = lp * Mathf.Exp(-t * 55f) * 1.2f;
                float body = lp2 * Mathf.Exp(-t * 9f) * 2.2f;
                float thump = Mathf.Sin(2f * Mathf.PI * 70f * t) * Mathf.Exp(-t * 14f) * 0.9f;
                // splintering ticks
                float ticks = 0f;
                if (t > 0.03f && t < 0.5f && rng.NextFloat() < 0.012f) ticks = (rng.NextFloat() * 2f - 1f) * 0.8f;
                d[i] = Mathf.Clamp((transient + body + thump + ticks * Mathf.Exp(-t * 4f)) * 0.9f, -1f, 1f);
            }
            return d;
        }

        private static float[] Fragments(ulong seed)
        {
            float duration = 1.1f;
            int n = (int)(duration * SampleRate);
            var d = new float[n];
            var rng = new SeededRandom(seed);
            int next = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                if (i >= next)
                {
                    next = i + rng.Range(400, 3000);
                    float f = rng.Range(1500f, 5000f);
                    float a = rng.Range(0.1f, 0.5f) * Mathf.Exp(-t * 2.5f);
                    int len = rng.Range(200, 900);
                    for (int k = 0; k < len && i + k < n; k++)
                    {
                        float tt = k / (float)SampleRate;
                        d[i + k] += Mathf.Sin(2f * Mathf.PI * f * tt) * Mathf.Exp(-tt * 300f) * a;
                    }
                }
            }
            for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(d[i], -1f, 1f);
            return d;
        }

        private static float[] Chime(float freq, float duration, ulong seed, float noise = 0f, float second = 0f)
        {
            int n = (int)(duration * SampleRate);
            var d = new float[n];
            var rng = new SeededRandom(seed);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 9f) * Mathf.Min(1f, t * 400f);
                float v = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * freq * 2.01f * t) * 0.15f;
                if (second > 0f && t > duration * 0.35f) v += Mathf.Sin(2f * Mathf.PI * second * (t - duration * 0.35f)) * 0.45f * Mathf.Exp(-(t - duration * 0.35f) * 7f);
                v += (rng.NextFloat() * 2f - 1f) * noise;
                d[i] = Mathf.Clamp(v * env * 0.6f, -1f, 1f);
            }
            return d;
        }

        private static float[] CrystalBreak(ulong seed)
        {
            float duration = 0.35f;
            int n = (int)(duration * SampleRate);
            var d = new float[n];
            var rng = new SeededRandom(seed);
            float f1 = rng.Range(2800f, 4200f), f2 = f1 * 1.37f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 22f);
                float v = Mathf.Sin(2f * Mathf.PI * f1 * t) * 0.4f + Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.3f;
                v += (rng.NextFloat() * 2f - 1f) * Mathf.Exp(-t * 90f) * 0.9f;
                d[i] = Mathf.Clamp(v * env, -1f, 1f);
            }
            return d;
        }

        private static float[] Discovery(ulong seed)
        {
            float duration = 1.6f;
            int n = (int)(duration * SampleRate);
            var d = new float[n];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            for (int k = 0; k < notes.Length; k++)
            {
                int start = (int)(k * 0.11f * SampleRate);
                for (int i = start; i < n; i++)
                {
                    float t = (i - start) / (float)SampleRate;
                    float env = Mathf.Exp(-t * 2.6f) * Mathf.Min(1f, t * 200f);
                    d[i] += (Mathf.Sin(2f * Mathf.PI * notes[k] * t) * 0.35f + Mathf.Sin(2f * Mathf.PI * notes[k] * 2f * t) * 0.08f) * env;
                }
            }
            for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(d[i] * 0.5f, -1f, 1f);
            return d;
        }

        private static float[] Slip(ulong seed)
        {
            float duration = 0.22f;
            int n = (int)(duration * SampleRate);
            var d = new float[n];
            var rng = new SeededRandom(seed);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float noise = rng.NextFloat() * 2f - 1f;
                lp += (noise - lp) * (0.15f + t * 2f);
                d[i] = Mathf.Clamp(lp * Mathf.Sin(t / duration * Mathf.PI) * 0.8f, -1f, 1f);
            }
            return d;
        }

        private static AudioClip Ambience(ulong seed)
        {
            float duration = 8f;
            int n = (int)(duration * SampleRate);
            var d = new float[n];
            var rng = new SeededRandom(seed);
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float noise = rng.NextFloat() * 2f - 1f;
                lp += (noise - lp) * 0.012f;
                lp2 += (lp - lp2) * 0.05f;
                float hum = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.02f + Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.01f;
                // loop-safe: fade ends symmetrically
                float edge = Mathf.Min(1f, Mathf.Min(t, duration - t) * 4f);
                d[i] = (lp2 * 2.2f + hum) * 0.35f * edge;
            }
            var clip = AudioClip.Create("ambience", n, 1, SampleRate, false);
            clip.SetData(d, 0);
            return clip;
        }

        public static AudioClip GetClip(string name)
        {
            EnsureBuilt();
            return _bank.TryGetValue(name, out var c) && c.Length > 0 ? c[0] : null;
        }
    }
}
