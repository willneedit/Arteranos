/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Mirror;

namespace Arteranos.Services
{
    public class StartupManager : SettingsManager
    {
        protected override void Update()
        {
            StartNetwork();

            // Startup of dependent services...
            GetComponent<XR.XRControl>().enabled = true;
            GetComponent<VoiceManager>().enabled = true;

            // And finish the startup.
            this.enabled = false;
        }
        protected void StartNetwork()
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

