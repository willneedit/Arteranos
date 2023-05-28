/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.UniVoice;
using Arteranos.Audio;
using Mirror;
using System.Collections;
using UnityEngine;
using Arteranos.Core;
using System;
using UnityEngine.Audio;

namespace Arteranos.Services
{
    public class AudioManagerImpl : MonoBehaviour, IAudioManager
    {
        public ChatroomAgentV2 ChatroomAgent { get; private set; }
        public UVMicInput MicInput { get; private set; }

        public event Action<short> OnJoinedChatroom;
        public void JoinChatroom(object data = null) => ChatroomNetwork?.JoinChatroom(data);
        public void LeaveChatroom(object data = null) => ChatroomNetwork?.LeaveChatroom(data);
        public AudioMixerGroup MixerGroupVoice => mixer.FindMatchingGroups("Master/Voice")[0];
        public AudioMixerGroup MixerGroupEnv => mixer.FindMatchingGroups("Master/Environment")[0];

        public event Action<float[]> OnSampleReady 
        {
            add => MicInput.OnSampleReady += value;
            remove => MicInput.OnSampleReady -= value;
        }

        public float VolumeMaster
        {
            get => GetVolume("Master");
            set => SetVolume("Master", value);
        }
        public float VolumeVoice
        {
            get => GetVolume("Voice");
            set => SetVolume("Voice", value);
        }
        public float VolumeEnv
        {
            get => GetVolume("Env");
            set => SetVolume("Env", value);
        }

        public new bool enabled
        { 
            get => base.enabled;
            set => base.enabled = value; 
        }

        public float MicGain
        {
            get => MicInput.Gain;
            set => MicInput.Gain = value;
        }

        public int MicAGCLevel
        {
            get => micAGCLevel;
            set
            {
                micAGCLevel = value;
                MicInput.SetAGCLevel(value);
            }
        }

        public bool MuteSelf
        {
            get => ChatroomAgent.MuteSelf;
            set => ChatroomAgent.MuteSelf = value;
        }

        public void MuteOther(short peerID, bool muted) => ChatroomAgent.PeerSettings[peerID].muteThem = muted;
        public bool MuteOther(short peerID) => ChatroomAgent.PeerSettings[peerID].muteThem;

        private int micAGCLevel;

        private bool serverActive = false;
        private IEnumerator cs_cr = null;
        private static AudioMixer mixer = null;
        private static IChatroomNetworkV2 ChatroomNetwork { get; set; }

        private void Awake()
        {
            AudioManager.Instance = this;
            mixer = Resources.Load<AudioMixer>("Audio/AudioMixer");
        }

        private void OnDestroy()
        {
            if(cs_cr != null)
                StopCoroutine(cs_cr);
            AudioManager.Instance = null;
        }

        private void Start()
        {
            MicInput = UVMicInput.New(GetDeviceId(), 24000);

            ChatroomAgent = new(
                UVTelepathyNetwork.New(SettingsManager.Server.VoicePort),
                MicInput,
                new UVAudioOutput.Factory(MixerGroupVoice));

            ChatroomNetwork = ChatroomAgent.Network;

            ChatroomNetwork.OnJoinedChatroom += (x) => OnJoinedChatroom?.Invoke(x);

            PullVolumeSettings();

            cs_cr = ManageChatServer();

            StartCoroutine(cs_cr);
        }

        public void RenewMic() => UVMicInput.Renew(GetDeviceId(), 24000);

        public int? GetDeviceId()
        {
            string device = SettingsManager.Client.AudioSettings.InputDevice;
            int? deviceId = Array.IndexOf(Microphone.devices, device);
            deviceId = (deviceId < 0) ? null : deviceId;
            return deviceId;
        }
        private static void SetVolume(string group, float volume) => mixer.SetFloat($"Vol{group}", volume - 80.0f);

        private static float GetVolume(string group)
        {
            mixer.GetFloat($"Vol{group}", out float val);
            return val + 80.0f;
        }

        public void PullVolumeSettings()
        {
            ClientAudioSettingsJSON audioSettings = SettingsManager.Client.AudioSettings;
            VolumeEnv = audioSettings.EnvVolume;
            VolumeMaster = audioSettings.MasterVolume;
            VolumeVoice = audioSettings.VoiceVolume;

            MicInput.Gain = Core.Utils.LoudnessToFactor(audioSettings.MicInputGain);
            MicInput.SetAGCLevel(audioSettings.AGCLevel);
        }

        public void PushVolumeSettings()
        {
            ClientAudioSettingsJSON audioSettings = SettingsManager.Client.AudioSettings;
            audioSettings.MasterVolume = VolumeMaster;
            audioSettings.VoiceVolume= VolumeVoice;
            audioSettings.EnvVolume = VolumeEnv;
        }

        private IEnumerator ManageChatServer()
        {
            while(true)
            {
                yield return new WaitForSeconds(2);

                // Follow the voice server setup (or connection) state through the
                // world server's (or connection's) state.

                // Transition offline --> Server or Host
                if(NetworkServer.active && !serverActive)
                {
                    ChatroomAgent.Network.HostChatroom();
#if UNITY_SERVER
                    ChatroomAgent.MuteSelf = true;
#endif
                    serverActive = true;
                }
                // Transition Server or Host --> offline 
                else if(!NetworkServer.active && serverActive)
                {
                    ChatroomAgent.Network.CloseChatroom();
                    serverActive = false;
                }

            }
        }
    }
}

