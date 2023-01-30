using UnityEngine;
using UnityEngine.Rendering;
using System;
using Unity.XR.CoreUtils;

using Mirror;

using Arteranos.ExtensionMethods;
using Arteranos.XR;
using Arteranos.NetworkTypes;
using Unity.VisualScripting;

namespace Arteranos.NetworkIO
{

    public class AvatarPlayer : NetworkBehaviour
    {
        public XROrigin m_Controller;
        public Transform m_LeftHand;
        public Transform m_RightHand;
        public Transform m_Camera;


        // TODO More complex SyncObject like SyncList for pose data
        public Transform[] m_JointTransforms = new Transform[SyncPose.MAX_JOINTS];
        public string[] m_JointNames = new string[SyncPose.MAX_JOINTS];

        public readonly SyncPose m_Joint = new SyncPose();

        private AvatarReplacer m_AvatarData = null;

        public void Awake()
        {
            syncDirection = SyncDirection.ClientToServer;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            m_AvatarData = FindObjectOfType<AvatarReplacer>();
            if(isOwned)
            {
                XRControl xrc = FindObjectOfType<XRControl>();
                xrc.m_XRSwitchEvent.AddListener(OnXRChanged);
                OnXRChanged(xrc.m_UsingXR);
            }

        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            if(isOwned)
            {
                XRControl xrc = FindObjectOfType<XRControl>();
                xrc.m_XRSwitchEvent.RemoveListener(OnXRChanged);
            }
        }

        /// <summary>
        /// Uploads the full set of the joint names. Both owner and alien avatars are
        /// synced by Replacer, so need not to sync here, too.
        /// </summary>
        /// <param name="names">Array of the joint (aka bone) names</param>
        public void UploadJointNames(string[] names)
        {
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

        public void UpdateOwnPose()
        {
            if(m_AvatarData.m_CenterEye == null) return;

            // FIXME: Only intereested to the y offset.
            Vector3 cEyeOffset = m_AvatarData.m_CenterEye.transform.position - m_Camera.position;

            // FIXME: Hand rotation, 90° on the local X.
            if(m_LeftHand && m_AvatarData.m_LeftHand)
                m_AvatarData.m_LeftHand.transform.SetPositionAndRotation(
                        m_LeftHand.position + cEyeOffset,
                        m_LeftHand.rotation);
            
            if(m_RightHand && m_AvatarData.m_RightHand)
                m_AvatarData.m_RightHand.transform.SetPositionAndRotation(
                        m_RightHand.position + cEyeOffset,
                        m_RightHand.rotation);

            for(int i = 0; i < SyncPose.MAX_JOINTS; i++)
                if (m_JointTransforms[i] != null)
                    m_Joint[i] = m_JointTransforms[i].localRotation.ToNetworkRotation();
        }

        public void UpdateAlienPose()
        {
            for (int i = 0; i < SyncPose.MAX_JOINTS; i++)
                if (m_JointTransforms[i] != null)
                    m_JointTransforms[i].localRotation = m_Joint[i].ToQuaternion();
        }

        void OnXRChanged(bool useXR)
        {
                m_Controller = FindObjectOfType<XROrigin>();
                m_LeftHand = m_Controller.transform.FindRecursive("LeftHand Controller");
                m_RightHand = m_Controller.transform.FindRecursive("RightHand Controller");
                m_Camera = m_Controller.transform.FindRecursive("_AvatarView");

                // FIXME: 2D controller, handedness
                if(m_RightHand == null)
                    m_RightHand = m_Controller.transform.FindRecursive("OneHand Controller");
        }

        void Update()
        {
            if(isOwned)
            {
                // FIXME: laggy due to IK update.
                UpdateOwnPose();

                // Own avatar get copied from the controller, alien avatars by NetworkTransform.
                transform.SetPositionAndRotation(m_Controller.transform.position, m_Controller.transform.rotation);
            } 
            else
            {
                UpdateAlienPose();
            }

        }
    }
}
