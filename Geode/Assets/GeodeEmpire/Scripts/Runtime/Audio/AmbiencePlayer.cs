using UnityEngine;
using GeodeEmpire.Core;

namespace GeodeEmpire.Audio
{
    /// <summary>Loops the synthesised room tone at the ambience volume.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class AmbiencePlayer : MonoBehaviour
    {
        private AudioSource _src;

        private void Start()
        {
            _src = GetComponent<AudioSource>();
            _src.clip = WorkshopAudio.GetClip("ambience");
            _src.loop = true;
            _src.spatialBlend = 0f;
            Apply();
            _src.Play();
            GameSettings.Changed += Apply;
        }

        private void OnDestroy() => GameSettings.Changed -= Apply;

        private void Apply()
        {
            if (_src != null) _src.volume = 0.5f * GameSettings.Current.AmbienceVolume;
        }
    }
}
