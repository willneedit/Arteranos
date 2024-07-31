/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Arteranos.User
{
    [DisallowMultipleComponent]
    [AddComponentMenu("User/Spawn Point")]
    public class SpawnPoint : MonoBehaviour
    {
        public void Awake() => SpawnManager.RegisterSpawnPoint(transform);

        public void OnDestroy() => SpawnManager.UnregisterSpawnPoint(transform);
    }

    public class SpawnManager
    {
        public static List<Transform> spawnPoints = new();

        public static void RegisterSpawnPoint(Transform start)
        {
            spawnPoints.Add(start);
            spawnPoints = spawnPoints.OrderBy(transform => transform.GetSiblingIndex()).ToList();
        }

        public static void UnregisterSpawnPoint(Transform start) => spawnPoints.Remove(start);

        public static Transform GetStartPosition()
        {
            // first remove any dead transforms
            spawnPoints.RemoveAll(t => t == null);

            if(spawnPoints.Count == 0) return null;
    
            return spawnPoints[Random.Range(0, spawnPoints.Count)];
        }
    }
}
