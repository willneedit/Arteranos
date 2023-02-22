/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

using Mirror;
using Adrenak.UniVoice;

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

        public static SettingsManager Instance { get; private set; } = null;
        public static ClientSettings Client { get; internal set; }
        public static ServerSettings Server { get; internal set; }
        public static Transform Purgatory { get; private set; }

        private CommandLine m_Command;

        private ConnectionMode m_ConnectionMode = ConnectionMode.Disconnected;

        private void Awake()
        {
            Instance = this;

            SetupPurgatory();

            ParseSettingsAndCmdLine();

        }

        private void SetupPurgatory()
        {
            Purgatory = new GameObject("_Purgatory").transform;
            Purgatory.position = new Vector3(0, -9000, 0);
            DontDestroyOnLoad(Purgatory.gameObject);
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

        private void Update()
        {
            StartNetwork();

            // Startup of dependent services...
            GetComponent<XR.XRControl>().enabled = true;
            GetComponent<Audio.VoiceManager>().enabled = true;

            // And finish the startup.
            this.enabled = false;
        }

        private void StartNetwork()
        {
            NetworkManager networkManager = GetComponentInParent<NetworkManager>();

            switch(m_ConnectionMode)
            {
                case ConnectionMode.Server:
                    networkManager.networkAddress = Server.ListenAddress;
                    networkManager.StartServer();
                    break;
                case ConnectionMode.Host:
                    networkManager.networkAddress = Server.ListenAddress;
                    networkManager.StartHost();
                    break;
                case ConnectionMode.Client:
                    networkManager.networkAddress = Client.ServerIP;
                    networkManager.StartClient();
                    break;
            }
        }
    }
}

