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

        public void Awake() => syncDirection = SyncDirection.ServerToClient;

        public override void OnStartClient()
        {
            base.OnStartClient();

            m_AvatarData = GetComponent<IAvatarLoader>();
            m_Poser = GetComponent<NetworkPose>();

            if(isOwned)
            {
                XRControl.Instance.XRSwitchEvent += OnXRChanged;
                OnXRChanged(XRControl.Instance.UsingXR);
            }

        }

        public override void OnStopClient()
        {
            if(isOwned)
                XRControl.Instance.XRSwitchEvent -= OnXRChanged;

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
        public void UploadJointNames(Transform rootTransform, string[] names) => m_Poser.UploadJointNames(rootTransform, names);

        // --------------------------------------------------------------------
        #region Pose updating

        private void AdjustFootIK(Transform foot)
        {
            // Everything except Layers 17 and 18 (BubbleFriend and BubbleStranger)
            int layerMask = ~((1 << 17) | (1 << 18));

            // If the avatar is a midget, he cannot lift his feet half a meter up, so sacle down accordingly.
            float maxLiftKnees = 0.50f * (m_AvatarData.OriginalFullHeight / m_AvatarData.FullHeight);

            Ray ray = new(foot.position + Vector3.up * maxLiftKnees, Vector3.down);

            if(Physics.SphereCast(ray, m_AvatarData.FootElevation, out RaycastHit hitInfo, 0.50f, layerMask))
            {
                foot.SetPositionAndRotation(hitInfo.point + Vector3.up * m_AvatarData.FootElevation,
                    Quaternion.FromToRotation(Vector3.up, hitInfo.normal) * foot.rotation);
            }
        }

        public void UpdateOwnPose()
        {
            // VR: Hand and head tracking
            if(XRControl.Instance.UsingXR)
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
                Vector3 moveSpeed = Quaternion.Inverse(transform.rotation) * cc.velocity;
                anim.SetFloat("Walking", moveSpeed.z);
            }
        }

        public void LateUpdate()
        {
            // VR + 2D: Feet IK (only with feet, of course)

            // Edge case: Client disconnected between the Update()'s and LateUpdate().
            if(m_AvatarData?.LeftFoot)
                AdjustFootIK(m_AvatarData.LeftFoot);

            if(m_AvatarData?.RightFoot)
                AdjustFootIK(m_AvatarData.RightFoot);
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
