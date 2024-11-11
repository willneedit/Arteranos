/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

namespace Arteranos.Core
{
    // Random (like, other things) need to be used from the main tasks, and
    // class fields have to be there on initialization, even if the Unity Engine
    // yells at you.
    public class SessionConstants
    {
        public static SessionConstants Instance = new();

        public readonly string DefaultServerName;
        public readonly string DefaultUserName;

        private SessionConstants() 
        {
            DefaultServerName = $"Unconfigured server {Random.Range(1000, 1000000000)}";

            DefaultUserName = $"Anonymous {Random.Range(1000, 1000000000)}";
        }
    }
}