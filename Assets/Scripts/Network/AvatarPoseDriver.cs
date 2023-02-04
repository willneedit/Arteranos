using UnityEngine;
using UnityEngine.Rendering;
using System;
using Unity.XR.CoreUtils;

using Mirror;

using Arteranos.ExtensionMethods;
using Arteranos.XR;
using Arteranos.NetworkTypes;
using Unity.VisualScripting;
using System.Collections.Generic;

namespace Arteranos.NetworkIO
{

    public class AvatarPoseDriver : NetworkBehaviour
    {
        public XROrigin m_Origin;
        public XRControl m_Controller;
        public Transform m_LeftHand;
        public Transform m_RightHand;
        public Transform m_Camera;

        private bool m_usingXR;

        public Transform[] m_JointTransforms = new Transform[SyncPose.MAX_JOINTS];
        public string[] m_JointNames = new string[SyncPose.MAX_JOINTS];

        public readonly SyncPose m_Joint = new SyncPose();

        private IAvatarLoader m_AvatarData = null;

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
                m_Origin = GetComponent<XROrigin>();
                m_Controller = FindObjectOfType<XRControl>();
                m_Controller.m_XRSwitchEvent.AddListener(OnXRChanged);
                OnXRChanged(m_Controller.m_UsingXR);
            }

        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            if(isOwned)
                m_Controller.m_XRSwitchEvent.RemoveListener(OnXRChanged);
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
            if(m_usingXR)
            {
                if (m_AvatarData.CenterEye == null) return;

                // FIXME: Only intereested to the y offset.
                Vector3 cEyeOffset = m_AvatarData.CenterEye.position - m_Camera.position;

                if (m_LeftHand && m_AvatarData.LeftHand)
                    m_AvatarData.LeftHand.SetPositionAndRotation(
                            m_LeftHand.position + cEyeOffset,
                            m_LeftHand.rotation * m_AvatarData.LhrOffset);

                if (m_RightHand && m_AvatarData.RightHand)
                    m_AvatarData.RightHand.SetPositionAndRotation(
                            m_RightHand.position + cEyeOffset,
                            m_RightHand.rotation * m_AvatarData.RhrOffset);
            }

            // ...but, the IK has to be made it real in the course of the next frame.


            // Pack the pose changes in the puppet...
            ushort mask = 0;
            List<NetworkRotation> lnr = new List<NetworkRotation>();

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
            m_usingXR = useXR;
            m_Origin = FindObjectOfType<XROrigin>();

            if (useXR)
            {
                // In VR, connect the VR hand controllers to the puppet's hands strings.
                m_LeftHand = m_Origin.transform.FindRecursive("LeftHand Controller");
                m_RightHand = m_Origin.transform.FindRecursive("RightHand Controller");
                m_Camera = m_Origin.transform.FindRecursive("_AvatarView");
            }
            else
            {
                // In 2D, just use the default pose and leave it be.
                m_AvatarData.ResetPose();
            }

            // And, move the XR (or 2D) rig to the own avatar's position.
            Debug.Log("Moving rig");
            m_Origin.transform.SetPositionAndRotation(transform.position, transform.rotation);
            Physics.SyncTransforms();
        }

        void Update()
        {
            if(isOwned)
            {
                // FIXME: laggy due to IK update.
                UpdateOwnPose();

                Camera cam = m_Origin.Camera;

                Vector3 playSpace = cam.transform.localPosition - m_Controller.m_CameraLocalOffset;

                // Own avatar get copied from the controller, alien avatars by NetworkTransform.
                transform.SetPositionAndRotation(m_Origin.transform.position + 
                    m_Origin.transform.rotation * playSpace,
                    m_Origin.transform.rotation);
            } 
            else
            {
                UpdateAlienPose();
            }

        }
    }
}
