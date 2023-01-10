using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Core
{
    public class SettingsManager : MonoBehaviour
    {
        private enum ConnectionMode {
            Disconnected = 0,
            Client,
            Server,
            Host
        }

        public ClientSettings m_Client;
        public ServerSettings m_Server;
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
                string dummy;
                bool result = def;

                if(GetCmdArg(key, out dummy))
                    return result = true;

                if(GetCmdArg("no-" + key, out dummy))
                    return result = false;
                
                return def;
            }

            m_Client = ClientSettings.LoadSettings();
            m_Server = ServerSettings.LoadSettings();
            m_Command = ScriptableObject.CreateInstance<CommandLine>();

            m_Command.ParseCommandLine();

            if(GetCmdArg("client", out string clientip))
            {
                m_Client.ServerIP = clientip;
                m_ConnectionMode = ConnectionMode.Client;
            }

            if(GetCmdArg("server", out string serverip))
            {
                m_Server.ListenAddress = serverip;
                m_ConnectionMode = ConnectionMode.Server;
            }

            if(GetCmdArg("host", out string hostip))
            {
                m_Server.ListenAddress = hostip;
                m_ConnectionMode = ConnectionMode.Host;
            }

            // if(GetCmdArg("mode", out string mode))
            // {
            //     switch(mode)
            //     {
            //         case "server":
            //             m_ConnectionMode = ConnectionMode.Server;
            //             break;
            //         case "client":
            //             m_ConnectionMode = ConnectionMode.Client;
            //             break;
            //         case "host":
            //             m_ConnectionMode = ConnectionMode.Host;
            //             break;
            //     }
            // }

            m_Client.VRMode = GetBoolArg("vr", m_Client.VRMode);

        }

        private void Update()
        {
            StartNetwork();

            GetComponent<XRControl>().enabled = true;

            this.enabled = false;
        }

        private void StartNetwork()
        {
            NetworkManager netManager = GetComponentInParent<NetworkManager>();
            UnityTransport transport = GetComponentInParent<UnityTransport>();

            switch(m_ConnectionMode)
            {
                case ConnectionMode.Server:
                    transport.ConnectionData.ServerListenAddress = m_Server.ListenAddress;
                    netManager.StartServer();
                    break;
                case ConnectionMode.Host:
                    transport.ConnectionData.ServerListenAddress = m_Server.ListenAddress;
                    netManager.StartHost();
                    break;
                case ConnectionMode.Client:
                    transport.ConnectionData.Address = m_Client.ServerIP;
                    netManager.StartClient();
                    break;
            }
        }
    }
}

