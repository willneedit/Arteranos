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
using Mirror;

namespace Arteranos.Avatar
{

    public class AvatarPoseDriver : NetworkBehaviour
    {
        private IAvatarMeasures AvatarMeasures = null;
        private NetworkPose NetworkPose = null;

        // Base animation directives, synchronized.
        [SyncVar]
        public Vector2 animMoveDirection = Vector2.zero;

        [SyncVar]
        public float animMoveSpeed = 1.0f;


        private void Awake()
        {
            NetworkPose = GetComponent<NetworkPose>();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if(isLocalPlayer)
            {
                SettingsManager.Client.OnVRModeChanged += OnXRChanged;
                OnXRChanged(SettingsManager.Client.VRMode);
            }

        }

        public override void OnStopClient()
        {
            if(isLocalPlayer)
                SettingsManager.Client.OnVRModeChanged -= OnXRChanged;

            base.OnStopClient();
        }

        protected virtual void OnValidate()
        {
            syncDirection = SyncDirection.ClientToServer;
            syncInterval = 0;
        }

        private void OnXRChanged(bool useXR)
        {
            Transform xrot = G.XRControl.rigTransform;

            // And, move the XR (or 2D) rig to the own avatar's position.
            Debug.Log("Moving rig");
            xrot.transform.SetPositionAndRotation(transform.position, transform.rotation);
            Physics.SyncTransforms();
        }

        public void UpdateAvatarMeasures(IAvatarMeasures am)
        {
            NetworkPose.UploadJointNames(am.Avatar.transform, am.JointNames.ToArray());
            AvatarMeasures = am;

            if (isLocalPlayer)
            {
                IXRControl xrc = G.XRControl;

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
                if(!AvatarMeasures.CenterEye || !AvatarMeasures.Head) return;

                Transform cam = G.XRControl.cameraTransform;                
                AvatarMeasures.Head.rotation = cam.rotation;
            }

            // VR + 2D: Walking animation (only with loaded avatars)
            GameObject xro = G.XRControl.rigTransform.gameObject;
            CharacterController cc = xro.GetComponent<CharacterController>();

            Vector3 moveSpeed = Quaternion.Inverse(transform.rotation) * cc.velocity;

            Vector2 newMoveDirection = Vector2.zero;

            if (moveSpeed.z < -0.5f) newMoveDirection.y = -1;
            if (moveSpeed.z > 0.5f) newMoveDirection.y = 1;

            if (moveSpeed.x < -0.5f) newMoveDirection.x = -1;
            if (moveSpeed.x > 0.5f) newMoveDirection.x = 1;

            animMoveDirection = newMoveDirection;

            animMoveSpeed = AvatarMeasures != null
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
            if(isLocalPlayer)
            {
                IXRControl instance = G.XRControl;
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
