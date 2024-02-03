using Arteranos.Services;
using Arteranos.UI;
using Arteranos.Web;
using Arteranos.XR;
using Ipfs;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
* Copyright (c) 2023, willneedit
* 
* Licensed by the Mozilla Public License 2.0,
* residing in the LICENSE.md file in the project's root directory.
*/

namespace Arteranos.Core.Operations
{
    public static class WorldTransition
    {
        private static IEnumerator EnterDownloadedWorldCoroutine(string worldABF)
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

        public static async Task MoveToOfflineWorld()
        {
            bool done = false;
            IEnumerator OfflineScene()
            {
                AsyncOperation ao = SceneManager.LoadSceneAsync("OfflineScene");
                while (!ao.isDone) yield return null;

                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();

                XRControl.Instance.MoveRig();

                SettingsManager.WorldInfoCid = null;
                ScreenFader.StartFading(0.0f);
                done = true;
            }
            
            SettingsManager.StartCoroutineAsync(OfflineScene);
            while(!done) await Task.Yield();
        }

        public static Task MoveToOnlineWorld(Cid WorldCid)
        {
            void Enter()
            {
                string worldABF = WorldDownloader.GetWorldABF(WorldCid);

                WorldInfo wi = WorldInfo.DBLookup(WorldCid);
                Cid WICid = wi.WorldInfoCid;

                EnterDownloadedWorld(worldABF);
                SettingsManager.WorldInfoCid = WICid;
            }

            return Task.Run(Enter);
        }

        public static async Task<(Exception, Context)> PreloadWorldDataAsync(Cid WorldCid)
        {
            IProgressUI pui = ProgressUIFactory.New();

            //pui.PatienceThreshold = 0f;
            //pui.AlmostFinishedThreshold = 0f;

            pui.AllowCancel = true;

            // FIXME See #71
            pui.SetupAsyncOperations(() => WorldDownloader.PrepareDownloadWorld(WorldCid));

            (Exception ex, Context co) = await pui.RunProgressAsync();

            if (ex != null)
            {
                Debug.LogWarning($"Error in loading world {WorldCid}");
                Debug.LogException(ex);
            }
            else
                Debug.Log($"Download and unpacking completed: {WorldCid}");

            return (ex, co);
        }

        public static async Task<Exception> VisitWorldAsync(Cid WorldCid)
        {
            // Offline world. Always a safe space.
            if(WorldCid == null)
            {
                await MoveToOfflineWorld();
                return null;
            }

            (Exception ex, Context _) = await PreloadWorldDataAsync(WorldCid);

            WorldInfo wi = WorldInfo.DBLookup(WorldCid);
            ServerPermissions wmd = wi?.ContentRating;

            if (wmd != null)
            {
                // Remotely connected user tries to sneak in something gross or raunchy?
                if (wmd.IsInViolation(SettingsManager.ActiveServerData.Permissions))
                {
                    Debug.Log("World is in violation of the server's content permission");
                    ex = new AccessViolationException("The world is in violation of the server's content permissions.");
                }
            }

            // The server says it's the new world, jump in or ship out.
            if (ex != null)
                await MoveToOfflineWorld();
            else
                await MoveToOnlineWorld(WorldCid);

            return ex;
        }

        /// <summary>
        /// Called from the client, either have the transition locally, or incite the
        /// server to do the transition.
        /// </summary>
        /// <param name="WorldCid"></param>
        /// 
        /// <returns>Task completed, or the server has been notified</returns>
        public static async Task EnterWorldAsync(Cid WorldCid)
        {
            ScreenFader.StartFading(1.0f);

            await Task.Delay(1000);

            if(NetworkStatus.GetOnlineLevel() == OnlineLevel.Offline)
            {
                // In the offline mode, directly change the scene.
                await VisitWorldAsync(WorldCid);
            }
            else
            {
                // In the online mode, let the own avatar ask the server to initiate transition.
                XRControl.Me.MakeWorkdToChange(WorldCid);
            }
        }

        public static void EnterDownloadedWorld(string worldABF)
        {
            SettingsManager.StartCoroutineAsync(() => EnterDownloadedWorldCoroutine(worldABF));
        }
    }
}