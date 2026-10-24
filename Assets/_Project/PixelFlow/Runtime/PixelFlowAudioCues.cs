using System;
using System.Collections.Generic;
using Dreamy.Audio;
using UnityEngine;

namespace PixelFlow
{
    [CreateAssetMenu(menuName = "Pixel Flow/Audio Cues")]
    public sealed class PixelFlowAudioCues : ScriptableObject
    {
        [Serializable] public sealed class SoundCue { public AudioClip Source; public float GainHint = .55f; public SoundAudioFile File; }
        public DreamyAudioProfile Profile;
        public MusicAudioFile MenuMusic, GameMusic;
        public List<SoundCue> Sounds = new();
        public SoundAudioFile Resolve(AudioClip clip, float gainHint) => Sounds.Find(x => x.Source == clip && Mathf.Approximately(x.GainHint, gainHint))?.File;
    }
}
