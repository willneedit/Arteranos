/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using Unity.XR.CoreUtils;

using Mirror;

using Arteranos.ExtensionMethods;
using Arteranos.XR;

namespace Arteranos.NetworkIO
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

            m_AvatarData = GetComponent<AvatarLoader_RPM>();
            m_Poser = GetComponent<NetworkPose>();

            if(isOwned)
            {
                XRControl.XRSwitchEvent += OnXRChanged;
                OnXRChanged(XRControl.UsingXR);
            }

        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            if(isOwned)
                XRControl.XRSwitchEvent -= OnXRChanged;
        }

        /// <summary>
        /// Uploads the full set of the joint names. Both owner and alien avatars are
        /// synced by Replacer, so need not to sync here, too.
        /// </summary>
        /// <param name="names">Array of the joint (aka bone) names</param>
        public void UploadJointNames(string[] names) => m_Poser.UploadJointNames(names);

        private void AdjustFootIK(Transform foot)
        {
            // FIXME Elevation of the foot joint to the floor needed
            //       Currently hardcoded to 0.12f, but it has to be changed for
            //       a stiletto heels, for example.
            Ray ray = new(foot.position + Vector3.up * 0.5f, Vector3.down);
            if(Physics.SphereCast(ray, 0.12f, out RaycastHit hitInfo, 0.50f))
            {
                foot.SetPositionAndRotation(hitInfo.point + Vector3.up * 0.12f,
                    Quaternion.FromToRotation(Vector3.up, hitInfo.normal) * foot.rotation);
            }
        }

        public void UpdateOwnPose()
        {
            // VR: Hand and head tracking
            if(XRControl.UsingXR)
            {
                if(m_AvatarData.CenterEye == null) return;

                Transform cam = XRControl.CurrentVRRig.Camera.transform;

                Vector3 cEyeOffset = m_AvatarData.CenterEye.position -
                    cam.position;

                if(LeftHand && m_AvatarData.LeftHand)
                {
                    m_AvatarData.LeftHand.SetPositionAndRotation(
                            LeftHand.position + cEyeOffset,
                            LeftHand.rotation * m_AvatarData.LhrOffset);
                }

                if(RightHand && m_AvatarData.RightHand)
                {
                    m_AvatarData.RightHand.SetPositionAndRotation(
                            RightHand.position + cEyeOffset,
                            RightHand.rotation * m_AvatarData.RhrOffset);
                }

                if(m_AvatarData.Head)
                    m_AvatarData.Head.rotation = cam.rotation;
            }

            // VR + 2D: Walking animation (only with loaded avatars)
            XROrigin xro = XRControl.CurrentVRRig;
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
            if(m_AvatarData.LeftFoot)
                AdjustFootIK(m_AvatarData.LeftFoot);

            if(m_AvatarData.RightFoot)
                AdjustFootIK(m_AvatarData.RightFoot);
        }

        public void UpdateAlienPose()
        {
            // Locomotion: NetworkTransform.
            // Pose: NetworkPose.

            // Maybe TODO: Face morphing and hand morphing for trigger/grip usage
        }

        void OnXRChanged(bool useXR)
        {
            Transform xrot = XRControl.CurrentVRRig.transform;

            if (useXR)
            {
                // In VR, connect the VR hand controllers to the puppet's hands strings.
                LeftHand = xrot.FindRecursive("LeftHand Controller");
                RightHand = xrot.FindRecursive("RightHand Controller");
            }
            else
            {
                // In 2D, just use the default pose and leave it be.
                m_AvatarData.ResetPose();
            }

            // And, move the XR (or 2D) rig to the own avatar's position.
            Debug.Log("Moving rig");
            xrot.transform.SetPositionAndRotation(transform.position, transform.rotation);
            Physics.SyncTransforms();
        }

        void Update()
        {
            if(isOwned)
            {
                XROrigin xro = XRControl.CurrentVRRig;
                Camera cam = xro.Camera;

                Vector3 playSpace = cam.transform.localPosition - XRControl.CameraLocalOffset;

                // Own avatar get copied from the controller, alien avatars by NetworkTransform.
                transform.SetPositionAndRotation(xro.transform.position + 
                    xro.transform.rotation * playSpace,
                    xro.transform.rotation);

                // Needs to update the pose AFTER the position and rotation because
                // of a nasty flickering.
                UpdateOwnPose();
            }
            else
            {
                UpdateAlienPose();
            }

        }
    }
}
