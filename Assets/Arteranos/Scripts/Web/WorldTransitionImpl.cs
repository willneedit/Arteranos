using Arteranos.Core;
using Arteranos.Services;
using Arteranos.UI;
using Arteranos.XR;
using Ipfs;
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
    public class WorldTransitionImpl : WorldTransition
    {
        private void Awake() => Instance = this;
        private void OnDestroy() => Instance = null;

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

        protected override async Task<(Exception, WorldData)> GetWorldDataAsync_(Cid WorldCid)
        {
            static (Exception, WorldData) _GetWorldData() => (null, new WorldData());

            return await Task.Run(_GetWorldData);
        }

        protected override bool IsWorldPreloaded_(Cid WorldCid)
            => WorldInfo.DBLookup(WorldCid) != null;

        protected override async Task MoveToOfflineWorld_()
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

        protected override Task MoveToOnlineWorld_(Cid WorldCid)
        {
            void Enter_()
            {
                string worldABF = WorldDownloader.GetWorldABF(WorldCid);

                WorldInfo wi = WorldInfo.DBLookup(WorldCid);
                Cid WICid = wi.WorldInfoCid;

                EnterDownloadedWorld_(worldABF);
                SettingsManager.WorldInfoCid = WICid;
            }

            return Task.Run(Enter_);
        }

        protected override async Task<(Exception, Context)> PreloadWorldDataAsync_(Cid WorldCid)
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

        protected override async Task<Exception> VisitWorldAsync_(Cid WorldCid)
        {
            // Offline world. Always a safe space.
            if(WorldCid == null)
            {
                await MoveToOfflineWorld_();
                return null;
            }

            (Exception ex, Context _) = await PreloadWorldDataAsync_(WorldCid);

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
                await MoveToOfflineWorld_();
            else
                await MoveToOnlineWorld_(WorldCid);

            return ex;
        }

        /// <summary>
        /// Called from the client, either have the transition locally, or incite the
        /// server to do the transition.
        /// </summary>
        /// <param name="WorldCid"></param>
        /// 
        /// <returns>Task completed, or the server has been notified</returns>
        protected override async Task EnterWorldAsync_(Cid WorldCid)
        {
            ScreenFader.StartFading(1.0f);

            await Task.Delay(1000);

            if(NetworkStatus.GetOnlineLevel() == OnlineLevel.Offline)
            {
                // In the offline mode, directly change the scene.
                await VisitWorldAsync_(WorldCid);
            }
            else
            {
                // In the online mode, let the own avatar ask the server to initiate transition.
                XRControl.Me.MakeWorkdToChange(WorldCid);
            }
        }

        protected override void EnterDownloadedWorld_(string worldABF)
        {
            SettingsManager.StartCoroutineAsync(() => EnterDownloadedWorldCoroutine(worldABF));
        }
    }
}