using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System;

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

        private GameObject m_SpawnedStandin = null;
        private AvatarObjectLoader m_AvatarLoader = null;
        private Arteranos.Core.SettingsManager m_SettingsManager = null;


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

            m_SpawnedStandin = Instantiate(m_AvatarStandin, transform.position, transform.rotation);
            m_SpawnedStandin.transform.SetParent(transform);
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

        GameObject RigIK(GameObject avatar, string limb)
        {

            Transform lHand = avatar.transform.FindRecursive(limb);
            if(lHand == null)
            {
                Debug.LogWarning($"Missing limb: {0}", lHand);
                return null;
            }

            GameObject handle = new GameObject("Handle_" + limb);
            handle.transform.SetPositionAndRotation(lHand.position, lHand.rotation);
            handle.transform.SetParent(avatar.transform.parent);

            avatar.SetActive(false);
            FastIKFabric lHandIK = lHand.gameObject.AddComponent<FastIKFabric>();

            lHandIK.ChainLength = 2;
            lHandIK.Target = handle.transform;
            avatar.SetActive(true);

            return handle;
        }

        void AvatarLoadComplete(object sender, CompletionEventArgs args)
        {
            Debug.Log("Successfully loaded avatar");
            loading = false;

            if(m_SpawnedStandin != null)
            {
                Destroy(m_SpawnedStandin);
                m_SpawnedStandin = null;
            }

            args.Avatar.name += args.Avatar.name + "_" + OwnerClientId;
            args.Avatar.transform.SetParent(transform);

            if(!IsOwner) return;

            GameObject lHand = RigIK(args.Avatar, "LeftHand");
            GameObject rHand = RigIK(args.Avatar, "RightHand");

        }

        void AvatarLoadFailed(object sender, FailureEventArgs args)
        {
            Debug.Log($"Avatar loading failed with error message: {args.Message}");
            loading = false;
        }
    }
}

#endif