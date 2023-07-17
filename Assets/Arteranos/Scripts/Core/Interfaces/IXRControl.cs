/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Avatar;
using System;
using UnityEngine;

#pragma warning disable IDE1006 // Because Unity's more relaxed naming convention

namespace Arteranos.XR
{
    public interface IXRControl
    {
        IAvatarBrain Me { get; set; }
        Vector3 CameraLocalOffset { get; }
        bool UsingXR { get; }
        bool enabled { get; set; }
        public float EyeHeight { get; set; }
        public float BodyHeight { get; set; }
        GameObject gameObject { get; }
        Transform rigTransform { get; }
        Transform cameraTransform { get; }
        Vector3 heightAdjustment { get; }

        public void ReconfigureXRRig();
        void FreezeControls(bool value);
        void MoveRig();

        event Action<bool> XRSwitchEvent;
    }
}
