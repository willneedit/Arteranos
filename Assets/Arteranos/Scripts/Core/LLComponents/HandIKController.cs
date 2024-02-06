/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

using Arteranos.Core;
using Arteranos.XR;

namespace Arteranos.Avatar
{
    public class HandIKController : MonoBehaviour
    {
        public Transform rootTransform = null;
        public IAvatarMeasures AvatarMeasures = null;
        public bool RightSide = false;

        private Transform guidedTransform = null;
        private Transform ControllerTransform = null;
        private Transform CameraTransform = null;

        private Quaternion HandRotationOffset = Quaternion.identity;

        private void Start()
        {
            guidedTransform = RightSide
                ? AvatarMeasures.RightHand 
                : AvatarMeasures.LeftHand;

            HandRotationOffset = RightSide
                ? Quaternion.Euler(0, -90, -90)
                : Quaternion.Euler(0, 90, 90);

            SettingsManager.Client.OnVRModeChanged += OnXRChanged;
            OnXRChanged(SettingsManager.Client.VRMode);
        }

        private void OnDestroy()
        {
            SettingsManager.Client.OnVRModeChanged -= OnXRChanged;
        }

        private void OnXRChanged(bool useXR)
        {
            if(useXR)
            {
                Transform xrot = XRControl.Instance.rigTransform;

                ControllerTransform = xrot.FindRecursive(RightSide
                    ? "RightHand Controller"
                    : "LeftHand Controller");

                CameraTransform = XRControl.Instance.cameraTransform;
            }
            else
            {
                ControllerTransform = null;
                CameraTransform = null;
            }
        }

        public void LateUpdate()
            => AdjustHandIK(transform);

        private void AdjustHandIK(Transform handHandle)
        {
            if (!ControllerTransform)
            {
                // No controller. Set the hand's pose along with its animation.
                handHandle.SetPositionAndRotation(guidedTransform.position, guidedTransform.rotation);
                return;
            }

            ControlSettingsJSON ccs = SettingsManager.Client.Controls;

            bool enabled = RightSide
                ? ccs.Controller_right
                : ccs.Controller_left;

            if (enabled)
            {
                // Override the hand pose with the controller's position and rotation
                Vector3 cEyeOffset = AvatarMeasures.CenterEye.position -
                    CameraTransform.position;

                handHandle.SetPositionAndRotation(
                    ControllerTransform.position + cEyeOffset,
                    ControllerTransform.rotation * HandRotationOffset
                    );
            }
            else
            {
                // Set the actual hand's position as-is.
                handHandle.SetPositionAndRotation(guidedTransform.position, guidedTransform.rotation);
            }
        }
    }
}