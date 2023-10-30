/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;

namespace Arteranos.Core
{
    public class ServerCollectionEntry
    {
        public string Name = string.Empty;
        public string Address = null;
        public string Description = string.Empty;
        public ServerPermissions Permissions = new();
        public DateTime LastOnline = DateTime.MinValue;
        public DateTime LastUpdated = DateTime.MinValue;
        // Ping time, in milliseconds
        public int PingMillis = 0;
        // Bits 0 - 29 are the history of the availability, with Bit 0 the most recent
        public int Reliability = (1 << 30) - 1;
    }

    public class ServerCollection
    {
        public Dictionary<string, ServerCollectionEntry> entries = new();

        public ServerCollectionEntry Get(string address) 
            => entries.TryGetValue(address, out ServerCollectionEntry entry) ? entry : null;

        /// <summary>
        /// Update (or add) the server's general data
        /// </summary>
        /// <param name="entry"></param>
        public void Update(ServerCollectionEntry entry)
        {
            if(entries.ContainsKey(entry.Address)) entries.Remove(entry.Address);
            entries[entry.Address] = entry;
            entries[entry.Address].LastUpdated = DateTime.Now;
        }

        /// <summary>
        /// Advance the server's ping and availability statistics
        /// </summary>
        /// <param name="address"></param>
        /// <param name="available"></param>
        /// <param name="ping"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Update(string address, bool available, int ping)
        {
            ServerCollectionEntry entry = Get(address) ?? throw new ArgumentNullException("Update without existing entry");
            entry.LastUpdated = DateTime.Now;
            entry.PingMillis = ping;
            entry.Reliability = ((entry.Reliability << 1) & ((1 << 30) - 1)) | (available ? 1 : 0);
            if (available) entry.LastOnline = DateTime.Now;
        }
    }
}