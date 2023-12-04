/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Audio;
using UnityEngine;
using Arteranos.Core;
using System;
using UnityEngine.Audio;

namespace Arteranos.Services
{
    public class AudioManagerImpl : AudioManager
    {
        protected override IVoiceInput MicInput_ { get; set; }

        protected override event Action<int, byte[]> OnSegmentReady_
        {
            add => MicInput_.OnSegmentReady += value;
            remove => MicInput_.OnSegmentReady -= value;
        }

        protected override event Action<float[]> OnSampleReady_
        {
            add => MicInput_.OnSampleReady += value;
            remove => MicInput_.OnSampleReady -= value;
        }

        protected override AudioMixerGroup MixerGroupVoice_ => mixer.FindMatchingGroups("Master/Voice")[0];
        protected override AudioMixerGroup MixerGroupEnv_ => mixer.FindMatchingGroups("Master/Environment")[0];

        protected override float VolumeMaster_
        {
            get => GetVolume("Master");
            set => SetVolume("Master", value);
        }
        protected override float VolumeVoice_
        {
            get => GetVolume("Voice");
            set => SetVolume("Voice", value);
        }
        protected override float VolumeEnv_
        {
            get => GetVolume("Env");
            set => SetVolume("Env", value);
        }

        protected override float MicGain_
        {
            get => MicInput_.Gain;
            set => MicInput_.Gain = value;
        }

        protected override int MicAGCLevel_
        {
            get => micAGCLevel;
            set
            {
                micAGCLevel = value;
                MicInput_.SetAGCLevel(value);
            }
        }

        protected override int ChannelCount_ => MicInput_.ChannelCount;

        protected override int SampleRate_ => MicInput_.SampleRate;

        private int micAGCLevel;

        private static AudioMixer mixer = null;

        private void Awake()
        {
            Instance = this;
            mixer = Resources.Load<AudioMixer>("Audio/AudioMixer");
        }

        private void OnDestroy() => Instance = null;

        private void Start()
        {
            MicInput_ = MicInput.New(GetDeviceId_(), 24000);

            PullVolumeSettings();
        }

        protected override void RenewMic_() => MicInput.Renew(GetDeviceId_(), 24000);

        protected override int? GetDeviceId_()
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
            VolumeEnv_ = audioSettings.EnvVolume;
            VolumeMaster_ = audioSettings.MasterVolume;
            VolumeVoice_ = audioSettings.VoiceVolume;

            MicInput_.Gain = Utils.LoudnessToFactor(audioSettings.MicInputGain);
            MicInput_.SetAGCLevel(audioSettings.AGCLevel);
        }

        protected override void PushVolumeSettings_()
        {
            ClientAudioSettingsJSON audioSettings = SettingsManager.Client.AudioSettings;
            audioSettings.MasterVolume = VolumeMaster_;
            audioSettings.VoiceVolume= VolumeVoice_;
            audioSettings.EnvVolume = VolumeEnv_;
        }

        protected override IVoiceOutput GetVoiceOutput_(int SampleRate, int ChannelCount) 
            => AudioOutput.New(SampleRate, ChannelCount, MixerGroupVoice_);
    }
}

