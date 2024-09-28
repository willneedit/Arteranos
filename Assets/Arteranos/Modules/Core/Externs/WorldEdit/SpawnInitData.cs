/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using ProtoBuf;

using Arteranos.Core;
using UnityEngine;
using System.IO;
using System;
using System.Threading;
using Ipfs.Unity;
using GLTFast;
using System.Threading.Tasks;
using AssetBundle = Arteranos.Core.Managed.AssetBundle;
using System.Reflection;
using Mirror;

namespace Arteranos.WorldEdit
{
    public class SpawnInitData : NetworkBehaviour
    {
        [SyncVar(hook = nameof(GotInitData))]
        public CTSObjectSpawn InitData = null;

        public void GotInitData(CTSObjectSpawn _1, CTSObjectSpawn _2)
        {
            Debug.Log($"Got Init Data, server={isServer}");

            Init(InitData);
        }

        public void Init(CTSObjectSpawn InitData)
        {
            G.WorldEditorData.CreateSpawnObject(InitData, transform);
        }
    }
}