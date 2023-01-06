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

        // [ServerRpc]
        // public void UpdateCurrentPositionServerRpc(Vector3 position, Quaternion rotation, ServerRpcParams rpcParams = default)
        // {
        //     _self.SetPositionAndRotation(position, rotation);
        // }

        void Update()
        {
            if (IsOwner && Controller == null)
                Controller = FindObjectOfType<XROrigin>();


            // Propagate the movement information to the server, if we're ourselves...
            if(IsOwner)
                _self.SetPositionAndRotation(Controller.transform.position, Controller.transform.rotation);
    
            transform.SetPositionAndRotation(_self.position, _self.rotation);

            
        }
    }
}