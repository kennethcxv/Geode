using UnityEngine;
using GeodeEmpire.Core;

namespace GeodeEmpire.Audio
{
    /// <summary>
    /// A restrained music bed: one calm pad in free roam, a slightly brighter one while a station is in use, faded
    /// under reveals; mixed at the music volume, always below the tools. Synthesised, seeded, no assets.
    /// </summary>
    public sealed class MusicPlayer : MonoBehaviour
    {
        public static MusicPlayer Instance { get; private set; }
        public float Intensity { get; private set; }
        private AudioSource _calm, _work;
        private float _duck = 1f, _duckUntil;

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start()
        {
            _calm = Make("music_calm"); _work = Make("music_work");
            _calm.Play(); _work.Play();
        }

        private AudioSource Make(string bank)
        {
            var go = new GameObject("Music_" + bank);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.clip = WorkshopAudio.GetClip(bank);
            src.loop = true; src.spatialBlend = 0f; src.volume = 0f; src.priority = 200;
            return src;
        }

        /// <summary>A reveal or a letter: drop the bed for a moment so the sting reads.</summary>
        public void Duck(float seconds) { _duckUntil = Time.time + seconds; }

        private void Update()
        {
            var s = GameSettings.Current;
            float master = s != null ? s.MasterVolume * s.MusicVolume : 0.4f;
            bool station = false;
            var bench = FindAnyObjectByType<Cracking.CrackingBench>(); if (bench != null && bench.Active) station = true;
            var saw = FindAnyObjectByType<Lapidary.SawStation>(); if (saw != null && saw.Active) station = true;
            var cracker = FindAnyObjectByType<Cracking.CrackerStation>(); if (cracker != null && cracker.Active) station = true;
            float want = station ? 1f : 0f;
            Intensity = Mathf.MoveTowards(Intensity, want, Time.deltaTime * 0.35f);
            _duck = Mathf.MoveTowards(_duck, Time.time < _duckUntil ? 0.25f : 1f, Time.deltaTime * 1.5f);
            float menu = CursorController.InMenu ? 0.6f : 1f;
            if (_calm != null) _calm.volume = master * 0.55f * (1f - Intensity * 0.7f) * _duck * menu;
            if (_work != null) _work.volume = master * 0.5f * Intensity * _duck * menu;
        }
    }
}
