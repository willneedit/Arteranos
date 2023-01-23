using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

#if true

using DitzelGames.FastIK;
using ReadyPlayerMe.AvatarLoader;
using Arteranos.ExtensionMethods;

namespace Arteranos.NetworkIO
{
    public class AvatarReplacer : NetworkBehaviour
    {

        public NetworkVariable<FixedString512Bytes> m_AvatarURL;
        public GameObject m_AvatarStandin = null;
        private bool loading = false;

        private GameObject m_AvatarGameObject = null;
        private AvatarObjectLoader m_AvatarLoader = null;
        private Arteranos.Core.SettingsManager m_SettingsManager = null;

        public GameObject m_LeftHand = null;
        public GameObject m_RightHand = null;
        public GameObject m_CenterEye = null;
        public GameObject m_Head = null;


        public override void OnNetworkSpawn()
        {
            m_SettingsManager = FindObjectOfType<Arteranos.Core.SettingsManager>();

            this.name = this.name + "_" + OwnerClientId;

            if(m_SettingsManager.m_Server.ShowAvatars || !IsServer || IsHost)
            {
                m_AvatarLoader = new AvatarObjectLoader();
                m_AvatarLoader.OnCompleted += AvatarLoadComplete;
                m_AvatarLoader.OnFailed += AvatarLoadFailed;

                m_SettingsManager.m_Client.OnAvatarChanged += RequestAvatarChange;
                m_AvatarURL.OnValueChanged += OnAvatarURLChanged;

                // The late-joining client's companions, or the newly joined clients.
                if(!IsOwner)
                    // FIXME: Burst mitigation
                    StartCoroutine(ProcessLoader(m_AvatarURL.Value));
                else
                {
                    RequestAvatarChange(null, m_SettingsManager.m_Client.AvatarURL);
                }
            }

            m_AvatarGameObject = Instantiate(m_AvatarStandin, transform.position, transform.rotation);
            m_AvatarGameObject.transform.SetParent(transform);
        }

        public override void OnNetworkDespawn()
        {
            m_SettingsManager.m_Client.OnAvatarChanged -= RequestAvatarChange;
            base.OnNetworkDespawn();
        }

        public void RequestAvatarChange(string old, string current)
        {
            if(current == m_AvatarURL.Value) return;

            UpdateAvatarServerRpc(current);
        }

        [ServerRpc]
        private void UpdateAvatarServerRpc(FixedString512Bytes avatarURL, ServerRpcParams rpcParams = default)
        {
            m_AvatarURL.Value = avatarURL;
        }

        void OnAvatarURLChanged(FixedString512Bytes old, FixedString512Bytes current)
        {
            if(loading) return;

            loading = true;
            StartCoroutine(ProcessLoader(current));
        }

        IEnumerator ProcessLoader(FixedString512Bytes current)
        {
            Debug.Log("Starting avatar loading: " + current);
            yield return null;

            m_AvatarLoader.LoadAvatar(current.ToString());
        }

        GameObject RigNetworkIK(GameObject avatar, string limb, int bones = 2)
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
            if(IsOwner)
            {
                handle = new GameObject("Handle_" + limb);
                handle.transform.SetPositionAndRotation(limbT.position, limbT.rotation);
                handle.transform.SetParent(avatar.transform.parent);

                FastIKFabric limbIK = limbT.gameObject.AddComponent<FastIKFabric>();

                limbIK.ChainLength = bones;
                limbIK.Target = handle.transform;
            }

            // ...and everyone has to set up the ClientNetworkTransform,
            // the owner as the sender, everyone else the receiver.
            Transform boneT = limbT;
            for(int i=0; i<=bones; i++)
            {
                ClientNetworkTransform netT = boneT.gameObject.AddComponent<Arteranos.NetworkIO.ClientNetworkTransform>();

                // In the body, the joints would rotate unless breaks them, of course.
                netT.SyncPositionX = false;
                netT.SyncPositionY = false;
                netT.SyncPositionZ = false;
                netT.SyncScaleX = false;
                netT.SyncScaleY = false;
                netT.SyncScaleZ = false;
                netT.InLocalSpace = true;

                boneT = boneT.parent;
            }

            avatar.SetActive(true);

            return handle;
        }

        void AvatarLoadComplete(object sender, CompletionEventArgs args)
        {
            Debug.Log("Successfully loaded avatar");
            loading = false;

            if(this.m_AvatarGameObject != null)
                Destroy(this.m_AvatarGameObject);

            m_AvatarGameObject = args.Avatar;
            Transform agot = m_AvatarGameObject.transform;

            m_AvatarGameObject.name += m_AvatarGameObject.name + "_" + OwnerClientId;
            agot.SetParent(transform);

            m_LeftHand = RigNetworkIK(m_AvatarGameObject, "LeftHand");
            m_RightHand = RigNetworkIK(m_AvatarGameObject, "RightHand");
            m_Head = RigNetworkIK(m_AvatarGameObject, "Head", 1);

            Transform rEye = agot.FindRecursive("RightEye");
            Transform lEye = agot.FindRecursive("LeftEye");
            Vector3 cEyePos = (lEye.position + rEye.position) / 2;

            m_CenterEye = new GameObject("Handle_centerEye");
            m_CenterEye.transform.SetPositionAndRotation(cEyePos, rEye.rotation);
            m_CenterEye.transform.SetParent(agot.parent);

        }

        void AvatarLoadFailed(object sender, FailureEventArgs args)
        {
            Debug.Log($"Avatar loading failed with error message: {args.Message}");
            loading = false;
        }
    }
}

#endif