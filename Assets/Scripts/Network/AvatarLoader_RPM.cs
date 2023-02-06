using System.Collections;
using UnityEngine;
using Unity.Collections;

using DitzelGames.FastIK;
using ReadyPlayerMe.AvatarLoader;
using Arteranos.ExtensionMethods;

using Mirror;
using System.Collections.Generic;
using Unity.XR.CoreUtils;

namespace Arteranos.NetworkIO
{
    public class AvatarLoader_RPM : NetworkBehaviour, IAvatarLoader
    {
        [SyncVar(hook = nameof(OnAvatarURLChanged))]
        public string m_AvatarURL;
        public GameObject m_AvatarStandin = null;
        private bool loading = false;

        private GameObject m_AvatarGameObject = null;
        private AvatarObjectLoader m_AvatarLoader = null;
        private Arteranos.Core.SettingsManager m_SettingsManager = null;

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
            m_SettingsManager = FindObjectOfType<Arteranos.Core.SettingsManager>();

            this.name = this.name + "_" + netIdentity.netId;

            if(m_SettingsManager.m_Server.ShowAvatars || !isServer || isLocalPlayer)
            {
                m_AvatarLoader = new AvatarObjectLoader();
                m_AvatarLoader.OnCompleted += AvatarLoadComplete;
                m_AvatarLoader.OnFailed += AvatarLoadFailed;

                m_SettingsManager.m_Client.OnAvatarChanged += RequestAvatarChange;

                // The late-joining client's companions, or the newly joined clients.
                if(isOwned)
                {
                    // FIXME: Burst mitigation
                    RequestAvatarChange(null, m_SettingsManager.m_Client.AvatarURL);
                }
            }

            m_AvatarGameObject = Instantiate(m_AvatarStandin, transform.position, transform.rotation);
            m_AvatarGameObject.transform.SetParent(transform);
            m_AvatarGameObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            base.OnStartClient();
        }

        public override void OnStopClient()
        {
            m_SettingsManager.m_Client.OnAvatarChanged -= RequestAvatarChange;
            base.OnStopClient();
        }

        public void RequestAvatarChange(string old, string current)
        {
            if(current == m_AvatarURL) return;

            UpdateAvatarServerRpc(current);
        }

        [Command]
        private void UpdateAvatarServerRpc(string avatarURL)
        {
            m_AvatarURL = avatarURL;
        }

        void OnAvatarURLChanged(string old, string current)
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

        Transform RigNetworkIK(GameObject avatar, string limb, ref List<string> jointnames, int bones = 2)
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
                handle = new GameObject("Handle_" + limb).transform;
                handle.SetPositionAndRotation(limbT.position, limbT.rotation);
                handle.SetParent(avatar.transform.parent);

                FastIKFabric limbIK = limbT.gameObject.AddComponent<FastIKFabric>();

                limbIK.ChainLength = bones;
                limbIK.Target = handle;
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
                Vector3 idle_lh = new Vector3(-0.4f, 0, 0);
                Quaternion idle_rlh = Quaternion.Euler(180, -90, 0);
                LeftHand.SetLocalPositionAndRotation(idle_lh, idle_rlh);
            }

            if (RightHand != null)
            {
                Vector3 idle_rh = new Vector3(0.4f, 0, 0);
                Quaternion idle_rrh = Quaternion.Euler(180, 90, 0);
                RightHand.SetLocalPositionAndRotation(idle_rh, idle_rrh);
            }

        }

        void AvatarLoadComplete(object sender, CompletionEventArgs args)
        {
            Debug.Log("Successfully loaded avatar");
            loading = false;

            if(m_AvatarGameObject != null)
                Destroy(m_AvatarGameObject);

            m_AvatarGameObject = args.Avatar;
            Transform agot = m_AvatarGameObject.transform;

            m_AvatarGameObject.name += m_AvatarGameObject.name + "_" + netIdentity.netId;
            agot.SetParent(transform);
            agot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            List<string> jointnames = new List<string>();

            LeftHand = RigNetworkIK(m_AvatarGameObject, "LeftHand", ref jointnames);
            RightHand = RigNetworkIK(m_AvatarGameObject, "RightHand", ref jointnames);
            LeftFoot = RigNetworkIK(m_AvatarGameObject, "LeftFoot", ref jointnames);
            RightFoot = RigNetworkIK(m_AvatarGameObject, "RightFoot", ref jointnames);
            Head = RigNetworkIK(m_AvatarGameObject, "Head", ref jointnames, 1);

            Transform rEye = agot.FindRecursive("RightEye");
            Transform lEye = agot.FindRecursive("LeftEye");
            Vector3 cEyePos = (lEye.position + rEye.position) / 2;

            if(isOwned)
            {
                CenterEye = new GameObject("Handle_centerEye").transform;
                CenterEye.SetPositionAndRotation(cEyePos, rEye.rotation);
                CenterEye.SetParent(agot.parent);
            }

            // Now upload the skeleton joint data to the Avatar Pose driver.
            AvatarPoseDriver apd = this.GetComponent<AvatarPoseDriver>();
            apd.UploadJointNames(jointnames.ToArray());

            ResetPose();

            // And reconfigure the XR Rig to match the avatar's dimensions.
            XR.XRControl xrc = XR.XRControl.Singleton;
            Transform fullHeight = agot.FindRecursive("HeadTop_End");

            xrc.m_EyeHeight = cEyePos.y - transform.position.y;
            xrc.m_BodyHeight = fullHeight.transform.position.y - transform.position.y;

            xrc.ReconfigureXRRig();
        }

        void AvatarLoadFailed(object sender, FailureEventArgs args)
        {
            Debug.Log($"Avatar loading failed with error message: {args.Message}");
            loading = false;
        }
    }
}
