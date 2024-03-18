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

                SettingsManager.WorldCid = null;
                SettingsManager.WorldName = null;
                ScreenFader.StartFading(0.0f);
                done = true;
            }
            
            SettingsManager.StartCoroutineAsync(OfflineScene);
            while(!done) await Task.Delay(8);
        }

        public static Task MoveToOnlineWorld(Cid WorldCid, string WorldName)
        {
            void Enter()
            {
                EnterDownloadedWorld();
                SettingsManager.WorldCid = WorldCid;
                SettingsManager.WorldName = WorldName;
            }

            return Task.Run(Enter);
        }

        public static void PreloadWorldDataAsync(Cid WorldCid, Action success, Action failure)
        {
            IProgressUI pui = ProgressUIFactory.New();

            //pui.PatienceThreshold = 0f;
            //pui.AlmostFinishedThreshold = 0f;

            pui.AllowCancel = true;

            // FIXME See #71
            pui.SetupAsyncOperations(() => WorldDownloader.PrepareGetWorldAsset(WorldCid));

            pui.SetupResultCallbacks(
                co =>
                {
                    Debug.Log($"Download of the world asset completed: {WorldCid}");
                    success?.Invoke();
                },
                (ex, co) =>
                {
                    Debug.LogWarning($"Error in loading world {WorldCid}");
                    Debug.LogException(ex);
                    failure?.Invoke();
                });
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
            WorldInfo wi = WorldInfo.DBLookup(WorldCid);

            await EnterWIAsync(wi);
        }

        public static async Task EnterWIAsync(WorldInfo wi)
        {
            ScreenFader.StartFading(1.0f);

            await Task.Delay(1000);

            // Pawn it off to the network message delivery service
            SettingsManager.EmitToServerCTSPacket(new CTSPWorldChangeAnnouncement()
            {
                WorldInfo = wi?.Strip(),
            });
        }

        public static void EnterDownloadedWorld()
        {
            static IEnumerator EnterDownloadedWorld_(string worldABF)
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

            SettingsManager.StartCoroutineAsync(() => EnterDownloadedWorld_(WorldDownloader.CurrentWorldAssetBundlePath));
        }
    }
}