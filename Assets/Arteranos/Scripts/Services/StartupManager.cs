/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Arteranos.Web;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arteranos.Services
{
    public class StartupManager : SettingsManager
    {
        private bool initialized = false;


        IEnumerator StartupCoroutine()
        {
            if(string.IsNullOrEmpty(DesiredWorld))
            {
                AsyncOperation ao = SceneManager.LoadSceneAsync("OfflineScene");

                if(!ao.isDone)
                    yield return new WaitForEndOfFrame();
            }

            // Startup of dependent services...
            AudioManager.Instance.enabled = true;
            GetComponent<MetaDataService>().enabled = true;
            NetworkStatus.Instance.enabled = true;

            if(!string.IsNullOrEmpty(TargetedServerPort))
            {
                Uri uri = Utils.ProcessUriString(TargetedServerPort,
                    scheme: "http",
                    port: ServerJSON.DefaultMetadataPort
                );

                ConnectionManager.ConnectToServer(uri.ToString());
            }
            else if(!string.IsNullOrEmpty(DesiredWorld))
            {
                NetworkStatus.StartHost();
            }

            XR.XRControl.Instance.enabled = true;

            yield return new WaitForEndOfFrame();

            // Enter the initial world, if we're not starting up with a startup trigger
            if(string.IsNullOrEmpty(DesiredWorld))
                WorldDownloaderLow.MoveToDownloadedWorld();

            // Finish the startup...
            enabled = false;

            // ... and raise the curtains. Though, keep waiting if we have to load the world.
            if(string.IsNullOrEmpty(DesiredWorld))
                XR.ScreenFader.StartFading(0.0f, 2.0f);
        }

        protected void Update()
        {
            if(initialized) return;

            initialized = true;

            // Initialize with the black screen
            XR.ScreenFader.StartFading(1.0f, 0.0f);

            StartCoroutine(StartupCoroutine());
        }
    }
}

