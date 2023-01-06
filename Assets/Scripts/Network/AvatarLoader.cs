using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System;

namespace NetworkIO
{
    public class AvatarLoader : NetworkBehaviour
    {

        public NetworkVariable<FixedString512Bytes> m_AvatarURL = new NetworkVariable<FixedString512Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public GameObject m_AvatarStandin = null;
        public GameObject m_AvatarObject = null;
        private bool loading = false;

        private GameObject m_SpawnedStandin = null;

        public override void OnNetworkSpawn()
        {
            m_AvatarURL.OnValueChanged += OnAvatarURLChanged;

            // DEBUG
            if(IsOwner)
                m_AvatarURL.Value = "https://api.readyplayer.me/v1/avatars/6394c1e69ef842b3a5112221.glb";

            m_SpawnedStandin = Instantiate(m_AvatarStandin, transform.position, transform.rotation);
            m_SpawnedStandin.transform.SetParent(transform);
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
            yield return new WaitForSeconds(5);

            if(m_SpawnedStandin != null)
            {
                Destroy(m_SpawnedStandin);
                m_SpawnedStandin = null;
            }

            GameObject newAv = Instantiate(m_AvatarObject, transform.position, transform.rotation);
            newAv.transform.SetParent(transform);
            loading = false;
        }

    }
}
