/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Services;
using System;
using System.Collections;
using UnityEngine;

namespace Arteranos.Core
{
    public abstract class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance;

        private CommandLine Command;

        protected static string TargetedServerPort = null;
        protected static string DesiredWorld = null;

        public static bool StartupTrigger { get; private set; } = false;

        public static ServerJSON CurrentServer { get; set; } = null;

        public static Transform Purgatory { get; private set; }
        public static Client Client { get; internal set; }
        public static Server Server { get; internal set; }
        public static ServerCollection ServerCollection { get; internal set; }

        public static ServerUserBase ServerUsers { get; internal set; }

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
            ServerCollection = ServerCollection.Load();
            Command = ScriptableObject.CreateInstance<CommandLine>();

            Command.GetCommandlineArgs();

            if(Command.PlainArgs.Count > 0)
            {

                Uri uri = Utils.ProcessUriString(
                    Command.PlainArgs[0],
           
                    scheme: "arteranos",
                    port: ServerJSON.DefaultMetadataPort
                );

                Command.PlainArgs.RemoveAt(0);

                if(!string.IsNullOrEmpty(uri.Host))
                    TargetedServerPort = $"{uri.Host}:{uri.Port}";

                if(uri.AbsolutePath != "/")
                    DesiredWorld = uri.AbsolutePath[1..];

                StartupTrigger = true;

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

        protected abstract string CurrentWorld_ { get; set; }
        protected abstract void PingServerChangeWorld_(string invoker, string worldURL);
        protected abstract void StartCoroutineAsync_(Func<IEnumerator> action);

        public static string CurrentWorld { get => Instance.CurrentWorld_; set => Instance.CurrentWorld_ = value; }
        public static void PingServerChangeWorld(string invoker, string worldURL)
            => Instance.PingServerChangeWorld_(invoker, worldURL);
        public static void StartCoroutineAsync(Func<IEnumerator> action) 
            => Instance.StartCoroutineAsync_(action);
    }
}