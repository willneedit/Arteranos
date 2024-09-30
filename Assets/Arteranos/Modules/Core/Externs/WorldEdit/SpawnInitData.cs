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
            Debug.Log($"Got Init Data, server={isServer}");

            Init(InitData, false);
        }

        public void Init(CTSObjectSpawn InitData, bool server)
        {
            G.WorldEditorData.CreateSpawnObject(InitData, transform, server, go =>
            {
                if (TryGetComponent(out NetworkTransform nt))
                    nt.target = go.transform;
            });
        }
    }
}