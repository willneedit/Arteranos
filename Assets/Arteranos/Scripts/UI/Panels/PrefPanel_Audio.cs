/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Arteranos.Core;
using Arteranos.Services;

namespace Arteranos.UI
{
    public class PrefPanel_Audio : UIBehaviour
    {
        public NumberedSlider sld_MasterVolume = null;
        public NumberedSlider sld_VoiceVolume = null;
        public NumberedSlider sld_EnvVolume = null;
        public Spinner spn_InputDevice = null;
        public NumberedSlider sld_MicInputGain = null;
        public Spinner spn_AGC = null;
        public Image img_Microphone = null;

        private Client cs = null;
        private bool dirty = false;
        private bool needsRenew = false;

        protected override void Awake()
        {
            base.Awake();

            List<string> devices = new()
            {
                "Default Device"
            };

            foreach(string device in Microphone.devices)
                devices.Add(device);

            spn_InputDevice.Options = devices.ToArray();
            spn_InputDevice.value = (AudioManager.GetDeviceId() ?? -1) + 1;

            sld_MasterVolume.OnValueChanged += OnMasterVolumeChanged;
            sld_VoiceVolume.OnValueChanged += OnVoiceVolumeChanged;
            sld_EnvVolume.OnValueChanged += OnEnvVolumeChanged;
            spn_InputDevice.OnChanged += OnInputDeviceChanged;
            sld_MicInputGain.OnValueChanged += OnMicInputGainChanged;
            spn_AGC.OnChanged += OnAGCChanged;

            AudioManager.OnSampleReady += TapMicrophoneInput;
        }


        protected override void Start()
        {
            base.Start();

            cs = SettingsManager.Client;

            // Reset the state as it's the initial state, not the blank slate.

            sld_MasterVolume.value = AudioManager.VolumeMaster;
            sld_VoiceVolume.value = AudioManager.VolumeVoice;
            sld_EnvVolume.value = AudioManager.VolumeEnv;

            sld_MicInputGain.value = cs.AudioSettings.MicInputGain;
            spn_AGC.value = cs.AudioSettings.AGCLevel;

            dirty = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // Might be to disabled before it's really started, so cs may be null yet.
            if(dirty)
            {
                AudioManager.PushVolumeSettings();

                cs.AudioSettings.MicInputGain = sld_MicInputGain.value;
                cs.AudioSettings.AGCLevel = spn_AGC.value;

                cs?.Save();
                if(needsRenew) AudioManager.RenewMic();
            }

            dirty = false;
            needsRenew= false;
        }

        protected override void OnDestroy()
        {
            AudioManager.OnSampleReady -= TapMicrophoneInput;

            base.OnDestroy();
        }

        private void Update()
        {
            Color MicColor = Color.black;

            float chargePercent = charge * 20.0f;
            if(chargePercent < 0.8f)
                // Black to Green from 0 to 80% amplitude
                MicColor = Color.Lerp(Color.black, Color.green, chargePercent * (1.00f / 0.80f));
            else
                // Green to red from 80% to 100% amplitude
                MicColor = Color.Lerp(Color.green, Color.red, (chargePercent - 0.80f) * (1.00f / 0.20f));

            img_Microphone.color = MicColor;
        }

        private void OnMasterVolumeChanged(float val)
        {
            AudioManager.VolumeMaster = val;
            dirty = true;
        }

        private void OnVoiceVolumeChanged(float val)
        {
            AudioManager.VolumeVoice = val;
            dirty = true;
        }

        private void OnEnvVolumeChanged(float val)
        {
            AudioManager.VolumeEnv = val;
            dirty = true;
        }

        private void OnInputDeviceChanged(int item, bool up)
        {
            cs.AudioSettings.InputDevice =
                (item == 0) ? null : Microphone.devices[item - 1];
            dirty = true;
            needsRenew = true;
        }

        private void OnMicInputGainChanged(float val)
        {
            AudioManager.MicGain = Utils.LoudnessToFactor(val);
            dirty = true;
        }

        private void OnAGCChanged(int item, bool up)
        {
            AudioManager.MicAGCLevel = spn_AGC.value;
            dirty = true;
        }

        private float charge = 0;
        private void TapMicrophoneInput(float[] samples)
        {
            foreach(float sample in samples)
                // Utils.CalcVU(sample, ref charge, 0.9f, 0.25e-05f);
                Utils.CalcVU(sample, ref charge, 0.1f, 0.001f);

        }

    }
}
