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
using UnityEngine.Networking;

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

        public ServerCollectionEntry(ServerJSON settings, string address, int port, bool online)
        {
            Name = settings.Name;
            Port = port;
            Address = address;
            Description = settings.Description;
            Permissions = settings.Permissions;
            LastUpdated = DateTime.Now;
            LastOnline = (online ? DateTime.Now : DateTime.MinValue);
        }

        public readonly string Key() => Key(Address, Port);

        public static string Key(string address, int port) => $"{address}:{port}";

        public async Task<bool> PingServer()
        {
            Uri uri = new($"http://{Address}:{Port}/");

            DownloadHandlerBuffer dh = new();
            using UnityWebRequest uwr = new(
                uri,
                UnityWebRequest.kHttpVerbHEAD,
                dh,
                null);
            uwr.timeout = 1;

            UnityWebRequestAsyncOperation uwr_ao = uwr.SendWebRequest();
            while (!uwr_ao.isDone) await Task.Yield();

            bool success = uwr.result == UnityWebRequest.Result.Success;

            LastUpdated = DateTime.Now;
            if (success) LastOnline = DateTime.Now;
            return success;
        }
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