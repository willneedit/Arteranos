/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Concurrent;

namespace Arteranos.Core
{
    public partial class ServerOnlineData
    {
        // Set on receive in Services.IPFSService
        public DateTime LastOnline;

        private static ConcurrentDictionary<string, ServerOnlineData> _OnlineData = new();

        public static ServerOnlineData DBLookup(string key) 
            => _OnlineData.TryGetValue(key, out ServerOnlineData sod) ? sod : null;

        public static void DBDelete(string key) 
            => _OnlineData.TryRemove(key, out _);

        public void DBInsert(string key) 
            => _OnlineData.AddOrUpdate(key, this, (key, val) => this);
    }
}
