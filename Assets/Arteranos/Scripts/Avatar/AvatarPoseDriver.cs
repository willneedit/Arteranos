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
        private Transform Controller_LeftHand = null;
        private Transform Controller_RightHand = null;
        private Transform Handle_LeftHand = null;
        private Transform Handle_RightHand = null;
        private IAvatarMeasures AvatarMeasures = null;
        private NetworkPose m_Poser = null;

        // Transpose controller rotations to avatar body rotations
        public readonly Quaternion LhrOffset = Quaternion.Euler(0, 90, 90);
        public readonly Quaternion RhrOffset = Quaternion.Euler(0, -90, -90);

        private AvatarBrain AvatarBrain = null;
        public bool isOwned => AvatarBrain?.isOwned ?? false;

        private void Awake()
        {
            m_Poser = GetComponent<NetworkPose>();
            AvatarBrain = GetComponent<AvatarBrain>();
        }

        private void Start()
        {
            if(isOwned)
            {
                SettingsManager.Client.OnVRModeChanged += OnXRChanged;
                OnXRChanged(SettingsManager.Client.VRMode);
            }

        }

        private void OnDestroy()
        {
            if(isOwned)
                SettingsManager.Client.OnVRModeChanged -= OnXRChanged;
        }

        private void OnXRChanged(bool useXR)
        {
            Transform xrot = XRControl.Instance.rigTransform;

            if(useXR)
            {
                // In VR, connect the VR hand controllers to the puppet's hands strings.
                Controller_LeftHand = xrot.FindRecursive("LeftHand Controller");
                Controller_RightHand = xrot.FindRecursive("RightHand Controller");
            }
            else
            {
                // In 2D, just use the default pose and leave it be.
                ResetPose(true, true);
            }

            // And, move the XR (or 2D) rig to the own avatar's position.
            Debug.Log("Moving rig");
            xrot.transform.SetPositionAndRotation(transform.position, transform.rotation);
            Physics.SyncTransforms();
        }

        public void UpdateAvatarMeasures(IAvatarMeasures am)
        {
            m_Poser.UploadJointNames(am.Avatar.transform, am.JointNames.ToArray());
            AvatarMeasures = am;

            Handle_LeftHand = am.Avatar.transform.FindRecursive($"Handle_{am.LeftHand.name}");
            Handle_RightHand = am.Avatar.transform.FindRecursive($"Handle_{am.RightHand.name}");

            ResetPose(true, true);

            if (isOwned)
            {
                IXRControl xrc = XRControl.Instance;

                xrc.EyeHeight = am.EyeHeight;
                xrc.BodyHeight = am.FullHeight;

                xrc.ReconfigureXRRig();
            }
        }

        public void ResetPose(bool leftHand, bool rightHand)
        {
            if (leftHand)
            {
                Vector3 idle_lh = new(-0.4f, 0, 0);
                Quaternion idle_rlh = Quaternion.Euler(180, -90, 0);
                Handle_LeftHand?.SetLocalPositionAndRotation(idle_lh, idle_rlh);
            }

            if (rightHand)
            {
                Vector3 idle_rh = new(0.4f, 0, 0);
                Quaternion idle_rrh = Quaternion.Euler(180, 90, 0);
                Handle_RightHand?.SetLocalPositionAndRotation(idle_rh, idle_rrh);
            }

        }

        // --------------------------------------------------------------------
        #region Pose updating

        public void UpdateOwnPose()
        {
            // VR: Hand and head tracking
            if(SettingsManager.Client.VRMode)
            {
                if(AvatarMeasures.CenterEye == null) return;

                Transform cam = XRControl.Instance.cameraTransform;
                ControlSettingsJSON ccs = SettingsManager.Client.Controls;

                Vector3 cEyeOffset = AvatarMeasures.CenterEye.position -
                    cam.position;

                // If the respective controllers are disabled, reset their hand poses
                // and bypass the tracking.
                ResetPose(!ccs.Controller_left, !ccs.Controller_right);

                if(Controller_LeftHand && Handle_LeftHand && ccs.Controller_left)
                {
                    Handle_LeftHand.SetPositionAndRotation(
                            Controller_LeftHand.position + cEyeOffset,
                            Controller_LeftHand.rotation * LhrOffset);
                }

                if(Controller_RightHand && Handle_RightHand && ccs.Controller_right)
                {
                    Handle_RightHand.SetPositionAndRotation(
                            Controller_RightHand.position + cEyeOffset,
                            Controller_RightHand.rotation * RhrOffset);
                }

                if(AvatarMeasures.Head)
                    AvatarMeasures.Head.rotation = cam.rotation;
            }

            // VR + 2D: Walking animation (only with loaded avatars)
            GameObject xro = XRControl.Instance.rigTransform.gameObject;
            CharacterController cc = xro.GetComponent<CharacterController>();
            Animator anim = GetComponentInChildren<Animator>();

            if(anim != null)
            {
                // The direction...
                Vector3 moveSpeed = Quaternion.Inverse(transform.rotation) * cc.velocity;

                int frontBack = 0;
                if (moveSpeed.z < -0.5f) frontBack = -1;
                if (moveSpeed.z >  0.5f) frontBack = 1;

                int leftRight = 0;
                if (moveSpeed.x < -0.5f) leftRight = -1;
                if (moveSpeed.x >  0.5f) leftRight = 1;

                anim.SetInteger("IntWalkFrontBack", frontBack);
                anim.SetInteger("IntWalkLeftRight", leftRight);

                // ... and smaller people have to walk in a quicker pace.
                anim.SetFloat("Speed", (float)(AvatarMeasures.UnscaledHeight / AvatarMeasures.FullHeight));
            }
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
                UpdateOwnPose();
            }

            RouteVoice();
        }
    }
}
