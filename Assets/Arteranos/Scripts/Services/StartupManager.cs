/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using System;
using System.Collections;
using UnityEngine;

namespace Arteranos.Services
{
    public class StartupManager : SettingsManager
    {
        private bool initialized = false;

        public static bool StartupTrigger { get; private set; } = true;

        public static string ResetStartupTrigger()
        {
            StartupTrigger = false;
            return DesiredWorld;
        }

        IEnumerator StartupCoroutine()
        {
            yield return new WaitForEndOfFrame();

            ConnectionManager = GetComponent<Web.IConnectionManager>();

            // Startup of dependent services...
            GetComponent<XR.XRControl>().enabled = true;
            GetComponent<AudioManager>().enabled = true;
            GetComponent<MetaDataService>().enabled = true;
            GetComponent<NetworkStatus>().enabled = true;

            yield return new WaitForSeconds(5);

            if(!string.IsNullOrEmpty(TargetedServerPort))
            {
                Uri uri = Utils.ProcessUriString(TargetedServerPort,
                    scheme: "http",
                    port: ServerSettingsJSON.DefaultMetadataPort
                );

                ConnectionManager.ConnectToServer(uri.ToString());
            }
            else if(!string.IsNullOrEmpty(DesiredWorld))
            {
                ConnectionManager.StartHost();
            }

            // And finish the startup.
            enabled = false;
        }

        protected void Update()
        {
            if(initialized) return;

            initialized = true;

            StartCoroutine(StartupCoroutine());
        }
        protected void StartNetwork()
        {

            // FIXME Too early.
            // I need UI.WorldTransitionUI.InitiateTransition(worldURL), and the live connection.
            //if(!string.IsNullOrEmpty(DesiredWorld))
            //{
            //    Server.WorldURL = DesiredWorld;
            //}
        }
    }
}

