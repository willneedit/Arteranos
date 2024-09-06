/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Arteranos.WorldEdit;

using Arteranos.Core.Managed;
using AssetBundle = Arteranos.Core.Managed.AssetBundle;

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

        public static IEnumerator TransitionTo(World World)
        {
            IEnumerator MoveToPreloadedWorld()
            {
                G.World.World = World;

                if (World == null)
                {
                    AsyncOperation ao = SceneManager.LoadSceneAsync("OfflineScene");
                    while (!ao.isDone) yield return null;

                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();

                    G.XRControl.MoveRig();
                }
                else
                {
                    yield return EnterDownloadedWorld(World);
                    yield return World.WorldInfo.WaitFor();
                }
            }

            G.XRVisualConfigurator.StartFading(1.0f);
            yield return new WaitForSeconds(0.5f);

            yield return MoveToPreloadedWorld();

            G.XRVisualConfigurator.StartFading(0.0f);

            G.SysMenu.EnableHUD(true);
        }

        public static IEnumerator EnterDownloadedWorld(World world)
        {
            // Last chance...
            yield return world.TemplateContent.WaitFor();
            yield return world.DecorationContent.WaitFor();

            Debug.Log($"Download complete");

            // Once the world is placed in global, the old value will be discarded,
            // leading the AssetBundle will be unloaded.
            yield return G.SceneLoader.LoadScene((AssetBundle)world.TemplateContent, false);

            if (world.DecorationContent.Result != null)
            {
                Debug.Log("World Decoration detected, building hand-edited world");
                yield return G.WorldEditorData.BuildWorld(world.DecorationContent.Result);
            }
            else
                Debug.Log("World is a bare template");

            G.XRControl.MoveRig();
        }
    }
}
