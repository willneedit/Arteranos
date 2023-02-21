/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using UnityEngine;

using DitzelGames.FastIK;
using ReadyPlayerMe.AvatarLoader;
using Arteranos.ExtensionMethods;

using Mirror;
using System.Collections.Generic;

namespace Arteranos.NetworkIO
{
    [RequireComponent(typeof(AvatarPoseDriver))]
    [RequireComponent(typeof(NetworkIdentity))]
    public class AvatarLoader_RPM : NetworkBehaviour, IAvatarLoader
    {
        [SyncVar(hook = nameof(OnAvatarURLChanged))]
        public string m_AvatarURL;
        public GameObject m_AvatarStandin = null;
        private bool loading = false;

        private GameObject m_AvatarGameObject = null;
        private AvatarObjectLoader_mod m_AvatarLoader = null;

        public Transform LeftHand { get; private set; }
        public Transform RightHand { get; private set; }
        public Transform LeftFoot { get; private set; }
        public Transform RightFoot { get; private set; }
        public Transform CenterEye { get; private set; }
        public Transform Head { get; private set; }

        public Quaternion LhrOffset { get => Quaternion.Euler(0, 90, 90); }
        public Quaternion RhrOffset { get => Quaternion.Euler(0, -90, -90); }

        public override void OnStartClient()
        {
            name += "_" + netIdentity.netId;

            Core.ServerSettings serverSettings = Core.SettingsManager.Server;
            Core.ClientSettings clientSettings = Core.SettingsManager.Client;

            if(serverSettings.ShowAvatars || !isServer || isLocalPlayer)
            {
                m_AvatarLoader = new AvatarObjectLoader_mod();
                // m_AvatarLoader.SaveInProjectFolder = true;
                m_AvatarLoader.OnCompleted += AvatarLoadComplete;
                m_AvatarLoader.OnFailed += AvatarLoadFailed;

                clientSettings.OnAvatarChanged += RequestAvatarChange;

                // The late-joining client's companions, or the newly joined clients.
                if(isOwned)
                {
                    // FIXME: Burst mitigation
                    RequestAvatarChange(null, clientSettings.AvatarURL);
                }
            }

            m_AvatarGameObject = Instantiate(m_AvatarStandin, transform.position, transform.rotation);
            m_AvatarGameObject.transform.SetParent(transform);
            m_AvatarGameObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            base.OnStartClient();
        }

        public override void OnStopClient()
        {
            Core.SettingsManager.Client.OnAvatarChanged -= RequestAvatarChange;
            base.OnStopClient();
        }

        public void RequestAvatarChange(string old, string current)
        {
            if(current == m_AvatarURL) return;

            UpdateAvatarServerRpc(current);
        }

        [Command]
        private void UpdateAvatarServerRpc(string avatarURL) => m_AvatarURL = avatarURL;

        void OnAvatarURLChanged(string _, string current)
        {
            if(loading) return;

            loading = true;
            StartCoroutine(ProcessLoader(current));
        }

        IEnumerator ProcessLoader(string current)
        {
            Debug.Log("Starting avatar loading: " + current);
            yield return null;

            m_AvatarLoader.LoadAvatar(current.ToString());
        }

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
            if(isOwned)
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
            for(int i=0; i<=bones; i++)
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
        public void ResetPose()
        {
            if (LeftHand != null)
            {
                Vector3 idle_lh = new(-0.4f, 0, 0);
                Quaternion idle_rlh = Quaternion.Euler(180, -90, 0);
                LeftHand.SetLocalPositionAndRotation(idle_lh, idle_rlh);
            }

            if (RightHand != null)
            {
                Vector3 idle_rh = new(0.4f, 0, 0);
                Quaternion idle_rrh = Quaternion.Euler(180, 90, 0);
                RightHand.SetLocalPositionAndRotation(idle_rh, idle_rrh);
            }

        }

        private void SetupAvatar(CompletionEventArgs args)
        {
            m_AvatarGameObject = args.Avatar;
            Transform agot = m_AvatarGameObject.transform;

            m_AvatarGameObject.name += "_" + netIdentity.netId;
            agot.SetParent(transform);
            agot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

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

            if(isOwned)
            {
                CenterEye = new GameObject("Target_centerEye").transform;
                CenterEye.SetPositionAndRotation(cEyePos, rEye.rotation);
                CenterEye.SetParent(agot);

                Animator anim = args.Avatar.GetComponent<Animator>();
                anim.avatar = null;

                anim.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("BaseRPMAnimator");
            }

            // Now upload the skeleton joint data to the Avatar Pose driver.
            GetComponent<AvatarPoseDriver>().UploadJointNames(jointnames.ToArray());

            ResetPose();

            // And reconfigure the XR Rig to match the avatar's dimensions.
            XR.XRControl xrc = XR.XRControl.Instance;
            Transform fullHeight = agot.FindRecursive("HeadTop_End");

            xrc.m_EyeHeight = cEyePos.y - transform.position.y;
            xrc.m_BodyHeight = fullHeight.transform.position.y - transform.position.y;

            xrc.ReconfigureXRRig();

            // Lastly, breathe some life into the avatar.
            EyeAnimationHandler eah = args.Avatar.AddComponent<EyeAnimationHandler>();
            eah.BlinkInterval = 6; // 3 seconds is a little bit too fast.
        }

        void AvatarLoadComplete(object _, CompletionEventArgs args)
        {
            Debug.Log("Successfully loaded avatar");
            loading = false;

            if(m_AvatarGameObject != null)
                Destroy(m_AvatarGameObject);

            SetupAvatar(args);
        }

        void AvatarLoadFailed(object sender, FailureEventArgs args)
        {
            Debug.Log($"Avatar loading failed with error message: {args.Message}");
            loading = false;
        }
    }
}
