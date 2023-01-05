using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using System;

namespace NetworkIO
{

    public class AvatarPlayer : NetworkBehaviour
    {
        public NetworkTrackedBone _self = new NetworkTrackedBone();
        public GameObject Controller;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                Controller = GameObject.Find("_AvatarView");
            }
        }

        [ServerRpc]
        public void UpdateCurrentPositionServerRpc(Vector3 position, Quaternion rotation, ServerRpcParams rpcParams = default)
        {
            _self.position = position;
            _self.rotation = rotation;
        }

        void Update()
        {
            if (Controller != null)
                UpdateCurrentPositionServerRpc(Controller.transform.position, Controller.transform.rotation);

            transform.position = _self.position;
            transform.rotation = _self.rotation;
        }
    }
}