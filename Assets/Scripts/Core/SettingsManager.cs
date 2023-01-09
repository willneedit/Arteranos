using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace Core
{
    public class SettingsManager : MonoBehaviour
    {
        public ClientSettings m_Client;
        public ServerSettings m_Server;
        private CommandLine m_Command;

        void Awake()
        {
            m_Client = ClientSettings.LoadSettings();
            m_Server = ServerSettings.LoadSettings();
            m_Command = ScriptableObject.CreateInstance<CommandLine>();

            m_Command.ParseCommandLine();

        }

        void Update()
        {
            StartNetwork();

            GetComponent<XRControl>().enabled = true;

            this.enabled = false;
        }

        void StartNetwork()
        {
            NetworkManager netManager = GetComponentInParent<NetworkManager>();

            if (m_Command.m_Commands.TryGetValue("-mode", out string mode))
            {
                switch (mode)
                {
                    case "server":
                        netManager.StartServer();
                        break;
                    case "host":
                        netManager.StartHost();
                        break;
                    case "client":
                        netManager.StartClient();
                        break;
                }
            }

        }
    }
}

