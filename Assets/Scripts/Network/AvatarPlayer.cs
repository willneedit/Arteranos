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
                Controller = GameObject.Find("_XR Origin(Clone)") ?? GameObject.Find("_XR Origin KM(Clone)");
            }
        }

        [ServerRpc]
        public void UpdateCurrentPositionServerRpc(Vector3 position, Quaternion rotation, ServerRpcParams rpcParams = default)
        {
            _self.SetPositionAndRotation(position, rotation);
        }

        void Update()
        {
            if (Controller != null)
                UpdateCurrentPositionServerRpc(Controller.transform.position, Controller.transform.rotation);

            _self.PushTransform(transform);
        }
    }
}