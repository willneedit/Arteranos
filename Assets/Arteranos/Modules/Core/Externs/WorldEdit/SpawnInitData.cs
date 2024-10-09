/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using UnityEngine;
using Mirror;

namespace Arteranos.WorldEdit
{
    public interface ISpawnInitData
    {
        GameObject CoreGO { get; }
        bool? IsOnServer { get; }

        void PropagateTransform(Vector3 position, Quaternion rotation);
        void ResumeNetworkTransform();
        void ServerInit(CTSObjectSpawn newValue);
        void SuspendNetworkTransform();
    }

    public class SpawnInitData : NetworkBehaviour, ISpawnInitData
    {
        [SyncVar(hook = nameof(GotInitData))]
        private CTSObjectSpawn InitData = null;

        public GameObject CoreGO { get; private set; }
        public bool? IsOnServer { get; private set; } = null;

        private NetworkTransformBase NetworkTransform = null;

        [Client]
        public void GotInitData(CTSObjectSpawn _1, CTSObjectSpawn newValue)
        {
            Init(newValue, false);
        }

        public void ServerInit(CTSObjectSpawn newValue)
        {
            Init(newValue, true);
            InitData = newValue;
        }

        private void Init(CTSObjectSpawn newValue, bool server)
        {
            Debug.Assert(newValue != null);

            if (IsOnServer != null) return;
            IsOnServer = server;

            // Latecomer in a world with already existing spawned objects.
            SettingsManager.SetupWorldObjectRoot();

            // Set DDOL both for the shell object for the latecomer - the world is yet to load...
            DontDestroyOnLoad(gameObject);

            G.WorldEditorData.CreateSpawnObject(newValue, transform, server, go =>
            {
                CoreGO = go;

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

        // Server and Host have to remain on. Everyone still have to get the data, and
        // Host doesn't bounce the data back to its client part.
        public void SuspendNetworkTransform()
        {
            if(NetworkTransform && !IsOnServer.Value)
                NetworkTransform.enabled = false;
        }

        public void ResumeNetworkTransform()
        {
            if (NetworkTransform && !IsOnServer.Value)
                NetworkTransform.enabled = true;
        }

        public void PropagateTransform(Vector3 position, Quaternion rotation)
        {
            CoreGO.transform.SetPositionAndRotation(position, rotation);
            NetworkTransform.Reset();
        }
    }
}