/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arteranos
{
    public class SpawnerTest : MonoBehaviour
    {
        private void Start()
        {
            NetworkClient.RegisterPrefab(ToSpawn);
        }

        private void OnDestroy()
        {
            NetworkClient.UnregisterPrefab(ToSpawn);
        }

        public GameObject ToSpawn;

        public void TriggerSpawn()
        {
            SettingsManager.EmitToServerCTSPacket(new CTSObjectSpawn());
        }
    }
}
