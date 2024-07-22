/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Avatar;
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

        public Blueprints Blueprints;

        private CommandLine Command;

        protected static MultiHash TargetedPeerID = null;
        protected static Cid DesiredWorldCid = null;

        public static bool StartupTrigger { get; private set; } = false;

        public static ServerJSON CurrentServer { get; set; } = null;

        public static Transform Purgatory { get; private set; }
        public static Client Client { get; internal set; }
        public static Server Server { get; internal set; }
        public static ServerUserBase ServerUsers { get; internal set; }
        public static Cid WorldCid { get; set; }
        public static string WorldName { get; set; }
        public static string DefaultTOStext { get; private set; } = null;

        public static Cid DefaultMaleAvatar { get; set; } = null;
        public static Cid DefaultFemaleAvatar { get; set; } = null;

        public static ServerJSON ActiveServerData =>
            G.NetworkStatus.GetOnlineLevel() == OnlineLevel.Client
            ? CurrentServer
            : Server;

        protected virtual void Awake()
        {
            BP.I = Blueprints;

            SetupPurgatory();

            ParseSettingsAndCmdLine();

            DefaultTOStext = BP.I.PrivacyTOSNotice.text;
            DefaultMaleAvatar = null;
            DefaultFemaleAvatar = null;
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
        /// <returns>Peer ID, the own one or the remote</returns>
        /// <exception cref="NotImplementedException"></exception>
        public static MultiHash GetServerConnectionData()
        {
            OnlineLevel ol = G.NetworkStatus.GetOnlineLevel();

            return ol switch
            {
                OnlineLevel.Offline => null,
                OnlineLevel.Client => G.NetworkStatus.RemotePeerId,
                OnlineLevel.Server => G.IPFSService.Self.Id,
                OnlineLevel.Host => G.IPFSService.Self.Id,
                _ => throw new NotImplementedException()
            };
        }

        protected abstract void EmitToClientCTSPacket_(CTSPacket packet, IAvatarBrain to = null);
        protected abstract void EmitToServerCTSPacket_(CTSPacket packet);

        protected abstract event Action<UserID, ServerUserState> OnClientReceivedServerUserStateAnswer_;
        protected abstract event Action<ServerJSON> OnClientReceivedServerConfigAnswer_;


        public static void EmitToServerCTSPacket(CTSPacket packet)
            => Instance?.EmitToServerCTSPacket_(packet);

        public static void EnterWorld(Cid WorldCid)
        {
            WorldInfo wi = WorldInfo.DBLookup(WorldCid);

            EmitToServerCTSPacket(new CTSPWorldChangeAnnouncement()
            {
                WorldInfo = wi?.Strip()
            });
        }

        public static event Action<UserID, ServerUserState> OnClientReceivedServerUserStateAnswer
        {
            add => Instance.OnClientReceivedServerUserStateAnswer_ += value;
            remove { if (Instance != null) Instance.OnClientReceivedServerUserStateAnswer_ -= value; }
        }

        public static event Action<ServerJSON> OnClientReceivedServerConfigAnswer
        {
            add => Instance.OnClientReceivedServerConfigAnswer_ += value;
            remove {  if (Instance != null) Instance.OnClientReceivedServerConfigAnswer_ -= value; }
        }

        public static void Quit()
        {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.ExitPlaymode();
#else
                UnityEngine.Application.Quit();
#endif
        }
    }
}