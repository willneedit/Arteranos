/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Mirror;
using UnityEngine;

namespace Arteranos.User
{
    [DisallowMultipleComponent]
    [AddComponentMenu("User/Spawn Point")]
    public class SpawnPoint : MonoBehaviour
    {
        public void Awake() => NetworkManager.RegisterStartPosition(transform);

        public void OnDestroy() => NetworkManager.UnRegisterStartPosition(transform);
    }
}
