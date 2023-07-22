/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Audio;
using System;
using UnityEngine.Audio;

#pragma warning disable IDE1006 // Because Unity's more relaxed naming convention

namespace Arteranos.Services
{
    public interface IAudioManager
    {
        AudioMixerGroup MixerGroupEnv { get; }
        AudioMixerGroup MixerGroupVoice { get; }
        float VolumeEnv { get; set; }
        float VolumeMaster { get; set; }
        float VolumeVoice { get; set; }
        bool enabled { get; set; }
        float MicGain { get; set; }
        int MicAGCLevel { get; set; }
        Audio.IVoiceInput MicInput { get; }
        int ChannelCount { get; }
        int SampleRate { get; }

        event Action<float[]> OnSampleReady;
        event Action<int, byte[]> OnSegmentReady;

        int? GetDeviceId();
        IVoiceOutput GetVoiceOutput(int SampleRate, int ChannelCount);
        void PushVolumeSettings();
        void RenewMic();
    }
}
