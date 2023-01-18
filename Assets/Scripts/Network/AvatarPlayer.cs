using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using Unity.XR.CoreUtils;

namespace Arteranos.NetworkIO
{

    public class AvatarPlayer : NetworkBehaviour
    {
        public XROrigin Controller;

        public override void OnNetworkSpawn()
        {

        }

        void Update()
        {
            if(!IsOwner) return;

            // Could drop to null b/c VR/2D transition
            if (Controller == null)
                Controller = FindObjectOfType<XROrigin>();

            if (Controller == null)
                return;

            // Own avatar get copied from the controller, alien avatars by NetworkTransform.
            transform.SetPositionAndRotation(Controller.transform.position, Controller.transform.rotation);
            
        }
    }
}