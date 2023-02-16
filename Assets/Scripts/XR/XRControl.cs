/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

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
        public static XRControl Singleton { get; private set; }
        public static XROrigin CurrentVRRig { get; private set; }
        public static ev_XRSwitch XRSwitchEvent { get; private set; } = new();
        public static bool UsingXR { get; private set; }
        public static Vector3 CameraLocalOffset { get; private set; }

        public XROrigin VRRig;
        public XROrigin NoVRRig;


        private Core.SettingsManager m_SettingsManager = null;

        public float m_EyeHeight { get; set; }

        public float m_BodyHeight { get; set; }


        public void Awake()
        {
            Singleton = this;
            CurrentVRRig = FindObjectOfType<XROrigin>();
        }

        public void OnDestroy()
        {
            Singleton = null;
        }

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
            CurrentVRRig = FindObjectOfType<XROrigin>();

            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;

            if(CurrentVRRig != null)
            {
                position = CurrentVRRig.transform.position;
                rotation = CurrentVRRig.transform.rotation;

                Destroy(CurrentVRRig);
            }

            CurrentVRRig = Instantiate(useVR ? VRRig : NoVRRig, position, rotation);
            ReconfigureXRRig();
            XRSwitchEvent.Invoke(useVR);
            UsingXR = useVR;
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
        public void ReconfigureXRRig()
        {
            Camera cam = CurrentVRRig.Camera;
            GameObject offsetObject = CurrentVRRig.CameraFloorOffsetObject;

            CameraLocalOffset = cam.transform.localPosition;

            // Oculus Quest 2's floor-to-eye adjustment is horribly lacking.
            // Even with in a seated position, the height measurement was off.
            // So, use the avatar's grounded standing eye height as the reference.
            CurrentVRRig.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.NotSpecified;
            CurrentVRRig.CameraYOffset = m_EyeHeight - CameraLocalOffset.y;
            offsetObject.transform.localPosition = new Vector3(0, m_EyeHeight, 0.2f) - CameraLocalOffset;

            CharacterController cc = CurrentVRRig.GetComponent<CharacterController>();
            cc.height = m_BodyHeight;
            cc.center = new Vector3(0, m_BodyHeight / 2 + cc.skinWidth, 0);

            // TODO Too wide means the floating feet, or I have to
            //      improve the feet IK up to the root pose.
            cc.radius = 0.01f;

        }

    }
}
