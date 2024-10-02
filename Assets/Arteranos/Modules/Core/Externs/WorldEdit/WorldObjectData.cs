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
    // Server-only data storage for transient data
    public class WorldObjectData : MonoBehaviour
    {
        public int SpawnedItems { get; set; } = 0;
    }
}