/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using UnityEngine;
using Mirror;
using System;

namespace Arteranos.WorldEdit
{
    public class SpawnInitData : NetworkBehaviour
    {
        [SyncVar(hook = nameof(GotInitData))]
        public CTSObjectSpawn InitData = null;

        public GameObject CoreGO {  get; private set; }

        [Client]
        public void GotInitData(CTSObjectSpawn _1, CTSObjectSpawn _2)
        {
            Init(InitData, false);
        }

        public void Init(CTSObjectSpawn InitData, bool server)
        {
            // Latecomer in a world with already existing spawned objects.
            SettingsManager.SetupWorldObjectRoot();

            // Set DDOL both for the shell object for the latecomer - the world is yet to load...
            DontDestroyOnLoad(gameObject);

            G.WorldEditorData.CreateSpawnObject(InitData, transform, server, go =>
            {
                CoreGO = go;

                if (TryGetComponent(out NetworkTransform nt))
                {
                    // Transfer the position and rotation data and its guidance to the World Editor Object
                    Vector3 oldPosition = transform.position;
                    Quaternion oldRotation = transform.rotation;
                    transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    nt.target = go.transform;
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
    }
}