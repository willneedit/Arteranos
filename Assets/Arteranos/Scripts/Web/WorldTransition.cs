using Arteranos.Core;
using Arteranos.UI;
using System;
using UnityEngine;
/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

namespace Arteranos.Web
{
    public static class WorldTransition
    {

        public static void InitiateTransition(string url, Action failureCallback = null, Action successCallback = null)
        {
            IProgressUI pui = ProgressUIFactory.New();

            //pui.PatienceThreshold = 0f;
            //pui.AlmostFinishedThreshold = 0f;

            pui.AllowCancel = true;

            (pui.Executor, pui.Context) = WorldDownloader.PrepareDownloadWorld(url, true);

            pui.Completed += (context) => OnLoadWorldComplete(url, context, successCallback);
            pui.Faulted += (ex, context) => OnLoadWorldFaulted(ex, failureCallback);
        }

        private static void OnLoadWorldComplete(string worldURL, Context _context, Action successCallback)
        {
            ServerSettings ss = SettingsManager.Server;

            WorldDownloader.EnterDownloadedWorld(_context);

            // Only then the URL is saved on the successful loading, otherwise the server
            // setting in this session is discarded.

            // Like with a smartphone - To restart a world, turn it off and back on.
            ss.WorldURL = null;
            ss.WorldURL = worldURL;

            successCallback?.Invoke();
        }

        private static void OnLoadWorldFaulted(Exception ex, Action failureCallback)
        {
            Debug.LogWarning($"Error in loading world: {ex.Message}");

            failureCallback?.Invoke();
        }
    }
}