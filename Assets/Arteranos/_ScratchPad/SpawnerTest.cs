/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arteranos
{
    public class SpawnerTest : MonoBehaviour
    {
        public GameObject ToSpawn;

        public void TriggerSpawn()
        {
            GameObject spawned = Instantiate(ToSpawn, 
                transform.position + transform.TransformDirection(Vector3.up), transform.rotation);
            if(NetworkServer.active) NetworkServer.Spawn(spawned);
        }
    }
}
