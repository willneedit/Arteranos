/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

using Arteranos.NetworkIO;
using Arteranos.XR;
using Arteranos.Core;

namespace Arteranos.Avatar
{

    public class AvatarPoseDriver : MonoBehaviour
    {
        private IAvatarMeasures AvatarMeasures = null;
        private NetworkPose NetworkPose = null;

        private AvatarBrain AvatarBrain = null;
        public bool isOwned => AvatarBrain?.isOwned ?? false;

        private void Awake()
        {
            NetworkPose = GetComponent<NetworkPose>();
            AvatarBrain = GetComponent<AvatarBrain>();
        }

        private void Start()
        {
            if(isOwned)
            {
                SettingsManager.Client.OnVRModeChanged += OnXRChanged;
                OnXRChanged(SettingsManager.Client.VRMode);
            }

        }

        private void OnDestroy()
        {
            if(isOwned)
                SettingsManager.Client.OnVRModeChanged -= OnXRChanged;
        }

        private void OnXRChanged(bool useXR)
        {
            Transform xrot = XRControl.Instance.rigTransform;

            // And, move the XR (or 2D) rig to the own avatar's position.
            Debug.Log("Moving rig");
            xrot.transform.SetPositionAndRotation(transform.position, transform.rotation);
            Physics.SyncTransforms();
        }

        public void UpdateAvatarMeasures(IAvatarMeasures am)
        {
            NetworkPose.UploadJointNames(am.Avatar.transform, am.JointNames.ToArray());
            AvatarMeasures = am;

            if (isOwned)
            {
                IXRControl xrc = XRControl.Instance;

                xrc.EyeHeight = am.EyeHeight;
                xrc.BodyHeight = am.FullHeight;

                xrc.ReconfigureXRRig();
            }
        }

        // --------------------------------------------------------------------
        #region Pose updating

        public void UpdateOwnPose()
        {
            // VR: Head tracking
            if(SettingsManager.Client.VRMode)
            {
                if(AvatarMeasures.CenterEye == null) return;

                Transform cam = XRControl.Instance.cameraTransform;

                if(AvatarMeasures.Head)
                    AvatarMeasures.Head.rotation = cam.rotation;
            }

            // VR + 2D: Walking animation (only with loaded avatars)
            GameObject xro = XRControl.Instance.rigTransform.gameObject;
            CharacterController cc = xro.GetComponent<CharacterController>();
            Animator anim = GetComponentInChildren<Animator>();

            if(anim != null)
            {
                // The direction...
                Vector3 moveSpeed = Quaternion.Inverse(transform.rotation) * cc.velocity;

                int frontBack = 0;
                if (moveSpeed.z < -0.5f) frontBack = -1;
                if (moveSpeed.z >  0.5f) frontBack = 1;

                int leftRight = 0;
                if (moveSpeed.x < -0.5f) leftRight = -1;
                if (moveSpeed.x >  0.5f) leftRight = 1;

                anim.SetInteger("IntWalkFrontBack", frontBack);
                anim.SetInteger("IntWalkLeftRight", leftRight);

                // ... and smaller people have to walk in a quicker pace.
                anim.SetFloat("Speed", (float)(AvatarMeasures.UnscaledHeight / AvatarMeasures.FullHeight));
            }
        }

        #endregion

        // --------------------------------------------------------------------
        #region Face/Voice morphing

        private void RouteVoice()
        {
            float amount = GetComponent<AvatarVoice>().MeasureAmplitude();

            if(AvatarMeasures?.Avatar != null)
                AvatarMeasures.Avatar.GetComponent<AvatarMouthAnimator>().MouthOpen = amount;
        }

        #endregion

        void Update()
        {
            // Avatars from other clients are slaved by the NetworkTransform and -Pose.
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

            RouteVoice();
        }
    }
}
