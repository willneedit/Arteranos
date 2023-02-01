using System.Collections;
using UnityEngine;
using Unity.Collections;

using DitzelGames.FastIK;
using ReadyPlayerMe.AvatarLoader;
using Arteranos.ExtensionMethods;

using Mirror;
using System.Collections.Generic;

namespace Arteranos.NetworkIO
{
    public class AvatarReplacer : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnAvatarURLChanged))]
        public string m_AvatarURL;
        public GameObject m_AvatarStandin = null;
        private bool loading = false;

        private GameObject m_AvatarGameObject = null;
        private AvatarObjectLoader m_AvatarLoader = null;
        private Arteranos.Core.SettingsManager m_SettingsManager = null;

        public GameObject m_LeftHand = null;
        public GameObject m_RightHand = null;
        public GameObject m_CenterEye = null;
        public GameObject m_Head = null;


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

        GameObject RigNetworkIK(GameObject avatar, string limb, ref List<string> jointnames, int bones = 2)
        {
            GameObject handle = null;
    
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
                handle = new GameObject("Handle_" + limb);
                handle.transform.SetPositionAndRotation(limbT.position, limbT.rotation);
                handle.transform.SetParent(avatar.transform.parent);

                FastIKFabric limbIK = limbT.gameObject.AddComponent<FastIKFabric>();

                limbIK.ChainLength = bones;
                limbIK.Target = handle.transform;
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
            if (m_LeftHand != null)
            {
                Vector3 idle_lh = new Vector3(-0.4f, 0, 0);
                Quaternion idle_rlh = Quaternion.Euler(180, -90, 0);
                m_LeftHand.transform.SetLocalPositionAndRotation(idle_lh, idle_rlh);
            }

            if (m_RightHand != null)
            {
                Vector3 idle_rh = new Vector3(0.4f, 0, 0);
                Quaternion idle_rrh = Quaternion.Euler(180, 90, 0);
                m_RightHand.transform.SetLocalPositionAndRotation(idle_rh, idle_rrh);
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

            m_LeftHand = RigNetworkIK(m_AvatarGameObject, "LeftHand", ref jointnames);
            m_RightHand = RigNetworkIK(m_AvatarGameObject, "RightHand", ref jointnames);
            m_Head = RigNetworkIK(m_AvatarGameObject, "Head", ref jointnames, 1);

            Transform rEye = agot.FindRecursive("RightEye");
            Transform lEye = agot.FindRecursive("LeftEye");
            Vector3 cEyePos = (lEye.position + rEye.position) / 2;

            if(isOwned)
            {
                m_CenterEye = new GameObject("Handle_centerEye");
                m_CenterEye.transform.SetPositionAndRotation(cEyePos, rEye.rotation);
                m_CenterEye.transform.SetParent(agot.parent);
            }

            // Now upload the skeleton joint data to the Avatar Pose driver.
            AvatarPlayer ap = this.GetComponent<AvatarPlayer>();
            ap.UploadJointNames(jointnames.ToArray());

            ResetPose();
        }

        void AvatarLoadFailed(object sender, FailureEventArgs args)
        {
            Debug.Log($"Avatar loading failed with error message: {args.Message}");
            loading = false;
        }
    }
}
