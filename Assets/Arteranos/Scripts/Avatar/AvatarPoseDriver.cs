/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

using Mirror;

using Arteranos.NetworkIO;
using Arteranos.XR;
using Arteranos.Core;

namespace Arteranos.Avatar
{

    public class AvatarPoseDriver : NetworkBehaviour
    {
        private Transform LeftHand = null;
        private Transform RightHand = null;
        private IAvatarLoader m_AvatarData = null;
        private NetworkPose m_Poser = null;

        public void Awake()
        {
            m_AvatarData = GetComponent<IAvatarLoader>();
            m_Poser = GetComponent<NetworkPose>();

            syncDirection = SyncDirection.ServerToClient;
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

            if(useXR)
            {
                // In VR, connect the VR hand controllers to the puppet's hands strings.
                LeftHand = xrot.FindRecursive("LeftHand Controller");
                RightHand = xrot.FindRecursive("RightHand Controller");
            }
            else
            {
                // In 2D, just use the default pose and leave it be.
                m_AvatarData.ResetPose(true, true);
            }

            // And, move the XR (or 2D) rig to the own avatar's position.
            Debug.Log("Moving rig");
            xrot.transform.SetPositionAndRotation(transform.position, transform.rotation);
            Physics.SyncTransforms();
        }

        /// <summary>
        /// Uploads the full set of the joint names. Both owner and alien avatars are
        /// synced by Replacer, so need not to sync here, too.
        /// </summary>
        /// <param name="rootTransform"></param>
        /// <param name="names">Array of the joint (aka bone) names</param>
        public void UploadJointNames(Transform rootTransform, string[] names) 
            => m_Poser.UploadJointNames(rootTransform, names);

        // --------------------------------------------------------------------
        #region Pose updating

        public void UpdateOwnPose()
        {
            // VR: Hand and head tracking
            if(SettingsManager.Client.VRMode)
            {
                if(m_AvatarData.CenterEye == null) return;

                Transform cam = XRControl.Instance.cameraTransform;
                ControlSettingsJSON ccs = SettingsManager.Client.Controls;

                Vector3 cEyeOffset = m_AvatarData.CenterEye.position -
                    cam.position;

                // If the respective controllers are disabled, reset their hand poses
                // and bypass the tracking.
                m_AvatarData.ResetPose(!ccs.Controller_left, !ccs.Controller_right);

                if(LeftHand && m_AvatarData.LeftHand && ccs.Controller_left)
                {
                    m_AvatarData.LeftHand.SetPositionAndRotation(
                            LeftHand.position + cEyeOffset,
                            LeftHand.rotation * m_AvatarData.LhrOffset);
                }

                if(RightHand && m_AvatarData.RightHand && ccs.Controller_right)
                {
                    m_AvatarData.RightHand.SetPositionAndRotation(
                            RightHand.position + cEyeOffset,
                            RightHand.rotation * m_AvatarData.RhrOffset);
                }

                if(m_AvatarData.Head)
                    m_AvatarData.Head.rotation = cam.rotation;
            }

            // VR + 2D: Walking animation (only with loaded avatars)
            GameObject xro = XRControl.Instance.rigTransform.gameObject;
            CharacterController cc = xro.GetComponent<CharacterController>();
            Animator anim = GetComponentInChildren<Animator>();

            if(anim != null)
            {
                // The direction...
                Vector3 moveSpeed = Quaternion.Inverse(transform.rotation) * cc.velocity;

                // ... and smaller people has to walk in a quicker pace.
                float speedScale = m_AvatarData.OriginalFullHeight / m_AvatarData.FullHeight;

                anim.SetFloat("Walking", moveSpeed.z);
                anim.SetFloat("SpeedScale", speedScale);
            }
        }

        public void UpdateAlienPose()
        {
            // Locomotion: NetworkTransform.
            // Pose: NetworkPose.

            // Maybe TODO: Face morphing and hand morphing for trigger/grip usage
        }

        #endregion

        // --------------------------------------------------------------------
        #region Face/Voice morphing

        private void RouteVoice()
        {
            float amount = GetComponent<AvatarVoice>().MeasureAmplitude();

            m_AvatarData?.UpdateOpenMouth(amount);
        }

        #endregion

        void Update()
        {
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
            else
            {
                UpdateAlienPose();
            }

            RouteVoice();
        }
    }
}
