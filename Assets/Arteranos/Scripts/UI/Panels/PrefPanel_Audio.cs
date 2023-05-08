/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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

        private ClientSettings cs = null;
        private bool dirty = false;
        private bool needsRenew = false;

        protected override void Awake()
        {
            base.Awake();

            List<string> devices = new();
            devices.Add("Default Device");

            foreach(string device in Microphone.devices)
                devices.Add(device);

            spn_InputDevice.Options = devices.ToArray();

            int? did = VoiceManager.GetDeviceId();
            spn_InputDevice.value = (did == null) ? 0 : (did.Value + 1);

            sld_MasterVolume.OnValueChanged += OnMasterVolumeChanged;
            sld_VoiceVolume.OnValueChanged += OnVoiceVolumeChanged;
            sld_EnvVolume.OnValueChanged += OnEnvVolumeChanged;
            spn_InputDevice.OnChanged += OnInputDeviceChanged;
            sld_MicInputGain.OnValueChanged += OnMicInputGainChanged;
            spn_AGC.OnChanged += OnAGCChanged;
        }

        protected override void Start()
        {
            base.Start();

            cs = SettingsManager.Client;

            // Reset the state as it's the initial state, not the blank slate.
            dirty = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // Might be to disabled before it's really started, so cs may be null yet.
            if(dirty)
            {
                cs?.SaveSettings();
                if(needsRenew) VoiceManager.RenewMic();
            }

            dirty = false;
        }

        private void OnMasterVolumeChanged(float val)
        {
            throw new NotImplementedException();
        }

        private void OnVoiceVolumeChanged(float val)
        {
            throw new NotImplementedException();
        }

        private void OnEnvVolumeChanged(float val)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        private void OnAGCChanged(int item, bool up)
        {
            throw new NotImplementedException();
        }
    }
}
