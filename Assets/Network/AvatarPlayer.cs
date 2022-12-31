using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace NetworkIO
{
    public class AvatarPlayer : NetworkBehaviour
    {
        public NetworkVariable<Vector3>     Position = new NetworkVariable<Vector3>();
        public NetworkVariable<Quaternion>  Rotation = new NetworkVariable<Quaternion>();

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
            Position.Value = position;
            Rotation.Value = rotation;
        }

        void Update()
        {
            if (Controller != null)
                UpdateCurrentPositionServerRpc(Controller.transform.position, Controller.transform.rotation);

            transform.position = Position.Value;
            transform.rotation = Rotation.Value;
        }
    }
}