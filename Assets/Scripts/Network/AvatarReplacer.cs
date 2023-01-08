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


        public override void OnNetworkSpawn()
        {
            m_AvatarURL.OnValueChanged += OnAvatarURLChanged;

            m_AvatarLoader = new AvatarObjectLoader();
            m_AvatarLoader.OnCompleted += AvatarLoadComplete;
            m_AvatarLoader.OnFailed += AvatarLoadFailed;

            this.name = this.name + "_" + OwnerClientId;

            // DEBUG
            if(IsOwner)
                UpdateAvatarServerRpc("https://api.readyplayer.me/v1/avatars/6394c1e69ef842b3a5112221.glb");

            // The late-joining client's companions
            if(!IsOwner)
                StartCoroutine(ProcessLoader(m_AvatarURL.Value));

            m_SpawnedStandin = Instantiate(m_AvatarStandin, transform.position, transform.rotation);
            m_SpawnedStandin.transform.SetParent(transform);
        }

        [ServerRpc]
        public void UpdateAvatarServerRpc(FixedString512Bytes avatarURL, ServerRpcParams rpcParams = default)
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