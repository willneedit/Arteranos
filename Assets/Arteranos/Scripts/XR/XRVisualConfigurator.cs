/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arteranos.XR
{
    public class XRVisualConfigurator : MonoBehaviour
    {
        private Vector3 pos = Vector3.zero;

        void Start()
        {
            SettingsManager.Client.OnXRControllerChanged += DownloadControlSettings;
            DownloadControlSettings();
        }

        void Update()
        {
            Transform t = XRControl.Me?.gameObject.transform;

            if(t == null) return;

            float d = Vector3.SqrMagnitude(t.position - pos);
        }

        private void OnDestroy()
        {
            SettingsManager.Client.OnXRControllerChanged += DownloadControlSettings;
        }

        private void DownloadControlSettings()
        {
            MovementSettingsJSON mcs = SettingsManager.Client?.Movement;

            if(mcs == null) return;

        }
    }
}
