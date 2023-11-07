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
    public struct ServerOnlineData
    {
        public int ServerPort;
        public byte[] ServerPublicKey;
        public byte[] Icon;
        public List<byte[]> UserPublicKeys;
        public string CurrentWorld;
    }

    public struct ServerPublicData
    {
        public string Name;
        public string Address;
        public int Port;
        public string Description;
        public ServerPermissions Permissions;
        public DateTime LastOnline;
        public DateTime LastUpdated;

        public ServerPublicData(ServerJSON settings, string address, int port, bool online)
        {
            Name = settings.Name;
            Port = port;
            Address = address;
            Description = settings.Description;
            Permissions = settings.Permissions;
            LastUpdated = DateTime.Now;
            LastOnline = (online ? DateTime.Now : DateTime.MinValue);
        }

        public ServerPublicData(string address, int port)
        {
            Name = string.Empty;
            Address = address;
            Port = port;
            Description = string.Empty;
            Permissions = new();
            LastOnline = DateTime.UnixEpoch;
            LastUpdated = DateTime.Now;
        }

        public readonly string Key() => Key(Address, Port);

        public static string Key(string address, int port) => $"{address}:{port}";

        /// <summary>
        /// Ping and update the server
        /// </summary>
        /// <returns>true if it seems to be alive</returns>
        public async Task<(ServerPublicData, bool)> PingServer()
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
            return (this, success);
        }

        /// <summary>
        /// Retrieve the essential server data
        /// </summary>
        /// <param name="timeout">Timeout in seconds</param>
        /// <returns>The updated public data, the online data</returns>
        public async Task<(ServerPublicData, ServerOnlineData?)> GetServerDataAsync(int timeout = 20)
        {
            Uri uri = new($"http://{Address}:{Port}{ServerJSON.DefaultMetadataPath}");

            DownloadHandlerBuffer dh = new();
            using UnityWebRequest uwr = new(
                uri,
                UnityWebRequest.kHttpVerbGET,
                dh,
                null);

            uwr.timeout = timeout;

            UnityWebRequestAsyncOperation uwr_ao = uwr.SendWebRequest();

            while (!uwr_ao.isDone) await Task.Yield();

            ServerMetadataJSON smdj = null;

            bool success = uwr.result == UnityWebRequest.Result.Success;

            LastUpdated = DateTime.Now;

            if (success)
            {
                LastOnline = DateTime.Now;
                smdj = Serializer.Deserialize<ServerMetadataJSON>(dh.data);

                Name = smdj.Settings.Name;
                Description = smdj.Settings.Description;
                Permissions = smdj.Settings.Permissions;

                return (this, new ServerOnlineData()
                {
                    ServerPort = smdj.Settings.ServerPort,
                    ServerPublicKey = smdj.Settings.ServerPublicKey,
                    Icon = smdj.Settings.Icon,
                    CurrentWorld = smdj.CurrentWorld,
                    UserPublicKeys = smdj.CurrentUsers
                });
            }
            else return (this, null);
        }

        public static async Task<(ServerPublicData?, ServerOnlineData?)> GetServerDataAsync(string address, int port, int timeout = 20)
        {
            ServerCollection sc = SettingsManager.ServerCollection;

            ServerPublicData? stored = sc.Get(address, port);

            ServerPublicData work = stored ?? new(address, port);

            ServerOnlineData? result;
            (work, result) = await work.GetServerDataAsync(timeout);

            if(stored != null) _ = sc.UpdateAsync(work); 
            
            return (stored, result);
        }

        public static Task<(ServerPublicData?, ServerOnlineData?)> GetServerDataAsync(string url, int timeout = 20)
        {
            Uri uri = Utils.ProcessUriString(url,
                scheme: "http",
                port: ServerJSON.DefaultMetadataPort,
                path: ServerJSON.DefaultMetadataPath
                );

            return GetServerDataAsync(uri.Host, uri.Port, timeout);
        }
    }

    public class ServerCollection
    {

        public Dictionary<string, ServerPublicData> entries = new();

        private readonly static Mutex SCMutex = new();
        private DateTime nextSave = DateTime.MinValue;

        public ServerPublicData? Get(string key)
            => entries.TryGetValue(key, out ServerPublicData entry) ? entry : null;

        public ServerPublicData? Get(string address, int port)
            => Get(ServerPublicData.Key(address, port));

        public ServerPublicData? Get(Uri uri)
            => Get(uri.Host, uri.Port);

        /// <summary>
        /// Update (or add) the server's general data
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>true if the collection is updated, false if we have a more recent entry</returns>
        public async Task<bool> UpdateAsync(ServerPublicData entry)
        {
            return await Task.Run(() => _Update(entry));

            bool _Update(ServerPublicData entry)
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

        public List<ServerPublicData> Dump(DateTime increment)
        {
            IEnumerable<ServerPublicData> q = from entry in entries
                    where entry.Value.LastUpdated > increment
                    select entry.Value;
            return q.ToList();
        }

        private void Restore(List<ServerPublicData> entryList)
        {
            entries.Clear();
            foreach(ServerPublicData entry in entryList)
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
                List<ServerPublicData> obj = Dump(DateTime.MinValue);
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
                sc.Restore(Serializer.Deserialize<List<ServerPublicData>>(dataDER));
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