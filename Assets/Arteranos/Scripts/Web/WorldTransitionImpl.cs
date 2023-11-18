﻿using Arteranos.Core;
using Arteranos.Services;
using Arteranos.UI;
using Arteranos.XR;
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
* Copyright (c) 2023, willneedit
* 
* Licensed by the Mozilla Public License 2.0,
* residing in the LICENSE.md file in the project's root directory.
*/

namespace Arteranos.Web
{
    public class WorldTransitionImpl : MonoBehaviour, IWorldTransition
    {
        private void Awake() => WorldTransition.Instance = this;
        private void OnDestroy() => WorldTransition.Instance = null;

        private IEnumerator EnterDownloadedWorldCoroutine(string worldABF)
        {
            Debug.Log($"Download complete, world={worldABF}");

            yield return null;

            // Deploy the scene loader.
            GameObject go = new("_SceneLoader");
            go.AddComponent<Persistence>();
            SceneLoader sl = go.AddComponent<SceneLoader>();
            sl.OnFinishingSceneChange += () => XRControl.Instance.MoveRig();
            sl.Name = worldABF;
        }

        public async Task<(Exception, WorldData)> GetWorldDataAsync(string worldURL)
        {
            static (Exception, WorldData) _GetWorldData() => (null, new WorldData());

            return await Task.Run(_GetWorldData);
        }

        public bool IsWorldPreloaded(string worldURL) 
            => File.Exists(WorldDownloader.GetTouchFile(worldURL));

        public async Task MoveToOfflineWorld()
        {
            bool done = false;
            IEnumerator OfflineScene()
            {
                AsyncOperation ao = SceneManager.LoadSceneAsync("OfflineScene");
                while (!ao.isDone) yield return null;

                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();

                XRControl.Instance.MoveRig();

                SettingsManager.CurrentWorld = null;
                ScreenFader.StartFading(0.0f);
                done = true;
            }
            
            SettingsManager.StartCoroutineAsync(OfflineScene);
            while(!done) await Task.Yield();
        }

        public Task MoveToOnlineWorld(string worldURL)
        {
            void Enter_()
            {
                string worldABF = WorldDownloader.GetWorldABF(worldURL);

                EnterDownloadedWorld(worldABF);
                SettingsManager.CurrentWorld = worldURL;
            }

            return Task.Run(Enter_);
        }

        public async Task<(Exception, Context)> PreloadWorldDataAsync(string worldURL, bool forceReload = false)
        {
            IProgressUI pui = ProgressUIFactory.New();

            //pui.PatienceThreshold = 0f;
            //pui.AlmostFinishedThreshold = 0f;

            pui.AllowCancel = true;

            // FIXME See #71
            pui.SetupAsyncOperations(() => WorldDownloader.PrepareDownloadWorld(worldURL, forceReload));

            (Exception ex, Context co) = await pui.RunProgressAsync();

            if (ex != null)
            {
                Debug.LogWarning($"Error in loading world {worldURL}");
                Debug.LogException(ex);
            }
            else
                Debug.Log($"Download and unpacking completed: {worldURL}");

            return (ex, co);
        }

        public async Task<Exception> VisitWorldAsync(string worldURL, bool forceReload = false)
        {
            // Offline world. Always a safe space.
            if(worldURL == null)
            {
                await MoveToOfflineWorld();
                return null;
            }

            (Exception ex, Context _) = await PreloadWorldDataAsync(worldURL, forceReload);

            // The server says it's the new world, jump in or ship out.
            if (ex != null)
                await MoveToOfflineWorld();
            else
                await MoveToOnlineWorld(worldURL);

            return ex;
        }

        /// <summary>
        /// Called from the client, either have the transition locally, or incite the
        /// server to do the transition.
        /// </summary>
        /// <param name="worldURL"></param>
        /// <param name="forceReload"></param>
        /// <returns>Task completed, or the server has been notified</returns>
        public async Task EnterWorldAsync(string worldURL, bool forceReload = false)
        {
            if(NetworkStatus.GetOnlineLevel() == OnlineLevel.Offline)
            {
                // In the offline mode, directly change the scene.
                await VisitWorldAsync(worldURL, forceReload);
            }
            else
            {
                // In the online mode, let the own avatar ask the server to initiate transition.
                XRControl.Me.MakeWorkdToChange(worldURL, forceReload);
            }
        }

        public void EnterDownloadedWorld(string worldABF)
        {
            SettingsManager.StartCoroutineAsync(() => EnterDownloadedWorldCoroutine(worldABF));
        }
    }
}