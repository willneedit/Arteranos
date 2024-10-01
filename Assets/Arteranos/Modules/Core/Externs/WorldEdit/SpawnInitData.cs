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

        [Client]
        public void GotInitData(CTSObjectSpawn _1, CTSObjectSpawn _2)
        {
            Init(InitData, false);
        }

        public void Init(CTSObjectSpawn InitData, bool server)
        {
            // Latecomer in a world with already existing spawned objects.
            SettingsManager.SetupWorldObjectRoot();

            // Set DDOL both for the framing object for the latecomer - the world is yet to load...
            DontDestroyOnLoad(gameObject);

            G.WorldEditorData.CreateSpawnObject(InitData, transform, server, go =>
            {
                if (TryGetComponent(out NetworkTransform nt))
                {
                    // Transfer the position and rotation data and its guidance to the World Editor Object
                    Vector3 oldPosition = transform.position;
                    Quaternion oldRotation = transform.rotation;
                    transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    nt.target = go.transform;
                    go.transform.SetPositionAndRotation(oldPosition, oldRotation);
                    //nt.Reset();
                }
            });
        }
    }
}