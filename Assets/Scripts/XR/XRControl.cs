using Arteranos.ExtensionMethods;
using System;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Management;

namespace Arteranos.XR
{
    [Serializable]
    public class ev_XRSwitch : UnityEvent<bool> { }


    public class XRControl : MonoBehaviour
    {
        public XROrigin VRRig;
        public XROrigin NoVRRig;

        public bool m_UsingXR { get; private set; }

        private Core.SettingsManager m_SettingsManager = null;

        public ev_XRSwitch m_XRSwitchEvent { get; private set; } = new ev_XRSwitch();

        public float m_EyeHeight { get; set; }

        public float m_BodyHeight { get; set; }

        public Vector3 m_CameraLocalOffset { get; private set; }

        public IEnumerator StartXRCoroutine()
        {
            Debug.Log("Initializing XR...");
            yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

            if (XRGeneralSettings.Instance.Manager.activeLoader == null)
            {
                Debug.LogError("Initializing XR Failed. Check Editor or Player log for details.");
                m_SettingsManager.m_Client.VRMode = false;
            }
            else
            {
                Debug.Log("Starting XR...");
                XRGeneralSettings.Instance.Manager.StartSubsystems();
                UpdateXROrigin(true);
            }
        }

        void StopXR()
        {
            Debug.Log("Stopping XR...");

            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            Debug.Log("XR stopped completely.");
            UpdateXROrigin(false);
        }
        
        // Start is called before the first frame update
        void Start()
        {
            m_SettingsManager = GetComponent<Core.SettingsManager>();
            m_SettingsManager.m_Client.OnVRModeChanged += OnVRModeChanged;
            if(m_SettingsManager.m_Client.VRMode)
                OnVRModeChanged(false, true);
            else
                UpdateXROrigin(false);
        }

        void OnVRModeChanged(bool old, bool current)
        {
            if(current)
                StartCoroutine(StartXRCoroutine());
            else
                StopXR();
        }

        void UpdateXROrigin(bool useVR)
        {
            XROrigin oldXROigin = FindObjectOfType<XROrigin>();

            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;

            if(oldXROigin != null)
            {
                position = oldXROigin.transform.position;
                rotation = oldXROigin.transform.rotation;

                Destroy(oldXROigin);
            }

            XROrigin xro = Instantiate(useVR ? VRRig : NoVRRig, position, rotation);
            ReconfigureXRRig(xro);
            m_XRSwitchEvent.Invoke(useVR);
            m_UsingXR = useVR;
        }

        // Cleanly shut down XR on exit.
        void OnApplicationQuit()
        {
            // Debug.Log("Bye!");
            if(m_SettingsManager.m_Client.VRMode)
                StopXR();
        }

        /// <summary>
        /// Reconfigure the XR rig to the new dimensions if we're switched
        /// from VR to 2D mode or switched avatars.
        /// </summary>
        /// <param name="xro">The new or xurrent XROrigin</param>
        public void ReconfigureXRRig(XROrigin xro = null)
        {
            if(xro == null)
                xro = FindObjectOfType<XROrigin>();

            Camera cam = xro.Camera;
            GameObject offsetObject = xro.CameraFloorOffsetObject;

            m_CameraLocalOffset = cam.transform.localPosition;

            xro.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.NotSpecified;
            xro.CameraYOffset = m_EyeHeight - m_CameraLocalOffset.y;
            offsetObject.transform.localPosition = new Vector3(0, m_EyeHeight, 0.2f) - m_CameraLocalOffset;

            CharacterController cc = xro.GetComponent<CharacterController>();
            cc.height = m_BodyHeight;
            cc.center = new Vector3(0, m_BodyHeight / 2, 0);

        }

    }
}
