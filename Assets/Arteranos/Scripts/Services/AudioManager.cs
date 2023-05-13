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
    public class AudioManager : MonoBehaviour
    {
        public static ChatroomAgentV2 ChatroomAgent { get; private set; }

        public static float VolumeMaster {
            get => GetVolume("Master");
            set => SetVolume("Master", value);
        }
        public static float VolumeVoice {
            get => GetVolume("Voice");
            set => SetVolume("Voice", value);
        }
        public static float VolumeEnv {
            get => GetVolume("Env");
            set => SetVolume("Env", value);
        }

        private bool serverActive = false;

        private IEnumerator cs_cr = null;

        private static AudioMixer mixer = null;

        private void Awake() => mixer = Resources.Load<AudioMixer>("Audio/AudioMixer");

        private void Start()
        {
            PullVolumeSettings();

            ChatroomAgent = new(
                UVTelepathyNetwork.New(SettingsManager.Server.VoicePort),
                UVMicInput.New(GetDeviceId(), 24000),
                new UVAudioOutput.Factory());

            cs_cr = ManageChatServer();

            StartCoroutine(cs_cr);
        }

        private void OnDestroy()
        {
            if(cs_cr != null)
                StopCoroutine(cs_cr);
        }

        public static void RenewMic() => UVMicInput.Renew(GetDeviceId(), 24000);

        public static int? GetDeviceId()
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

        public static void PullVolumeSettings()
        {
            ClientAudioSettingsJSON audioSettings = SettingsManager.Client.AudioSettings;
            VolumeEnv = audioSettings.EnvVolume;
            VolumeMaster = audioSettings.MasterVolume;
            VolumeVoice = audioSettings.VoiceVolume;
        }

        public static void PushVolumeSettings()
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

