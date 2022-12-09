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

        public void Move()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                var randomPosition = GetRandomPositionOnPlane();
                transform.position = randomPosition;
                Position.Value = randomPosition;
            }
            else
            {
                SubmitPositionRequestServerRpc();
            }
        }

        [ServerRpc]
        public void SubmitPositionRequestServerRpc(ServerRpcParams rpcParams = default)
        {
            // Position.Value = transform.position;
            Position.Value = GetRandomPositionOnPlane();
        }

        [ServerRpc]
        public void UpdateCurrentPositionServerRpc(Vector3 position, Quaternion rotation, ServerRpcParams rpcParams = default)
        {
            Position.Value = position;
            Rotation.Value = rotation;
        }

        static Vector3 GetRandomPositionOnPlane()
        {
            return new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
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