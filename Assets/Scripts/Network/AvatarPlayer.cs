using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using Unity.XR.CoreUtils;

namespace NetworkIO
{

    public class AvatarPlayer : NetworkBehaviour
    {
        public NetworkTrackedBone _self = new NetworkTrackedBone();
        public XROrigin Controller;

        public override void OnNetworkSpawn()
        {

        }

        [ServerRpc]
        public void UpdateCurrentPositionServerRpc(Vector3 position, Quaternion rotation, ServerRpcParams rpcParams = default)
        {
            _self.SetPositionAndRotation(position, rotation);
        }

        void Update()
        {
            if (IsOwner && Controller == null)
                Controller = FindObjectOfType<XROrigin>();

            // For the others, update their transforms with the server's data.
            if(!IsOwner)
                transform.SetPositionAndRotation(_self.position, _self.rotation);

            if (Controller == null)
                return;

            // Propagate the movement information to the server....            
            UpdateCurrentPositionServerRpc(Controller.transform.position, Controller.transform.rotation);

            // For the own avatar, copy the pose to the avatar rig.
            transform.SetPositionAndRotation(Controller.transform.position, Controller.transform.rotation);
            
        }
    }
}