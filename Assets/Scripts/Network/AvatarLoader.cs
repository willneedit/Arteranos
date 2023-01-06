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

        public NetworkVariable<FixedString512Bytes> m_AvatarURL;
        public GameObject m_AvatarObject = null;
        private bool loading = false;

        public override void OnNetworkSpawn()
        {
            m_AvatarURL.Value = "https://api.readyplayer.me/v1/avatars/6394c1e69ef842b3a5112221.glb";
        }

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            if(loading) return;

            if(!m_AvatarURL.Value.IsEmpty)
            {
                loading = true;
                StartCoroutine(ProcessLoader());
            }
        }

        IEnumerator ProcessLoader()
        {
            Debug.Log("Starting avatar loading: " + m_AvatarURL.Value);
            yield return new WaitForSeconds(5);

            GameObject newAv = Instantiate(m_AvatarObject, transform.position, transform.rotation);
            newAv.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);

            this.GetComponent<NetworkObject>().Despawn();
        }
    }
}
