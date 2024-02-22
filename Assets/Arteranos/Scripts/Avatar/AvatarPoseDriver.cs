/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

using Arteranos.NetworkIO;
using Arteranos.XR;
using Arteranos.Core;

namespace Arteranos.Avatar
{

    public class AvatarPoseDriver : MonoBehaviour
    {
        private IAvatarMeasures AvatarMeasures = null;
        private NetworkPose NetworkPose = null;

        private bool IsOwned => NetworkPose.isOwned;

        private void Awake()
        {
            NetworkPose = GetComponent<NetworkPose>();
        }

        private void Start()
        {
            if(IsOwned)
            {
                SettingsManager.Client.OnVRModeChanged += OnXRChanged;
                OnXRChanged(SettingsManager.Client.VRMode);
            }

        }

        private void OnDestroy()
        {
            if(IsOwned)
                SettingsManager.Client.OnVRModeChanged -= OnXRChanged;
        }

        private void OnXRChanged(bool useXR)
        {
            Transform xrot = XRControl.Instance.rigTransform;

            // And, move the XR (or 2D) rig to the own avatar's position.
            Debug.Log("Moving rig");
            xrot.transform.SetPositionAndRotation(transform.position, transform.rotation);
            Physics.SyncTransforms();
        }

        public void UpdateAvatarMeasures(IAvatarMeasures am)
        {
            NetworkPose.UploadJointNames(am.Avatar.transform, am.JointNames.ToArray());
            AvatarMeasures = am;

            if (IsOwned)
            {
                IXRControl xrc = XRControl.Instance;

                xrc.EyeHeight = am.EyeHeight;
                xrc.BodyHeight = am.FullHeight;

                xrc.ReconfigureXRRig();
            }
        }

        // --------------------------------------------------------------------
        #region Pose updating

        public void UpdateOwnPose()
        {
            // VR: Head tracking
            if(SettingsManager.Client.VRMode)
            {
                if(AvatarMeasures.CenterEye == null) return;

                Transform cam = XRControl.Instance.cameraTransform;

                if(AvatarMeasures.Head)
                    AvatarMeasures.Head.rotation = cam.rotation;
            }

            // VR + 2D: Walking animation (only with loaded avatars)
            GameObject xro = XRControl.Instance.rigTransform.gameObject;
            CharacterController cc = xro.GetComponent<CharacterController>();

            Vector3 moveSpeed = Quaternion.Inverse(transform.rotation) * cc.velocity;

            Vector2 newMoveDirection = Vector2.zero;

            if (moveSpeed.z < -0.5f) newMoveDirection.y = -1;
            if (moveSpeed.z > 0.5f) newMoveDirection.y = 1;

            if (moveSpeed.x < -0.5f) newMoveDirection.x = -1;
            if (moveSpeed.x > 0.5f) newMoveDirection.x = 1;

            NetworkPose.animMoveDirection = newMoveDirection;

            NetworkPose.animMoveSpeed = AvatarMeasures != null
                ? (float)(AvatarMeasures.UnscaledHeight / AvatarMeasures.FullHeight)
                : 1.0f;
        }

        #endregion

        // --------------------------------------------------------------------
        #region Face/Voice morphing

        private AvatarVoice AvatarVoice = null;
        private AvatarMouthAnimator AvatarMouthAnimator = null;

        private void RouteVoice()
        {
            if (!AvatarVoice)
                AvatarVoice = GetComponent<AvatarVoice>();

            if (!AvatarMouthAnimator && AvatarMeasures?.Avatar)
                AvatarMouthAnimator = AvatarMeasures.Avatar.GetComponent<AvatarMouthAnimator>();

            float amount = AvatarVoice.MeasureAmplitude();

            if(AvatarMeasures?.Avatar != null)
                AvatarMouthAnimator.MouthOpen = amount;
        }

        #endregion

        void Update()
        {
            // Avatars from other clients are slaved by the NetworkTransform and -Pose.
            if(IsOwned)
            {
                IXRControl instance = XRControl.Instance;
                Transform xro = instance.rigTransform;
                Transform cam = instance.cameraTransform;

                Vector3 playSpace = cam.localPosition - instance.CameraLocalOffset;

                // Own avatar get copied from the controller, alien avatars by NetworkTransform.
                transform.SetPositionAndRotation(xro.position + 
                    xro.rotation * playSpace,
                    xro.rotation);

                // Needs to update the pose AFTER the position and rotation because
                // of a nasty flickering.

                // Set and propagate the avatar baseline animation (e.g. walking)
                UpdateOwnPose();
            }

            // Route the user's audio level into the mouth morphing
            RouteVoice();
        }
    }
}
