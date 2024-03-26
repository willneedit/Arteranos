/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Arteranos.Core.Operations;
using Arteranos.XR;
using Ipfs;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Arteranos.Web;

namespace Arteranos.Services
{
    public class TransitionProgress : TransitionProgressStatic
    {

        public GameObject[] ProgressBarObjects = null;
        public TMP_Text ProgressNotificationOb = null;

        public string ProgressNotification { 
            get => ProgressNotificationOb.text;
            private set => ProgressNotificationOb.text = value;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            foreach(var progress in ProgressBarObjects) progress.SetActive(false);
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        // async safe
        public override void OnProgressChanged(float progress, string progressText)
        {
            IEnumerator ProgessCoroutine(float progress, string progressText)
            {
                // Even if there are three bars on your smartphone,
                // there is a fourth state -- zero bars.
                int lit = (int)(progress * (ProgressBarObjects.Length + 1));
                for (int i = 0; i < ProgressBarObjects.Length; i++)
                    ProgressBarObjects[i].SetActive(i < lit);

                ProgressNotification = progressText;

                yield return null;
            }

            SettingsManager.StartCoroutineAsync(() => ProgessCoroutine(progress, progressText));
        }

        // NOTE: Needs preloaded world! Just deploys the sceneloader which it uses
        // the current preloaded world asset bundle!
        public static IEnumerator TransitionTo(Cid WorldCid, string WorldName)
        {
            ScreenFader.StartFading(1.0f);
            yield return new WaitForSeconds(0.5f);

            yield return MoveToPreloadedWorld(WorldCid, WorldName);

            ScreenFader.StartFading(0.0f);
        }

        private static IEnumerator MoveToPreloadedWorld(Cid WorldCid, string WorldName)
        {
            if (WorldCid == null)
            {
                AsyncOperation ao = SceneManager.LoadSceneAsync("OfflineScene");
                while (!ao.isDone) yield return null;

                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();

                XRControl.Instance.MoveRig();
            }
            else
                yield return EnterDownloadedWorld();

            SettingsManager.WorldCid = WorldCid;
            SettingsManager.WorldName = WorldName;
        }

        public static IEnumerator EnterDownloadedWorld()
        {
            string worldABF = WorldDownloader.CurrentWorldAssetBundlePath;

            Debug.Log($"Download complete, world={worldABF}");

            yield return null;

            bool done = false;

            // Deploy the scene loader.
            GameObject go = new("_SceneLoader");
            go.AddComponent<Persistence>();
            SceneLoader sl = go.AddComponent<SceneLoader>();
            sl.OnFinishingSceneChange += () =>
            {
                XRControl.Instance.MoveRig();
                done = true;
            };

            sl.Name = worldABF;

            yield return new WaitUntil(() => done);
        }
    }
}
