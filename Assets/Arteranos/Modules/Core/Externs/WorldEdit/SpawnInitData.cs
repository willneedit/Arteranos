/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using UnityEngine;
using Mirror;
using Arteranos.Services;

namespace Arteranos.WorldEdit
{

    public interface ISpawnInitData
    {
        void ServerInit(CTSObjectSpawn newValue);
    }

    public class SpawnInitData : NetworkBehaviour, ISpawnInitData, IEnclosingObject
    {
        [SyncVar]
        private CTSObjectSpawn InitData = null;

        public bool? IsOnServer { get; private set; } = null;

        public GameObject EnclosedObject { get; set; } = null;

        private NetworkTransformBase NetworkTransform = null;

        public void ServerInit(CTSObjectSpawn newValue)
        {
            InitData = newValue;

            if (!NetworkServer.active) Init(newValue);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            // Debug.Log($"OnStartServer, s={isServer}, netId={netId}");

            IsOnServer = isServer;
            Init(InitData);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            // Debug.Log($"OnStartClient, s={isServer}, netId={netId}");

            // Don't do that twice on host mode.
            if(!isServer)
            {
                IsOnServer = isServer;
                Init(InitData);
            }
        }

        private Rigidbody Rigidbody = null;
        private bool? wasKinematic = null;

        private void Init(CTSObjectSpawn newValue)
        {
            // TODO Perhaps a network ID confusion because of a botched initialization.
            // The connection to the core GO is mistakenly to the *last* one
            Debug.Assert(newValue != null);

            gameObject.name = $"Spawned Object s={IsOnServer}, nid={netId}";

            // Latecomer in a world with already existing spawned objects.
            SettingsManager.SetupWorldObjectRoot();

            // Set DDOL both for the shell object for the latecomer - the world is yet to load...
            DontDestroyOnLoad(gameObject);

            G.WorldEditorData.CreateSpawnObject(newValue, transform, go =>
            {
                if (!go)
                {
                    Debug.LogError("CreateSpawnObject didn't return an object!");
                    return;
                }

                this.EnclosedObject = go;

                Debug.Assert(go.TryGetComponent(out IEnclosedObject o) && o.EnclosingObject == gameObject);

                if (TryGetComponent(out NetworkTransform))
                {
                    // Transfer the position and rotation data and its guidance to the World Editor Object
                    Vector3 oldPosition = transform.position;
                    Quaternion oldRotation = transform.rotation;
                    transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    NetworkTransform.target = go.transform;
                    go.transform.SetPositionAndRotation(oldPosition, oldRotation);

                    // Default is orphaned object, server takes the role of a foster parent
                    // and the clients have to stay silent, fornow.
                    if (isServer) NetworkTransform.syncDirection = SyncDirection.ServerToClient;
                }

                if(go.TryGetComponent(out Rigidbody))
                {
                    // Set the default kinematic state.
                    wasKinematic = Rigidbody.isKinematic;
                }
            });
        }

        // Even if the shell object is destroyed (maybe, scene changes, or by expiring)
        // do destroy the enclosing object, even if it's detached by grab.
        private void OnDestroy()
        {
            Destroy(EnclosedObject);
        }

        // ---------------------------------------------------------------

        [Server]
        public void ChangeAuthority(GameObject targetGO, bool auth)
        {
            // Debug.Log($"[{gameObject.name}] Authority change: o={targetGO.name}, auth={auth}");

            if (!targetGO.TryGetComponent(out NetworkIdentity targetIdentity))
            {
                Debug.LogError($"{targetGO.name} has no NetworkIdentity");
                return;
            }

            NetworkTransform.syncDirection = auth
                ? SyncDirection.ClientToServer  // true: let the client guide the flow
                : SyncDirection.ServerToClient; // false: Server takes the helm

            if (Rigidbody)
                Rigidbody.isKinematic = auth
                    || wasKinematic.Value;       // false: revert to the default

            if (auth)
                netIdentity.AssignClientAuthority(targetIdentity.connectionToClient);
            else
                netIdentity.RemoveClientAuthority();

            NetworkTransform.Reset();
        }
    }
}