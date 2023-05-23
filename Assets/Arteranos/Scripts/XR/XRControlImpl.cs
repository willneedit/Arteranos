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
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Management;

namespace Arteranos.XR
{
    public class XRControlImpl : MonoBehaviour, IXRControl
    {
        public XROrigin VRRig;
        public XROrigin NoVRRig;

        public GameObject Me { get; set; }
        public bool UsingXR { get; private set; }
        public Vector3 CameraLocalOffset { get; private set; }
        public float EyeHeight { get; set; } = 1.75f;
        public float BodyHeight { get; set; } = 1.85f;
        public event Action<bool> XRSwitchEvent;

        public Vector3 heightAdjustment => CurrentVRRig.Origin.transform.up * CurrentVRRig.CameraInOriginSpaceHeight;

        private XROrigin CurrentVRRig { get; set; }
        private bool VRRunning = false;

        public new bool enabled
        {
            get => base.enabled;
            set => base.enabled = value;
        }

        public Transform rigTransform
        {
            get => CurrentVRRig.transform;
        }

        public Transform cameraTransform
        {
            get => CurrentVRRig.Camera.transform;
        }

        public new GameObject gameObject
        {
            get => base.gameObject;
        }

        public void Awake()
        {
            XRControl.Instance = this;
            CurrentVRRig = FindObjectOfType<XROrigin>();
        }

        public void OnDestroy() => XRControl.Instance = null;

        public IEnumerator StartXRCoroutine()
        {
            Debug.Log("Initializing XR...");
            yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

            if (XRGeneralSettings.Instance.Manager.activeLoader == null)
            {
                Debug.LogWarning("Initializing XR Failed. Check Editor or Player log for details. Maybe there's no VR device available.");
                Core.SettingsManager.Client.VRMode = false;
                UpdateXROrigin(false);
            }
            else
            {
                Debug.Log("Starting XR...");
                XRGeneralSettings.Instance.Manager.StartSubsystems();
                VRRunning = true;
                UpdateXROrigin(true);
            }
        }

        void StopXR()
        {
            Debug.Log("Stopping XR...");

            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            Debug.Log("XR stopped completely.");
            VRRunning = false;
            UpdateXROrigin(false);
        }
        
        // Start is called before the first frame update
        void Start()
        {
            Core.SettingsManager.Client.OnVRModeChanged += OnVRModeChanged;
            if(Core.SettingsManager.Client.VRMode != VRRunning)
                OnVRModeChanged(true);
            else
                UpdateXROrigin(false);
        }

        void OnVRModeChanged(bool current)
        {
            if(current == VRRunning) return;

            if(current)
                StartCoroutine(StartXRCoroutine());
            else
                StopXR();
        }

        void UpdateXROrigin(bool useVR)
        {
            IEnumerator ReconfigureCR()
            {
                yield return new WaitForSeconds(5);
                ReconfigureXRRig();
            };

            CurrentVRRig = FindObjectOfType<XROrigin>();

            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;

            if(CurrentVRRig != null)
            {
                position = CurrentVRRig.transform.position;
                rotation = CurrentVRRig.transform.rotation;

                Destroy(CurrentVRRig.gameObject);
            }

            CurrentVRRig = Instantiate(useVR ? VRRig : NoVRRig, position, rotation);
            StartCoroutine(ReconfigureCR());
            XRSwitchEvent?.Invoke(useVR);
            UsingXR = useVR;
        }

        // Cleanly shut down XR on exit.
        void OnApplicationQuit()
        {
            // Debug.Log("Bye!");
            if(Core.SettingsManager.Client.VRMode)
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
            CurrentVRRig.CameraYOffset = EyeHeight - CameraLocalOffset.y;
            offsetObject.transform.localPosition = new Vector3(0, EyeHeight, 0.2f) - CameraLocalOffset;

            CharacterController cc = CurrentVRRig.GetComponent<CharacterController>();
            cc.height = BodyHeight;
            cc.center = new Vector3(0, BodyHeight / 2 + cc.skinWidth, 0);

            // TODO Too wide means the floating feet, or I have to
            //      improve the feet IK up to the root pose.
            cc.radius = 0.01f;

        }

        public void FreezeControls(bool value)
        {
            XROrigin xro = CurrentVRRig;
            if(xro == null) return;
            
            // TODO More movement schenes
            ActionBasedSnapTurnProvider snapTurnProvider = 
                xro.gameObject.GetComponent<ActionBasedSnapTurnProvider>();

            ActionBasedContinuousMoveProvider continuousMoveProvider = 
                xro.gameObject.GetComponent<ActionBasedContinuousMoveProvider>();

            KMTrackedPoseDriver kMTrackedPoseDriver =
                xro.gameObject.GetComponentInChildren<KMTrackedPoseDriver>();

            if(snapTurnProvider != null) snapTurnProvider.enabled = !value;
            if(continuousMoveProvider != null) continuousMoveProvider.enabled = !value;
            if(kMTrackedPoseDriver != null) kMTrackedPoseDriver.enabled = !value;
        }

        public void MoveRig(Vector3 startPosition, Quaternion startRotation)
        {
            XROrigin xro = CurrentVRRig;

            xro.MatchOriginUpCameraForward(startRotation * Vector3.up, startRotation * Vector3.forward);
            xro.MoveCameraToWorldLocation(startPosition);
            Physics.SyncTransforms();
        }

    }
}