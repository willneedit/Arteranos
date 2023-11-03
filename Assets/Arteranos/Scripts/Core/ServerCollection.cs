/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DERSerializer;
using UnityEngine;

namespace Arteranos.Core
{
    public struct ServerCollectionEntry
    {
        public string Name;
        public string Address;
        public int Port;
        public string Description;
        public ServerPermissions Permissions;
        public DateTime LastOnline;
        public DateTime LastUpdated;
        // Ping time, in milliseconds
        public int PingMillis;
        // Bits 0 - 29 are the history of the availability, with Bit 0 the most recent
        public int Reliability;

        public ServerCollectionEntry(ServerJSON settings, string address, int port, bool online, int ping)
        {
            Name = settings.Name;
            Port = port;
            Address = address;
            Description = settings.Description;
            Permissions = settings.Permissions;
            LastUpdated = DateTime.Now;
            Reliability = (1 << 30) - (online ? 1 : 2); // 3FFFFFFF or 3FFFFFFE :)
            LastOnline = (online ? DateTime.Now : DateTime.MinValue);
            PingMillis = ping;
        }

        /// <summary>
        /// Normalized reliability index over the past thirty attempts.
        /// </summary>
        /// <returns>0.0 means permanently offline, 1.0 means it's always online</returns>
        public readonly float ReliabilityIndex()
        {
            int count = 0;
            for (int i = 0; i < 30; i++)
                if ((Reliability & (1 << i)) != 0) count++;
            return count / 30.0f;
        }

        public readonly string Key() => Key(Address, Port);

        public static string Key(string address, int port) => $"{address}:{port}";
    }

    public class ServerCollection
    {

        public Dictionary<string, ServerCollectionEntry> entries = new();

        private readonly static Mutex SCMutex = new();
        private DateTime nextSave = DateTime.MinValue;


        public ServerCollectionEntry? Get(string address, int port)
            => entries.TryGetValue(ServerCollectionEntry.Key(address, port), out ServerCollectionEntry entry) ? entry : null;

        public async void Update(ServerCollectionEntry entry, Action<bool> callback)
        {
            bool result = await UpdateAsync(entry);
            callback(result);
        }

        /// <summary>
        /// Update (or add) the server's general data
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>true if the collection is updated, false if we have a more recent entry</returns>
        public async Task<bool> UpdateAsync(ServerCollectionEntry entry)
        {
            return await Task.Run(() => _Update(entry));

            bool _Update(ServerCollectionEntry entry)
            {
                // Critical Section Gate
                using (Guard guard = new(() => SCMutex.WaitOne(), () => SCMutex.ReleaseMutex()))
                {
                    if (entries.ContainsKey(entry.Key()))
                    {
                        // We have a more recent entry than you offered, bin it.
                        // Same as with the exactly equal time, to prevent loops
                        if (entries[entry.Key()].LastUpdated >= entry.LastUpdated) return false;
                        entries.Remove(entry.Key());
                    }
                    entries[entry.Key()] = entry;
                }

                SaveAsync();
                return true;
            }
        }

        /// <summary>
        /// Advance the server's ping and availability statistics
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="online"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <param name="ping"></param>
        public async Task<bool> UpdateAsync(string address, int port, bool online, int ping)
        {
            return await Task.Run(() => _Update(address, online, ping));

            bool _Update(string address, bool online, int ping)
            {
                // Critical Section Gate
                using (Guard guard = new(() => SCMutex.WaitOne(), () => SCMutex.ReleaseMutex()))
                {
                    ServerCollectionEntry entry = Get(address, port) ?? throw new ArgumentNullException("Update without existing entry");
                    entry.LastUpdated = DateTime.Now;
                    entry.PingMillis = ping;
                    entry.Reliability = ((entry.Reliability << 1) & ((1 << 30) - 1)) | (online ? 1 : 0);
                    if (online) entry.LastOnline = DateTime.Now;
                    entries[entry.Key()] = entry;
                }

                SaveAsync();
                return true;
            }
        }

        public List<ServerCollectionEntry> Dump(DateTime increment)
        {
            IEnumerable<ServerCollectionEntry> q = from entry in entries
                    where entry.Value.LastUpdated > increment
                    select entry.Value;
            return q.ToList();
        }

        private void Restore(List<ServerCollectionEntry> entryList)
        {
            entries.Clear();
            foreach(ServerCollectionEntry entry in entryList)
                entries.TryAdd(entry.Key(), entry);
        }

        public const string PATH_SERVER_COLLECTION = "ServerCollection.asn1";

        private readonly string oldFileName = $"{Application.persistentDataPath}/{PATH_SERVER_COLLECTION}.old";
        private readonly string currentFileName = $"{Application.persistentDataPath}/{PATH_SERVER_COLLECTION}";

        public async void SaveAsync()
        {

            // Critical Section Gate
            using Guard guard = new(() => SCMutex.WaitOne(), () => SCMutex.ReleaseMutex());

            // earliest time to next save is not passed yet.
            if (nextSave > DateTime.Now) return;

            try
            {
                if (File.Exists(oldFileName)) File.Delete(oldFileName);

                File.Move(currentFileName, oldFileName);
            }
            catch { }

            try
            {
                List<ServerCollectionEntry> obj = Dump(DateTime.MinValue);
                byte[] dataDER = Serializer.Serialize(obj);
                await File.WriteAllBytesAsync(currentFileName, dataDER);
                nextSave = DateTime.Now + TimeSpan.FromSeconds(60);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to save server collection: {e.Message}");
            }
        }

        public static ServerCollection Load()
        {
            ServerCollection sc = new()
            {
                nextSave = DateTime.MaxValue
            };

            try
            {
                byte[] dataDER = File.ReadAllBytes($"{Application.persistentDataPath}/{PATH_SERVER_COLLECTION}");
                sc.Restore(Serializer.Deserialize<List<ServerCollectionEntry>>(dataDER));
                sc.nextSave = DateTime.Now + TimeSpan.FromSeconds(60);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load server collection: {e.Message}");
                sc.nextSave = DateTime.MinValue;
            }

            return sc;
        }
    }
}