/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using UnityEngine;
using UnityEngine.Rendering;


namespace Arteranos.XR
{
    public class XRVisualConfigurator : MonoBehaviour
    {
        // The required speed (1m/s) to fully close the shutters (or rather to fully use the effects)
        [SerializeField] private float VelocityMax = 1.0f;

        // 1/4th of a second to close/open the shutters
        [SerializeField] private float TweenSpeed = 4.0f;

        private Vector3 pos = Vector3.zero;
        private float BlindersMaxValue = 0.0f;

        private float TweenedD = 0.0f;

        private Volume vol;

        void Start()
        {
            vol = GetComponentInChildren<Volume>();

            SettingsManager.Client.OnXRControllerChanged += DownloadControlSettings;
            SettingsManager.Client.PingXRControllersChanged();
        }

        void Update()
        {
            float d;
        
            Transform t = Camera.main != null ? Camera.main.transform : null;

            if(t == null) return;

            d = Vector3.Magnitude(t.position - pos) / Time.deltaTime;
            d = Mathf.Clamp01(d / VelocityMax);
            pos = t.position;

            if(d > TweenedD) TweenedD += TweenSpeed * Time.deltaTime;
            if(d < TweenedD) TweenedD -= TweenSpeed * Time.deltaTime;
            TweenedD = Mathf.Clamp01(TweenedD);

            // Too much hassle to pick out the vignette. Just use the whole global volume.
            if(vol != null) vol.weight = TweenedD * BlindersMaxValue;
        }

        private void OnDestroy() => SettingsManager.Client.OnXRControllerChanged += DownloadControlSettings;

        private void DownloadControlSettings(ControlSettingsJSON ccs, MovementSettingsJSON mcs)
        {
            if(mcs == null) return;

            switch(mcs.ComfortBlinders)
            {
                case ComfortBlindersType.Off:
                    BlindersMaxValue = 0.0f;
                    break;
                case ComfortBlindersType.Low:
                    BlindersMaxValue = 0.33f;
                    break;
                case ComfortBlindersType.Medium:
                    BlindersMaxValue = 0.66f;
                    break;
                case ComfortBlindersType.High:
                    BlindersMaxValue = 1.0f;
                    break;
            }
        }
    }
}
