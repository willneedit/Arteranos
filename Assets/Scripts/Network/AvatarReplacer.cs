using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System;

#if true

using ReadyPlayerMe.AvatarLoader;

namespace NetworkIO
{
    public class AvatarReplacer : NetworkBehaviour
    {

        public NetworkVariable<FixedString512Bytes> m_AvatarURL;
        public GameObject m_AvatarStandin = null;
        public GameObject m_AvatarObject = null;
        private bool loading = false;

        private GameObject m_SpawnedStandin = null;
        private AvatarObjectLoader m_AvatarLoader = null;
        private Core.SettingsManager m_SettingsManager = null;


        public override void OnNetworkSpawn()
        {
            m_SettingsManager = FindObjectOfType<Core.SettingsManager>();
            m_SettingsManager.m_Client.OnAvatarChanged += RequestAvatarChange;

            m_AvatarURL.OnValueChanged += OnAvatarURLChanged;

            m_AvatarLoader = new AvatarObjectLoader();
            m_AvatarLoader.OnCompleted += AvatarLoadComplete;
            m_AvatarLoader.OnFailed += AvatarLoadFailed;

            this.name = this.name + "_" + OwnerClientId;

            // The late-joining client's companions, or the newly joined clients.
            if(!IsOwner)
                // FIXME: Burst mitigation
                StartCoroutine(ProcessLoader(m_AvatarURL.Value));
            else
            {
                RequestAvatarChange(null, m_SettingsManager.m_Client.AvatarURL);
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
        }

        void AvatarLoadFailed(object sender, FailureEventArgs args)
        {
            Debug.Log($"Avatar loading failed with error message: {args.Message}");
            loading = false;
        }
    }
}

#endif