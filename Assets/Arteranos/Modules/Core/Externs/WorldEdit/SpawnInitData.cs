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
        void PropagateTransform(Vector3 position, Quaternion rotation);
        void ResumeNetworkTransform();
        void ServerInit(CTSObjectSpawn newValue);
        void SuspendNetworkTransform();
    }

    public class SpawnInitData : NetworkBehaviour, ISpawnInitData, IEnclosingObject
    {
        [SyncVar]
        private CTSObjectSpawn InitData = null;

        public bool? IsOnServer { get; private set; } = null;

        public GameObject EnclosedObject => CoreGO;

        private NetworkTransformBase NetworkTransform = null;
        private GameObject CoreGO = null;

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
                CoreGO = go;

                Debug.Assert(go.TryGetComponent(out IEnclosedObject o) && o.EnclosingObject == gameObject);

                if (TryGetComponent(out NetworkTransform))
                {
                    // Transfer the position and rotation data and its guidance to the World Editor Object
                    Vector3 oldPosition = transform.position;
                    Quaternion oldRotation = transform.rotation;
                    transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    NetworkTransform.target = go.transform;
                    go.transform.SetPositionAndRotation(oldPosition, oldRotation);
                }
            });
        }

        // Even if the shell object is destroyed (maybe, scene changes, or by expiring)
        // do destroy the enclosing object, even if it's detached by grab.
        private void OnDestroy()
        {
            Destroy(CoreGO);
        }

        // TODO Doesn't work. Inequal enabled state causes misattributing network transform data.

        // Server and Host have to remain on. Everyone still have to get the data, and
        // Host doesn't bounce the data back to its client part.
        public void SuspendNetworkTransform()
        {
            //if(NetworkTransform && IsOnServer == false)
            //    NetworkTransform.enabled = false;
        }

        public void ResumeNetworkTransform()
        {
            //if (NetworkTransform && IsOnServer == false)
            //    NetworkTransform.enabled = true;
        }

        public void PropagateTransform(Vector3 position, Quaternion rotation)
        {
            CoreGO.transform.SetPositionAndRotation(position, rotation);
            NetworkTransform.Reset();
        }
    }
}