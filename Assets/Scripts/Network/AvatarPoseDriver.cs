using UnityEngine;
using System;
using Unity.XR.CoreUtils;

using Mirror;

using Arteranos.ExtensionMethods;
using Arteranos.XR;
using Arteranos.NetworkTypes;
using System.Collections.Generic;

namespace Arteranos.NetworkIO
{

    public class AvatarPoseDriver : NetworkBehaviour
    {
        public Transform m_LeftHand;
        public Transform m_RightHand;

        public Transform[] m_JointTransforms = new Transform[SyncPose.MAX_JOINTS];
        public string[] m_JointNames = new string[SyncPose.MAX_JOINTS];

        public readonly SyncPose m_Joint = new();

        private IAvatarLoader m_AvatarData = null;

        public Vector3 m_Debug_Velocity;

        public void Awake()
        {
            syncDirection = SyncDirection.ServerToClient;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            m_AvatarData = GetComponent<AvatarLoader_RPM>();
            if(isOwned)
            {
                XRControl.XRSwitchEvent.AddListener(OnXRChanged);
                OnXRChanged(XRControl.UsingXR);
            }

        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            if(isOwned)
                XRControl.XRSwitchEvent.RemoveListener(OnXRChanged);
        }

        /// <summary>
        /// Uploads the full set of the joint names. Both owner and alien avatars are
        /// synced by Replacer, so need not to sync here, too.
        /// </summary>
        /// <param name="names">Array of the joint (aka bone) names</param>
        public void UploadJointNames(string[] names)
        {
            Debug.Log($"UploadJointNames: {netIdentity.netId}");
            if(names.Length > SyncPose.MAX_JOINTS)
                throw new ArgumentOutOfRangeException($"{name.Length} exceeds {SyncPose.MAX_JOINTS}");

            for (int i1 = 0; i1 < SyncPose.MAX_JOINTS; i1++)
            {
                m_JointNames[i1] = String.Empty;
                m_JointTransforms[i1] = null;
            }

            for(int i2 = 0; i2 < names.Length; i2++)
            {
                m_JointNames[i2] = names[i2];
                if((m_JointTransforms[i2] = transform.FindRecursive(names[i2])) == null)
                    throw new ArgumentException($"Mismatch in skeleton: Nonexistent bone '{names[i2]}' in the loaded avatar");
            }
        }

        [Command]
        public void SendToOwnPose(ushort mask, List<NetworkRotation> pack)
        {
            for(int i = 0, j = 0; i<SyncPose.MAX_JOINTS; i++)
            {
                if((ushort)(mask & (1 << i)) != 0)
                    m_Joint[i] = pack[j++];
            }
        }

        public void UpdateOwnPose()
        {
            // VR: pull the strings of the puppet handles...
            if(XRControl.UsingXR)
            {
                if (m_AvatarData.CenterEye == null) return;

                Transform cam = XRControl.CurrentVRRig.Camera.transform;

                Vector3 cEyeOffset = m_AvatarData.CenterEye.position - 
                    cam.position;

                if (m_LeftHand && m_AvatarData.LeftHand)
                    m_AvatarData.LeftHand.SetPositionAndRotation(
                            m_LeftHand.position + cEyeOffset,
                            m_LeftHand.rotation * m_AvatarData.LhrOffset);

                if (m_RightHand && m_AvatarData.RightHand)
                    m_AvatarData.RightHand.SetPositionAndRotation(
                            m_RightHand.position + cEyeOffset,
                            m_RightHand.rotation * m_AvatarData.RhrOffset);

                if(m_AvatarData.Head)
                    m_AvatarData.Head.rotation = cam.rotation;
            }

            // Pack the pose changes in the puppet...
            ushort mask = 0;
            List<NetworkRotation> lnr = new();

            for (int i = 0; i < SyncPose.MAX_JOINTS; i++)
            {
                if (m_JointTransforms[i] != null)
                {
                    NetworkRotation nr = m_JointTransforms[i].localRotation.ToNetworkRotation();
                    if(m_Joint[i] != nr)
                    {
                        m_Joint[i] = nr;
                        lnr.Add(m_Joint[i]);
                        mask = (ushort)(mask | (1 << i));
                    }
                }
            }

            XROrigin xro = XRControl.CurrentVRRig;
            CharacterController cc = xro.GetComponent<CharacterController>();
            Animator anim = GetComponentInChildren<Animator>();

            if(anim != null)
            {
                Vector3 moveSpeed = m_Debug_Velocity =  Quaternion.Inverse(transform.rotation) * cc.velocity;
                anim.SetFloat("Walking", moveSpeed.z);
            }

            // ... and propagate it to the others to view it.
            SendToOwnPose(mask, lnr);
        }

        public void UpdateAlienPose()
        {
            for (int i = 0; i < SyncPose.MAX_JOINTS; i++)
                if (m_JointTransforms[i] != null)
                    m_JointTransforms[i].localRotation = m_Joint[i].ToQuaternion();
        }

        void OnXRChanged(bool useXR)
        {
            Transform xrot = XRControl.CurrentVRRig.transform;

            if (useXR)
            {
                // In VR, connect the VR hand controllers to the puppet's hands strings.
                m_LeftHand = xrot.FindRecursive("LeftHand Controller");
                m_RightHand = xrot.FindRecursive("RightHand Controller");
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
