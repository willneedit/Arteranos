/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

using DitzelGames.FastIK;
using ReadyPlayerMe.AvatarLoader;

using System.Collections.Generic;
using Arteranos.XR;
using Arteranos.Core;

namespace Arteranos.Avatar
{
    public class AvatarLoader_RPM : MonoBehaviour, IAvatarLoader
    {
        private GameObject m_AvatarStandin = null;
        private bool loading = false;

        private GameObject m_AvatarGameObject = null;
        private AvatarObjectLoader m_AvatarLoader = null;

        // Non-null: manually load a puppet avatar on init
        public string GalleryModeURL { get => m_GalleryModeURL; set => m_GalleryModeURL = value; }

        [SerializeField] private string m_GalleryModeURL = null;

        // Animate the avatar in the gallery mode
        [SerializeField] private RuntimeAnimatorController animator = null;

        public Transform LeftHand { get; private set; }
        public Transform RightHand { get; private set; }
        public Transform LeftFoot { get; private set; }
        public Transform RightFoot { get; private set; }
        public Transform CenterEye { get; private set; }
        public Transform Head { get; private set; }
        public float FootElevation { get; private set; }
        public float EyeHeight { get; private set; }
        public float FullHeight { get; private set; }


        public Quaternion LhrOffset { get => Quaternion.Euler(0, 90, 90); }
        public Quaternion RhrOffset { get => Quaternion.Euler(0, -90, -90); }


        public bool Invisible
        {
            get => m_invisible;
            set
            {
                m_invisible = value;
                if(m_AvatarGameObject!= null)
                {
                    Renderer[] renderers = m_AvatarGameObject.GetComponentsInChildren<Renderer>();
                    foreach(Renderer renderer in renderers)
                        renderer.enabled = !Invisible;
                }
            }
        }

        private AvatarBrain avatarBrain = null;

        private void Awake() => m_AvatarStandin = Resources.Load<GameObject>("Avatar/Avatar_StandIn");
        private bool m_invisible = false;

        public void OnEnable()
        {
            m_AvatarLoader = new AvatarObjectLoader();
            // m_AvatarLoader.SaveInProjectFolder = true;
            m_AvatarLoader.OnCompleted += AvatarLoadComplete;
            m_AvatarLoader.OnFailed += AvatarLoadFailed;

            m_AvatarGameObject = Instantiate(m_AvatarStandin);
            m_AvatarGameObject.transform.SetParent(transform, false);

            if(!string.IsNullOrEmpty(GalleryModeURL))
            {
                RequestAvatarChange(GalleryModeURL);
            }
            else
            {
                avatarBrain = GetComponent<AvatarBrain>();
                avatarBrain.BodyAvatarURLChanging += RequestAvatarChange;
            }
        }

        public void OnDisable()
        {
            if(string.IsNullOrEmpty(GalleryModeURL))
                avatarBrain.BodyAvatarURLChanging -= RequestAvatarChange;
        }

        private string last = null;
        private string present = null;

        void RequestAvatarChange(string current)
        {
            if(loading || current == null || last == current) return;
            present= current;

            loading = true;
            Debug.Log("Starting avatar loading: " + current);

            m_AvatarLoader.LoadAvatar(current.ToString());
        }


        // --------------------------------------------------------------------
        #region Skeleton/Pose measurement

        Transform RigNetworkIK(
            GameObject avatar, string limb, ref List<string> jointnames,
            Vector3? poleOffset = null, int bones = 2)
        {
            Transform handle = null;

            Transform limbT = avatar.transform.FindRecursive(limb);
            if(limbT == null)
            {
                Debug.LogWarning($"Missing limb: {limb}");
                return null;
            }

            avatar.SetActive(false);

            // Owner is setting up the IK and the puppet handles...
            if(avatarBrain.isOwned)
            {
                Transform pole = null;

                handle = new GameObject("Target_" + limb).transform;
                handle.SetPositionAndRotation(limbT.position, limbT.rotation);
                handle.SetParent(avatar.transform);

                if(poleOffset != null)
                {
                    pole = new GameObject("Pole_" + limb).transform;
                    pole.SetPositionAndRotation(
                        limbT.position + avatar.transform.rotation * poleOffset.Value,
                        limbT.rotation);
                    pole.SetParent(avatar.transform);
                }
                FastIKFabric limbIK = limbT.gameObject.AddComponent<FastIKFabric>();

                limbIK.ChainLength = bones;
                limbIK.Target = handle;
                limbIK.Pole = pole;
            }

            // Owned and alien avatars have to set up the Skeleton IK data record.
            Transform boneT = limbT;
            for(int i = 0; i <= bones; i++)
            {
                jointnames.Add(boneT.name);
                boneT = boneT.parent;
            }

            avatar.SetActive(true);

            return handle;
        }

        /// <summary>
        /// Reset the avatar to the 'attention' pose rather than the A- or T-pose, using
        /// the IK handles.
        /// </summary>
        public void ResetPose(bool leftHand, bool rightHand)
        {
            if(LeftHand != null && leftHand)
            {
                Vector3 idle_lh = new(-0.4f, 0, 0);
                Quaternion idle_rlh = Quaternion.Euler(180, -90, 0);
                LeftHand.SetLocalPositionAndRotation(idle_lh, idle_rlh);
            }

            if(RightHand != null && rightHand)
            {
                Vector3 idle_rh = new(0.4f, 0, 0);
                Quaternion idle_rrh = Quaternion.Euler(180, 90, 0);
                RightHand.SetLocalPositionAndRotation(idle_rh, idle_rrh);
            }

        }

        #endregion

        // --------------------------------------------------------------------
        #region Mouth morphing

        private const string MOUTH_OPEN_BLEND_SHAPE_NAME = "mouthOpen";
        private const int AMPLITUDE_MULTIPLIER = 50;
        private const string AvatarAnimator = "AvatarAnim/BaseRPMAnimator";

        private SkinnedMeshRenderer headMesh;
        private SkinnedMeshRenderer beardMesh;
        private SkinnedMeshRenderer teethMesh;

        private int mouthOpenBlendShapeIndexOnHeadMesh = -1;
        private int mouthOpenBlendShapeIndexOnBeardMesh = -1;
        private int mouthOpenBlendShapeIndexOnTeethMesh = -1;

        private void SetupMouthBlendShapes(GameObject ago)
        {
            headMesh = GetMeshAndSetIndex(ago, MeshType.HeadMesh, ref mouthOpenBlendShapeIndexOnHeadMesh);
            beardMesh = GetMeshAndSetIndex(ago, MeshType.BeardMesh, ref mouthOpenBlendShapeIndexOnBeardMesh);
            teethMesh = GetMeshAndSetIndex(ago, MeshType.TeethMesh, ref mouthOpenBlendShapeIndexOnTeethMesh);
        }

        private void SetBlendShapeWeights(float weight)
        {
            SetBlendShapeWeight(headMesh, mouthOpenBlendShapeIndexOnHeadMesh);
            SetBlendShapeWeight(beardMesh, mouthOpenBlendShapeIndexOnBeardMesh);
            SetBlendShapeWeight(teethMesh, mouthOpenBlendShapeIndexOnTeethMesh);

            void SetBlendShapeWeight(SkinnedMeshRenderer mesh, int index)
            {
                if(index >= 0)
                    mesh.SetBlendShapeWeight(index, weight);
            }
        }

        private SkinnedMeshRenderer GetMeshAndSetIndex(GameObject ago, MeshType meshType, ref int index)
        {
            SkinnedMeshRenderer mesh = ago.GetMeshRenderer(meshType);
            if(mesh != null)
                index = mesh.sharedMesh.GetBlendShapeIndex(MOUTH_OPEN_BLEND_SHAPE_NAME);

            return mesh;
        }

        public void UpdateOpenMouth(float amount) => SetBlendShapeWeights(Mathf.Clamp01(amount * AMPLITUDE_MULTIPLIER));


        #endregion

        private void SetupAvatar(CompletionEventArgs args)
        {
            // TODO Non-standard 'up' vector considerations!
            m_AvatarGameObject = args.Avatar;
            Transform agot = m_AvatarGameObject.transform;


            List<string> jointnames = new();

            LeftHand = RigNetworkIK(m_AvatarGameObject, "LeftHand", ref jointnames);
            RightHand = RigNetworkIK(m_AvatarGameObject, "RightHand", ref jointnames);
            LeftFoot = RigNetworkIK(m_AvatarGameObject, "LeftFoot", ref jointnames, new Vector3(0, 0, 2));
            RightFoot = RigNetworkIK(m_AvatarGameObject, "RightFoot", ref jointnames, new Vector3(0, 0, 2));
            Head = RigNetworkIK(m_AvatarGameObject, "Head", ref jointnames, null, 1);

            Transform rEye = agot.FindRecursive("RightEye");
            Transform lEye = agot.FindRecursive("LeftEye");

            // FIXME Fixup for the VR device specific skew?
            Vector3 cEyePos = (lEye.position + rEye.position) / 2 + new Vector3(0, 0, 0.11f);

            // Animation and pose data will be transmitted though the NetworkPose, so
            // the animator will be owner-driven.
            if(avatarBrain.isOwned)
            {
                CenterEye = new GameObject("Target_centerEye").transform;
                CenterEye.SetPositionAndRotation(cEyePos, rEye.rotation);
                CenterEye.SetParent(agot);

                Animator anim = args.Avatar.GetComponent<Animator>();
                anim.avatar = null;
                anim.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(AvatarAnimator);

                // Height of feet joints to the floor
                FootElevation = (LeftFoot.position.y + RightFoot.position.y) / 2 - agot.position.y;
            }

            // Now upload the skeleton joint data to the Avatar Pose driver.
            GetComponent<AvatarPoseDriver>().UploadJointNames(agot, jointnames.ToArray());

            // Set the avatar to the attention pose from the A- or T-pose.
            ResetPose(true, true);

            Transform fullHeight = agot.FindRecursive("HeadTop_End");

            EyeHeight = cEyePos.y - transform.position.y;
            FullHeight = fullHeight.transform.position.y - transform.position.y;

            if(avatarBrain.isOwned)
            {
                // And reconfigure the XR Rig to match the avatar's dimensions.
                IXRControl xrc = XRControl.Instance;

                xrc.EyeHeight = EyeHeight;
                xrc.BodyHeight = FullHeight;

                xrc.ReconfigureXRRig();
            }

            // Lastly, breathe some life into the avatar.
            EyeAnimationHandler eah = args.Avatar.AddComponent<EyeAnimationHandler>();
            eah.BlinkInterval = 6; // 3 seconds is a little bit too fast.

            SetupMouthBlendShapes(m_AvatarGameObject);
        }

        void AvatarLoadComplete(object _, CompletionEventArgs args)
        {
            if(m_AvatarGameObject != null)
                Destroy(m_AvatarGameObject);

            if(avatarBrain != null)
                args.Avatar.name += $"_{avatarBrain.NetID}";
            else
                args.Avatar.name += "_puppet";

            args.Avatar.transform.SetParent(transform, false);

            if(string.IsNullOrEmpty(GalleryModeURL))
            {
                SetupAvatar(args);
            }
            else if(animator != null)
            {
                args.Avatar.GetComponent<Animator>().runtimeAnimatorController = animator;

                EyeAnimationHandler eah = args.Avatar.AddComponent<EyeAnimationHandler>();
                eah.BlinkInterval = 6; // 3 seconds is a little bit too fast.
            }

            // Refresh the visibility state for the new avatar
            Invisible = m_invisible;

            Debug.Log("Successfully loaded avatar");
            last = present;
            loading = false;
        }

        void AvatarLoadFailed(object sender, FailureEventArgs args)
        {
            Debug.Log($"Avatar loading failed with error message: {args.Message}");
            loading = false;
        }

    }
}
