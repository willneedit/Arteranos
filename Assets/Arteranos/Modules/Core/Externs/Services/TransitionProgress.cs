/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Ipfs;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Arteranos.WorldEdit;

using AsyncOperation = UnityEngine.AsyncOperation;
using Arteranos.Core.Operations;
using Arteranos.Core.Managed;

namespace Arteranos.Services
{
    public static class TransitionProgress
    {
        public static IEnumerator TransitionFrom()
        {
            G.XRVisualConfigurator.StartFading(1.0f);
            yield return new WaitForSeconds(0.5f);

            AsyncOperation ao = SceneManager.LoadSceneAsync("Transition");
            yield return new WaitUntil(() => ao.isDone);


            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            G.XRControl.MoveRig();

            G.XRVisualConfigurator.StartFading(0.0f);

            yield return new WaitUntil(() => (G.TransitionProgress != null));

            G.SysMenu.EnableHUD(false);
        }

        // NOTE: Needs preloaded world! Just deploys the sceneloader which it uses
        // the current preloaded world asset bundle!
        public static IEnumerator TransitionTo(World World)
        {
            IEnumerator MoveToPreloadedWorld()
            {
                if (World == null)
                {
                    AsyncOperation ao = SceneManager.LoadSceneAsync("OfflineScene");
                    while (!ao.isDone) yield return null;

                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();

                    G.XRControl.MoveRig();

                    G.World.Name = "Somewhere";
                }
                else
                {
                    yield return EnterDownloadedWorld();
                    yield return World.WorldInfo.WaitFor();

                    G.World.Name = ((WorldInfoNetwork)World.WorldInfo).WorldName;
                }

                G.World.Cid = World?.RootCid;
            }

            G.XRVisualConfigurator.StartFading(1.0f);
            yield return new WaitForSeconds(0.5f);

            yield return MoveToPreloadedWorld();

            G.XRVisualConfigurator.StartFading(0.0f);

            G.SysMenu.EnableHUD(true);
        }

        public static IEnumerator EnterDownloadedWorld()
        {

            string worldABF = WorldDownloader.GetWorldDataFile(WorldDownloader.CurrentWorldContext);
            IWorldDecoration worldDecoration = WorldDownloader.GetWorldDecoration(WorldDownloader.CurrentWorldContext);

            Debug.Log($"Download complete, world={worldABF}");

            yield return G.SceneLoader.LoadScene(worldABF);

            if (worldDecoration != null)
            {
                Debug.Log("World Decoration detected, building hand-edited world");
                yield return G.WorldEditorData.BuildWorld(worldDecoration);
            }
            else
                Debug.Log("World is a bare template");

            G.XRControl.MoveRig();
        }
    }
}
