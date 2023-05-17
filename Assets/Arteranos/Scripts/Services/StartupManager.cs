/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using System;

namespace Arteranos.Services
{
    public class StartupManager : SettingsManager
    {
        protected void Update()
        {
            ConnectionManager = GetComponent<IConnectionManager>();

            // Startup of dependent services...
            GetComponent<XR.XRControl>().enabled = true;
            GetComponent<AudioManager>().enabled = true;
            GetComponent<MetaDataService>().enabled = true;
            GetComponent<NetworkStatus>().enabled = true;

            // And finish the startup.
            this.enabled = false;

            StartNetwork();
        }
        protected void StartNetwork()
        {
            if(!string.IsNullOrEmpty(TargetedServerPort))
            {
                Uri uri = Utils.ProcessUriString(TargetedServerPort,
                    scheme: "http",
                    port: ServerSettingsJSON.DefaultMetadataPort
                );

                ConnectionManager.ConnectToServer(uri.ToString());
            }

            // FIXME Too early.
            // I need UI.WorldTransitionUI.InitiateTransition(worldURL), and the live connection.
            //if(!string.IsNullOrEmpty(DesiredWorld))
            //{
            //    Server.WorldURL = DesiredWorld;
            //}
        }
    }
}

