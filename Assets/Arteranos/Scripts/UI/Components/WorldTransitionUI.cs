/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using UnityEngine;

using Arteranos.Core;
using Arteranos.Web;

namespace Arteranos.UI
{
    public class WorldTransitionUI
    {
        public static void ShowWorldChangeDialog(string worldURL, Action<int> resposeCallback)
        {

            WorldMetaData md = WorldGallery.RetrieveWorldMetaData(worldURL);

            string worldname = md?.WorldName ?? worldURL;

            IDialogUI dialog = DialogUIFactory.New();

            dialog.Text =
                "This server is about to change the world to\n" +
                $"{worldname}\n" +
                "What to do?";

            dialog.Buttons = new string[]
            {
                "Go offline",
                "Stay",
                "Follow"
            };

            dialog.OnDialogDone += resposeCallback;
        }

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

        private static void OnLoadWorldFaulted(Exception ex, Action failureCallback)
        {
            Debug.LogWarning($"Error in loading world: {ex.Message}");

            failureCallback?.Invoke();
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
    }
}
