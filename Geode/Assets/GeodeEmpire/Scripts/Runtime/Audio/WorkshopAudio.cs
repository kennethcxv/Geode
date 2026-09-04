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
        public static float UiVolume => GameSettings.Current.UiVolume;
        private static float VolumeFor(string name) => name.StartsWith("ui") ? UiVolume : SfxVolume;

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

        /// <summary>Start a looping clip on its own source (machines): the caller owns pitch/volume and stops it.</summary>
        public static AudioSource StartLoop(string name, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            EnsureBuilt();
            if (!_bank.TryGetValue(name, out var clips) || clips.Length == 0) return null;
            var go = new GameObject("Loop_" + name);
            go.transform.SetParent(_root.transform, false);
            go.transform.position = position;
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 0.8f;
            src.maxDistance = 10f;
            src.dopplerLevel = 0f;
            src.clip = clips[0];
            src.volume = volume * SfxVolume;
            src.pitch = pitch;
            src.Play();
            return src;
        }

        public static void SetLoop(AudioSource src, float volume, float pitch)
        {
            if (src == null) return;
            src.volume = volume * SfxVolume;
            src.pitch = pitch;
        }

        public static void StopLoop(AudioSource src)
        {
            if (src == null) return;
            src.Stop();
            Object.Destroy(src.gameObject);
        }

        public static void Play2D(string name, float volume = 1f, float pitch = 1f)
        {
            EnsureBuilt();
            if (!_bank.TryGetValue(name, out var clips) || clips.Length == 0) return;
            var clip = clips[Random.Range(0, clips.Length)];
            var src = _pool[_poolIndex++ % _pool.Count];
            if (src == null) { _root = null; EnsureBuilt(); src = _pool[_poolIndex++ % _pool.Count]; }
            src.pitch = pitch;
            src.volume = volume * VolumeFor(name);
            src.spatialBlend = 0f;
            src.clip = clip;
            src.Play();
        }

        // ------------------------------------------------------------------------------------
        // Synthesis
        // ------------------------------------------------------------------------------------
        private static void Build()
        {
            _bank["swing"] = Variants(2, i => Whoosh(0.14f + i * 0.03f, seed: 300 + (ulong)i));
            _bank["chisel_ring"] = Variants(3, i => Ring(2900f + i * 350f, 0.22f, seed: 310 + (ulong)i));
            _bank["tension"] = Variants(2, i => Tension(0.55f + i * 0.15f, seed: 320 + (ulong)i));
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
            _bank["loupe_up"] = Variants(1, i => Chime(2600f, 0.12f, seed: 210, noise: 0.15f));
            _bank["shop_bell"] = Variants(2, i => Chime(1975f + i * 120f, 0.9f, seed: 220 + (ulong)i, second: 2637f));
            _bank["counter_bell"] = Variants(1, i => Chime(2350f, 0.7f, seed: 230, second: 3100f));
            _bank["register_beep"] = Variants(1, i => Chime(1760f, 0.09f, seed: 240, noise: 0.05f));
            _bank["register"] = Variants(1, i => Register(seed: 250));
            _bank["loupe_down"] = Variants(1, i => Impact(0.08f, 700f, 0.3f, 0.05f, 0.5f, 6f, seed: 211));
            // tapping a rock in the hand: a solid nodule thuds, a hollow shell rings (knock_0 solid .. knock_2 hollow)
            _bank["knock_0"] = Variants(2, i => Knock(0f, seed: 260 + (ulong)i));
            _bank["knock_1"] = Variants(2, i => Knock(0.5f, seed: 264 + (ulong)i));
            _bank["knock_2"] = Variants(2, i => Knock(1f, seed: 268 + (ulong)i));
            _bank["scrub"] = Variants(3, i => Scrub(0.32f + i * 0.04f, seed: 270 + (ulong)i));
            _bank["splash"] = Variants(2, i => Splash(seed: 280 + (ulong)i));
            // lapidary saw: motor loop, blade-in-stone grind loop, clamp clack, the released piece dropping, the cut-through ring
            _bank["saw_motor"] = new[] { LoopClip(Motor(seed: 400)) };
            _bank["saw_grind"] = new[] { LoopClip(Grind(seed: 410)) };
            _bank["clamp"] = Variants(2, i => Impact(0.14f, 640f + i * 60f, 0.7f, 0.1f, 0.35f, 4f, seed: 420 + (ulong)i));
            _bank["cut_through"] = Variants(1, i => CutThrough(seed: 430));
            _bank["coolant_hiss"] = new[] { LoopClip(Hiss(seed: 435)) };
            _bank["slab_place"] = Variants(2, i => Impact(0.16f, 900f + i * 120f, 0.5f, 0.06f, 0.7f, 5f, seed: 436 + (ulong)i));
            // the music bed: four bars of slow, detuned pads (a synthesised loop the MusicPlayer crossfades in and out)
            _bank["music_calm"] = new[] { LoopClip(Pad(seed: 500, root: 110f, bright: 0.25f)) };
            _bank["music_work"] = new[] { LoopClip(Pad(seed: 510, root: 130.8f, bright: 0.45f)) };
            _bank["lap_motor"] = new[] { LoopClip(Motor(seed: 440, hum: 150f, whine: 0.35f)) };
            _bank["lap_contact"] = new[] { LoopClip(Grind(seed: 450, fine: true)) };
            _bank["ambience"] = new[] { Ambience(seed: 300) };
        }

        private static AudioClip LoopClip(float[] data)
        {
            var clip = AudioClip.Create("loop", data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>A small motor: mains hum, a belt whine and bearing noise. Loop-safe (whole periods of the hum).</summary>
        private static float[] Motor(ulong seed, float hum = 100f, float whine = 0.55f)
        {
            float duration = 1.0f;
            int n = (int)(duration * SampleRate);
            var d = new float[n];
            var rng = new SeededRandom(seed);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float noise = rng.NextFloat() * 2f - 1f;
                lp += (noise - lp) * 0.08f;
                float h = Mathf.Sin(2f * Mathf.PI * hum * t) * 0.35f + Mathf.Sin(2f * Mathf.PI * hum * 2f * t) * 0.15f;
                float w = Mathf.Sin(2f * Mathf.PI * hum * 11f * t + Mathf.Sin(t * 6f) * 0.3f) * whine * 0.18f;
                d[i] = Mathf.Clamp((h + w + lp * 0.9f) * 0.55f, -1f, 1f);
            }
            return d;
        }

        /// <summary>Diamond blade in stone: a wide hiss with a gritty rumble underneath. Loop-safe.</summary>
        /// <summary>
        /// A slow pad: two detuned sines an octave apart on a four-chord cycle (i, VI, III, VII of the minor root),
        /// low-passed noise breath under it, eight seconds a chord, seamless. Deliberately quiet and featureless: it
        /// sits under the workshop instead of playing at the player.
        /// </summary>
        private static float[] Pad(ulong seed, float root, float bright)
        {
            float chordSeconds = 8f;
            int chords = 4;
            int n = (int)(chordSeconds * chords * SampleRate);
            var d = new float[n];
            var rng = new SeededRandom(seed);
            float[] degrees = { 1f, 1.5f, 1.2f, 1.7818f };            // root, fifth, minor third, minor seventh (as chord roots)
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                int c = Mathf.Min(chords - 1, (int)(t / chordSeconds));
                float within = (t - c * chordSeconds) / chordSeconds;
                float env = Mathf.Sin(within * Mathf.PI);            // each chord swells and fades
                float f = root * degrees[c];
                float v = 0f;
                v += Mathf.Sin(2f * Mathf.PI * f * t) * 0.5f;
                v += Mathf.Sin(2f * Mathf.PI * f * 1.003f * t) * 0.35f;           // detune
                v += Mathf.Sin(2f * Mathf.PI * f * 1.5f * t) * 0.22f;             // fifth
                v += Mathf.Sin(2f * Mathf.PI * f * 2f * t + Mathf.Sin(t * 0.7f) * 0.5f) * 0.18f * bright;
                float noise = rng.NextFloat() * 2f - 1f;
                lp += (noise - lp) * 0.01f;
                v += lp * 0.6f;
                d[i] = Mathf.Clamp(v * env * 0.22f, -1f, 1f);
            }
            return d;
        }

        /// <summary>Coolant on a spinning blade: a soft filtered hiss with a fine drip patter.</summary>
        private static float[] Hiss(ulong seed)
        {
            float duration = 1.0f;
            int n = (int)(duration * SampleRate);
            var d = new float[n];
            var rng = new SeededRandom(seed);
            float lp = 0f, hp = 0f, prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float noise = rng.NextFloat() * 2f - 1f;
                lp += (noise - lp) * 0.35f;
                hp = lp - prev; prev = lp;
                float drip = rng.NextFloat() < 0.004f ? (rng.NextFloat() - 0.5f) * 0.9f : 0f;
                d[i] = Mathf.Clamp((hp * 1.6f + lp * 0.25f + drip) * 0.5f, -1f, 1f);
            }
            int x = 2000;
            for (int i = 0; i < x; i++) { float a = i / (float)x; int j = n - x + i; d[j] = d[j] * (1f - a) + d[i] * a; }
            return d;
        }

        private static float[] Grind(ulong seed, bool fine = false)
        {
            float duration = 1.0f;
            int n = (int)(duration * SampleRate);
            var d = new float[n];
            var rng = new SeededRandom(seed);
            float lp = 0f, lp2 = 0f, bp = 0f;
            for (int i = 0; i < n; i++)
            {
                float noise = rng.NextFloat() * 2f - 1f;
                lp += (noise - lp) * (fine ? 0.5f : 0.3f);
                lp2 += (lp - lp2) * 0.6f;
                bp += (noise - bp) * 0.02f;
                float grit = rng.NextFloat() < (fine ? 0.02f : 0.05f) ? noise * 0.6f : 0f;
                float v = lp2 * 1.4f + (fine ? 0f : bp * 2.2f) + grit;
                d[i] = Mathf.Clamp(v * 0.5f, -1f, 1f);
            }
            // seamless: crossfade the last 2000 samples into the first
            int x = 2000;
            for (int i = 0; i < x; i++) { float a = i / (float)x; int j = n - x + i; d[j] = d[j] * (1f - a) + d[i] * a; }
            return d;
        }

        /// <summary>The blade breaks through: the load drops with a short ring and the halves knock.</summary>
        private static float[] CutThrough(ulong seed)
        {
            float duration = 0.7f;
            int n = (int)(duration * SampleRate);
            var d = new float[n];
            var rng = new SeededRandom(seed);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float noise = rng.NextFloat() * 2f - 1f;
                lp += (noise - lp) * 0.35f;
                float ring = Mathf.Sin(2f * Mathf.PI * 3400f * t) * Mathf.Exp(-t * 18f) * 0.35f + Mathf.Sin(2f * Mathf.PI * 5100f * t) * Mathf.Exp(-t * 30f) * 0.15f;
                float knock = t > 0.22f ? Mathf.Sin(2f * Mathf.PI * 380f * (t - 0.22f)) * Mathf.Exp(-(t - 0.22f) * 40f) * 0.7f + lp * Mathf.Exp(-(t - 0.22f) * 60f) * 0.5f : 0f;
                d[i] = Mathf.Clamp(ring + knock, -1f, 1f);
            }
            return d;
        }

        /// <summary>Knuckle on stone. A hollow shell has a ringing body mode and a slow decay; a solid one is a dull, short thud.</summary>
        private static float[] Knock(float hollow, ulong seed)
        {
            float duration = Mathf.Lerp(0.16f, 0.42f, hollow);
            int n = (int)(duration * SampleRate);
            var d = new float[n];
            var rng = new SeededRandom(seed);
            float f1 = Mathf.Lerp(420f, 980f, hollow), f2 = f1 * Mathf.Lerp(1.9f, 2.4f, hollow);
            float decay = Mathf.Lerp(38f, 9f, hollow);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float noise = rng.NextFloat() * 2f - 1f;
                lp += (noise - lp) * 0.4f;
                float click = lp * Mathf.Exp(-t * 120f) * 0.9f;
                float body = Mathf.Sin(2f * Mathf.PI * f1 * t) * Mathf.Exp(-t * decay) * Mathf.Lerp(0.5f, 0.75f, hollow);
                float ring = Mathf.Sin(2f * Mathf.PI * f2 * t) * Mathf.Exp(-t * decay * 1.6f) * 0.25f * hollow;
                float thump = Mathf.Sin(2f * Mathf.PI * 110f * t) * Mathf.Exp(-t * 30f) * Mathf.Lerp(0.45f, 0.1f, hollow);
                d[i] = Mathf.Clamp((click + body + ring + thump) * 0.8f, -1f, 1f);
            }
            return d;
        }

        /// <summary>One stroke of a stiff brush over wet stone.</summary>
        private static float[] Scrub(float duration, ulong seed)
        {
            int n = (int)(duration * SampleRate);
            var d = new float[n];
            var rng = new SeededRandom(seed);
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float x = t / duration;
                float noise = rng.NextFloat() * 2f - 1f;
                lp += (noise - lp) * 0.45f;
                lp2 += (lp - lp2) * 0.6f;
                float env = Mathf.Pow(Mathf.Sin(x * Mathf.PI), 1.4f);
                float bristles = rng.NextFloat() < 0.06f ? noise * 0.5f : 0f;
                d[i] = Mathf.Clamp((lp2 * 1.6f + bristles) * env * 0.5f, -1f, 1f);
            }
            return d;
        }

        /// <summary>A small splash and drip.</summary>
        private static float[] Splash(ulong seed)
        {
            float duration = 0.5f;
            int n = (int)(duration * SampleRate);
            var d = new float[n];
            var rng = new SeededRandom(seed);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float noise = rng.NextFloat() * 2f - 1f;
                lp += (noise - lp) * 0.2f;
                float burst = lp * Mathf.Exp(-t * 14f) * 1.4f;
                float drips = 0f;
                if (t > 0.12f && rng.NextFloat() < 0.0025f) drips = 0.7f;
                float dripTone = drips * Mathf.Sin(2f * Mathf.PI * rng.Range(1200f, 2400f) * t);
                d[i] = Mathf.Clamp((burst + dripTone) * 0.6f, -1f, 1f);
            }
            return d;
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

        /// <summary>Hammer through air: a short band-limited noise swell that peaks just before contact.</summary>
        private static float[] Whoosh(float duration, ulong seed)
        {
            int n = (int)(duration * SampleRate);
            var d = new float[n];
            var rng = new SeededRandom(seed);
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float x = t / duration;
                float env = Mathf.Pow(Mathf.Sin(x * Mathf.PI), 2.2f) * (0.4f + 0.6f * x);
                float noise = rng.NextFloat() * 2f - 1f;
                float cutoff = 0.08f + 0.18f * x;              // opens up as the head accelerates
                lp += (noise - lp) * cutoff;
                lp2 += (lp - lp2) * cutoff;
                d[i] = Mathf.Clamp(lp2 * 3.2f * env, -1f, 1f);
            }
            return d;
        }

        /// <summary>Steel on steel: the chisel cap's inharmonic ring layered on top of the stone thud.</summary>
        private static float[] Ring(float freq, float duration, ulong seed)
        {
            int n = (int)(duration * SampleRate);
            var d = new float[n];
            var rng = new SeededRandom(seed);
            float f2 = freq * 1.41f, f3 = freq * 2.27f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 16f) * Mathf.Min(1f, t * 3000f);
                float v = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * f2 * t) * 0.3f * Mathf.Exp(-t * 10f) + Mathf.Sin(2f * Mathf.PI * f3 * t) * 0.2f * Mathf.Exp(-t * 26f);
                v += (rng.NextFloat() * 2f - 1f) * 0.05f * Mathf.Exp(-t * 60f);
                d[i] = Mathf.Clamp(v * env * 0.55f, -1f, 1f);
            }
            return d;
        }

        /// <summary>Near-break groan: a low, slowly wobbling grind that only a mostly-cracked shell makes.</summary>
        private static float[] Tension(float duration, ulong seed)
        {
            int n = (int)(duration * SampleRate);
            var d = new float[n];
            var rng = new SeededRandom(seed);
            float phase = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float f = 62f + Mathf.Sin(t * 9f) * 8f + rng.NextFloat() * 4f;
                phase += f / SampleRate;
                float saw = (phase - Mathf.Floor(phase)) * 2f - 1f;
                float grain = saw * 0.35f + Mathf.Sin(phase * 2f * Mathf.PI * 2f) * 0.2f;
                float crackle = rng.NextFloat() < 0.002f ? (rng.NextFloat() * 2f - 1f) : 0f;
                lp += (grain - lp) * 0.25f;
                float env = Mathf.Sin(t / duration * Mathf.PI) * (0.8f + 0.2f * Mathf.Sin(t * 31f));
                d[i] = Mathf.Clamp((lp + crackle * 0.6f) * env * 0.6f, -1f, 1f);
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

        /// <summary>Cash register: a mechanical clack, the drawer sliding open on its rails, a bell.</summary>
        private static float[] Register(ulong seed)
        {
            float duration = 0.75f;
            int n = (int)(duration * SampleRate);
            var d = new float[n];
            var rng = new SeededRandom(seed);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float noise = rng.NextFloat() * 2f - 1f;
                lp += (noise - lp) * 0.3f;
                float clack = Mathf.Exp(-t * 90f) * noise * 0.9f + Mathf.Sin(2f * Mathf.PI * 520f * t) * Mathf.Exp(-t * 40f) * 0.5f;
                float slide = t > 0.08f && t < 0.32f ? lp * 0.25f * Mathf.Sin((t - 0.08f) / 0.24f * Mathf.PI) : 0f;
                float stop = t > 0.3f ? Mathf.Exp(-(t - 0.3f) * 60f) * noise * 0.5f : 0f;
                float bell = t > 0.34f ? (Mathf.Sin(2f * Mathf.PI * 2093f * (t - 0.34f)) * 0.35f + Mathf.Sin(2f * Mathf.PI * 3136f * (t - 0.34f)) * 0.15f) * Mathf.Exp(-(t - 0.34f) * 7f) : 0f;
                d[i] = Mathf.Clamp(clack + slide + stop + bell, -1f, 1f);
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
