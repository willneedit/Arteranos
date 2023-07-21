/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

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
        bool MuteSelf { get; set; }
        Audio.IVoiceInput MicInput { get; }

        event Action<short> OnJoinedChatroom;
        event Action<float[]> OnSampleReady;

        int? GetDeviceId();
        void JoinChatroom(object data = null);
        void LeaveChatroom(object data = null);
        void MuteOther(short peerID, bool muted);
        bool MuteOther(short peerID);
        void PushVolumeSettings();
        void RenewMic();
    }
}
