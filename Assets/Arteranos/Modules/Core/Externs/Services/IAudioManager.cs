/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Audio;
using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Arteranos.Services
{
    public interface IAudioManager : IMonoBehaviour
    {
        int ChannelCount { get; }
        int MicAGCLevel { get; set; }
        float MicGain { get; set; }
        IVoiceInput MicInput { get; set; }
        AudioMixerGroup MixerGroupEnv { get; }
        AudioMixerGroup MixerGroupVoice { get; }
        int SampleRate { get; }
        float VolumeEnv { get; set; }
        float VolumeMaster { get; set; }
        float VolumeVoice { get; set; }

        event Action<float[]> OnSampleReady;
        event Action<int, byte[]> OnSegmentReady;

        int? GetDeviceId();
        IVoiceOutput GetVoiceOutput(int SampleRate, int ChannelCount);
        void PushVolumeSettings();
        void RenewMic();
    }
}