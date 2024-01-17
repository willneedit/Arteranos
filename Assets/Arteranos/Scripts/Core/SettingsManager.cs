/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Services;
using Ipfs;
using System;
using System.Collections;
using UnityEngine;

namespace Arteranos.Core
{
    public abstract class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance;

        private CommandLine Command;

        protected static MultiHash TargetedPeerID = null;
        protected static Cid DesiredWorldCid = null;

        public static bool StartupTrigger { get; private set; } = false;

        public static ServerJSON CurrentServer { get; set; } = null;

        public static Transform Purgatory { get; private set; }
        public static Client Client { get; internal set; }
        public static Server Server { get; internal set; }
        public static ServerUserBase ServerUsers { get; internal set; }
        public static Cid WorldInfoCid { get; set; }

        public static ServerJSON ActiveServerData =>
            NetworkStatus.GetOnlineLevel() == OnlineLevel.Client
            ? CurrentServer
            : Server;

        protected virtual void Awake()
        {
            SetupPurgatory();

            ParseSettingsAndCmdLine();
        }

        protected abstract void OnDestroy();

        private void ParseSettingsAndCmdLine()
        {
            bool GetCmdArg(string key, out string val)
            {
                return Command.Commands.TryGetValue(key, out val);
            }

            bool GetBoolArg(string key, bool def = false)
            {
                if(GetCmdArg(key, out _))
                    return true;

                if(GetCmdArg("-no" + key, out _))
                    return false;

                return def;
            }

            Client = Client.Load();
            Server = Server.Load();
            ServerUsers = ServerUserBase.Load();
            Command = ScriptableObject.CreateInstance<CommandLine>();

            Command.GetCommandlineArgs();

            if(Command.PlainArgs.Count > 0)
            {
                // arteranos:/[<PeerID>]/[<WorldCid>]

                string[] parts = Command.PlainArgs[0].Split('/');
                if(parts.Length == 2 && parts[0] == "arteranos:")
                {
                    TargetedPeerID = string.IsNullOrEmpty(parts[1]) ? parts[1] : null;
                    DesiredWorldCid = (string.IsNullOrEmpty(parts[2]) ? parts[2] : null);

                    if(TargetedPeerID != null || DesiredWorldCid != null) 
                        StartupTrigger = true;
                }
            }

            Client.VRMode = GetBoolArg("-vr", Client.VRMode);
        }

        private void SetupPurgatory()
        {
            Purgatory = new GameObject("_Purgatory").transform;
            Purgatory.position = new Vector3(0, -9000, 0);
            DontDestroyOnLoad(Purgatory.gameObject);
        }

        /// <summary>
        /// Returns the connection data of the remote server (in client mode) or the
        /// local server in the server and host mode. Nulls if offline.
        /// </summary>
        /// <returns>IP address, server port, metadata port</returns>
        /// <exception cref="NotImplementedException"></exception>
        public static (string address, int port, int mdport) GetServerConnectionData()
        {
            OnlineLevel ol = NetworkStatus.GetOnlineLevel();

            return ol switch
            {
                OnlineLevel.Offline => (null, 0, 0),
                OnlineLevel.Client => (NetworkStatus.ServerHost, NetworkStatus.ServerPort, CurrentServer?.MetadataPort ?? 0),
                OnlineLevel.Server => (NetworkStatus.PublicIPAddress.ToString(), Server.ServerPort, Server.MetadataPort),
                OnlineLevel.Host => (NetworkStatus.PublicIPAddress.ToString(), Server.ServerPort, Server.MetadataPort),
                _ => throw new NotImplementedException()
            };
        }

        protected abstract bool IsSelf_(MultiHash ServerPeerID);
        protected abstract void PingServerChangeWorld_(string invoker, Cid WorldCid);
        protected abstract void StartCoroutineAsync_(Func<IEnumerator> action);

        public static void PingServerChangeWorld(string invoker, Cid WorldCid)
            => Instance.PingServerChangeWorld_(invoker, WorldCid);
        public static void StartCoroutineAsync(Func<IEnumerator> action)
            => Instance.StartCoroutineAsync_(action);

        public static bool IsSelf(MultiHash ServerPeerID)
            => Instance.IsSelf_(ServerPeerID);
    }
}