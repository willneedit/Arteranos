/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

using Mirror;

namespace Arteranos.Core
{
    public class SettingsManager : MonoBehaviour
    {
        private enum ConnectionMode {
            Disconnected = 0,
            Client,
            Server,
            Host
        }

        public ClientSettings m_Client { get; internal set; }
        public ServerSettings m_Server { get; internal set; }
        private CommandLine m_Command;

        private ConnectionMode m_ConnectionMode = ConnectionMode.Disconnected;

        private void Awake()
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

            m_Client = ClientSettings.LoadSettings();
            m_Server = ServerSettings.LoadSettings();
            m_Command = ScriptableObject.CreateInstance<CommandLine>();

            m_Command.GetCommandlineArgs();

            if(GetCmdArg("-client", out string clientip))
            {
                m_Client.ServerIP = clientip;
                m_ConnectionMode = ConnectionMode.Client;
            }

            if(GetCmdArg("-server", out string serverip))
            {
                m_Server.ListenAddress = serverip;
                m_ConnectionMode = ConnectionMode.Server;
            }

            if(GetCmdArg("-host", out string hostip))
            {
                m_Server.ListenAddress = hostip;
                m_ConnectionMode = ConnectionMode.Host;
            }

            m_Client.VRMode = GetBoolArg("-vr", m_Client.VRMode);

        }

        private void Update()
        {
            StartNetwork();

            GetComponent<Arteranos.XR.XRControl>().enabled = true;

            this.enabled = false;
        }

        private void StartNetwork()
        {
            NetworkManager networkManager = GetComponentInParent<NetworkManager>();

            switch(m_ConnectionMode)
            {
                case ConnectionMode.Server:
                    networkManager.networkAddress = m_Server.ListenAddress;
                    networkManager.StartServer();
                    break;
                case ConnectionMode.Host:
                    networkManager.networkAddress = m_Server.ListenAddress;
                    networkManager.StartHost();
                    break;
                case ConnectionMode.Client:
                    networkManager.networkAddress = m_Client.ServerIP;
                    networkManager.StartClient();
                    break;
            }
        }
    }
}

