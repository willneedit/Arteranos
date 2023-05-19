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

            // Startup of dependent services...
            XR.XRControl.Instance.enabled = true;
            GetComponent<AudioManager>().enabled = true;
            GetComponent<MetaDataService>().enabled = true;
            NetworkStatus.Instance.enabled = true;

            yield return new WaitForSeconds(5);

            if(!string.IsNullOrEmpty(TargetedServerPort))
            {
                Uri uri = Utils.ProcessUriString(TargetedServerPort,
                    scheme: "http",
                    port: ServerSettingsJSON.DefaultMetadataPort
                );

                Web.ConnectionManager.ConnectToServer(uri.ToString());
            }
            else if(!string.IsNullOrEmpty(DesiredWorld))
            {
                Web.ConnectionManager.StartHost();
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

