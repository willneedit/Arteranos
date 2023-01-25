using UnityEngine;
using UnityEngine.Rendering;
using System;
using Unity.XR.CoreUtils;

using Arteranos.ExtensionMethods;

using Mirror;

namespace Arteranos.NetworkIO
{

    public class AvatarPlayer : NetworkBehaviour
    {
        public XROrigin m_Controller;
        public Transform m_LeftHand;
        public Transform m_RightHand;
        public Transform m_Camera;

        private AvatarReplacer m_AvatarData = null;

        public override void OnStartClient()
        {
            m_AvatarData = FindObjectOfType<AvatarReplacer>();
        }

        void UpdatePose()
        {
            if(m_AvatarData.m_CenterEye == null) return;

            // FIXME: Only intereested to the y offset.
            Vector3 cEyeOffset = m_AvatarData.m_CenterEye.transform.position - m_Camera.position;

            // FIXME: Hand rotation, 90° on the local X.
            if(m_LeftHand && m_AvatarData.m_LeftHand)
                m_AvatarData.m_LeftHand.transform.SetPositionAndRotation(
                        m_LeftHand.position + cEyeOffset,
                        m_LeftHand.rotation);
            
            if(m_RightHand && m_RightHand)
                m_AvatarData.m_RightHand.transform.SetPositionAndRotation(
                        m_RightHand.position +cEyeOffset,
                        m_RightHand.rotation);
        }

        void Update()
        {
            if(!isOwned) return;

            // Could drop to null b/c VR/2D transition
            if (m_Controller == null)
            {
                m_Controller = FindObjectOfType<XROrigin>();
                m_LeftHand = m_Controller.transform.FindRecursive("LeftHand Controller");
                m_RightHand = m_Controller.transform.FindRecursive("RightHand Controller");
                m_Camera = m_Controller.transform.FindRecursive("_AvatarView");

                // FIXME: 2D controller, handedness
                if(m_RightHand == null)
                    m_Controller.transform.FindRecursive("OneHand Controller");
            }

            if (m_Controller == null)
                return;

            // FIXME: laggy due to IK update.
            UpdatePose();

            // Own avatar get copied from the controller, alien avatars by NetworkTransform.
            transform.SetPositionAndRotation(m_Controller.transform.position, m_Controller.transform.rotation);
            
        }
    }
}
