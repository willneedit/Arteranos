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
        private Animator anim = null;

        [SyncVar]
        public Vector2 animMoveDirection = Vector2.zero;

        [SyncVar]
        public float animMoveSpeed = 1.0f;

        private void Awake()
        {
            NetworkPose = GetComponent<NetworkPose>();
            syncDirection = SyncDirection.ClientToServer;
            syncInterval = 0;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if(isOwned)
            {
                SettingsManager.Client.OnVRModeChanged += OnXRChanged;
                OnXRChanged(SettingsManager.Client.VRMode);
            }

        }

        public override void OnStopClient()
        {
            if(isOwned)
                SettingsManager.Client.OnVRModeChanged -= OnXRChanged;

            base.OnStopClient();
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

            if (isOwned)
            {
                IXRControl xrc = XRControl.Instance;

                xrc.EyeHeight = am.EyeHeight;
                xrc.BodyHeight = am.FullHeight;

                xrc.ReconfigureXRRig();
            }

            // The user got a new body in general, even if it's not yet active.
            anim = GetComponentInChildren<Animator>(true);
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

            animMoveDirection = newMoveDirection;

            animMoveSpeed = AvatarMeasures != null
                ? (float)(AvatarMeasures.UnscaledHeight / AvatarMeasures.FullHeight)
                : 1.0f;
        }

        private void UpdateBaseAnimation()
        {
            if (anim == null) return;

            anim.SetInteger("IntWalkFrontBack", (int)animMoveDirection.y);
            anim.SetInteger("IntWalkLeftRight", (int)animMoveDirection.x);
            anim.SetFloat("Speed", animMoveSpeed);
        }

        #endregion

        // --------------------------------------------------------------------
        #region Face/Voice morphing

        private void RouteVoice()
        {
            float amount = GetComponent<AvatarVoice>().MeasureAmplitude();

            if(AvatarMeasures?.Avatar != null)
                AvatarMeasures.Avatar.GetComponent<AvatarMouthAnimator>().MouthOpen = amount;
        }

        #endregion

        void Update()
        {
            // Avatars from other clients are slaved by the NetworkTransform and -Pose.
            if(isOwned)
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

            // Both local and remote avatar have to set the animation direction into effect
            UpdateBaseAnimation();

            // Route the user's audio level into the mouth morphing
            RouteVoice();
        }
    }
}
