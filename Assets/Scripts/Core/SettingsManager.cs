/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

namespace Arteranos.Core
{
    public class SettingsManager : MonoBehaviour
    {

        protected enum ConnectionMode
        {
            Disconnected = 0,
            Client,
            Server,
            Host
        }

        private CommandLine m_Command;

        protected ConnectionMode m_ConnectionMode = ConnectionMode.Disconnected;

        public static SettingsManager Instance { get; private set; } = null;
        public static Transform Purgatory { get; private set; }
        public static ClientSettings Client { get; internal set; }
        public static ServerSettings Server { get; internal set; }

        private void Awake()
        {
            Instance = this;

            SetupPurgatory();

            ParseSettingsAndCmdLine();

        }

        private void ParseSettingsAndCmdLine()
        {
            bool GetCmdArg(string key, out string val)
            {
                return m_Command.m_Commands.TryGetValue(key, out val);
            }

            bool GetBoolArg(string key, bool def = false)
            {
                if(GetCmdArg(key, out _))
                    return true;

                if(GetCmdArg("-no" + key, out _))
                    return false;

                return def;
            }

            Client = ClientSettings.LoadSettings();
            Server = ServerSettings.LoadSettings();
            m_Command = ScriptableObject.CreateInstance<CommandLine>();

            m_Command.GetCommandlineArgs();

            if(GetCmdArg("-client", out string clientip))
            {
                Client.ServerIP = clientip;
                m_ConnectionMode = ConnectionMode.Client;
            }

            if(GetCmdArg("-server", out string serverip))
            {
                Server.ListenAddress = serverip;
                m_ConnectionMode = ConnectionMode.Server;
            }

            if(GetCmdArg("-host", out string hostip))
            {
                Server.ListenAddress = hostip;
                m_ConnectionMode = ConnectionMode.Host;
            }

            Client.VRMode = GetBoolArg("-vr", Client.VRMode);
        }

        private void SetupPurgatory()
        {
            Purgatory = new GameObject("_Purgatory").transform;
            Purgatory.position = new Vector3(0, -9000, 0);
            DontDestroyOnLoad(Purgatory.gameObject);
        }

        protected virtual void Update() { }
    }
}