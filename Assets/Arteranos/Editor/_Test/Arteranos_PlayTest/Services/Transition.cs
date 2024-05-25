using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Arteranos.Services;
using System.IO;
using System.Threading.Tasks;
using Arteranos.Core;
using Ipfs;
using UnityEditor;
using Arteranos.Core.Operations;

namespace Arteranos.PlayTest.Services
{
    public class TransitionTest
    {
        private const string PlainFileAsset = "Assets/Arteranos/_Test/Sceelix_Abbey.zip";
        private string FileURLAsset => $"file:///{PlainFileAsset}";
        Cid WorldCid = null;


        [UnitySetUp]
        public IEnumerator SetupIPFS()
        {
            //Camera ca;
            //Light li;

            //ca = new GameObject("Camera").AddComponent<Camera>();
            //ca.transform.position = new(0, 1.75f, 0.2f);

            //li = new GameObject("Light").AddComponent<Light>();
            //li.transform.SetPositionAndRotation(new(0, 3, 0), Quaternion.Euler(50, -30, 0));
            //li.type = LightType.Directional;
            //li.color = Color.white;

            GameObject bp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arteranos/Prefabs/Core/_SceneEssentials.prefab");
            GameObject go = UnityEngine.Object.Instantiate(bp);

            yield return null;
        }

        private IEnumerator UploadTestWorld()
        {
            // Resynchronize with the background IPFS uploading processes
            yield return new WaitUntil(() => SettingsManager.DefaultFemaleAvatar != null);

            (AsyncOperationExecutor<Context> ao, Context co) =
                AssetUploader.PrepareUploadToIPFS(FileURLAsset, true);

            yield return ao.ExecuteCoroutine(co);

            WorldCid = AssetUploader.GetUploadedCid(co);

            Assert.IsNotNull(WorldCid);
        }


        [UnityTest]
        public IEnumerator InAndOut()
        {
            yield return new WaitForSeconds(10);

            yield return TransitionProgress.TransitionFrom();

            yield return new WaitForSeconds(5);

            yield return TransitionProgress.TransitionTo(null, null);

            yield return new WaitForSeconds(5);
        }

        [UnityTest]
        public IEnumerator ProgressMonitoring()
        {
            yield return new WaitForSeconds(10);

            yield return TransitionProgress.TransitionFrom();

            for(int i = 0; i < 10; i++)
            {
                float progress = (float)i / (float)10;
                TransitionProgress.Instance.OnProgressChanged(progress, $"Progress {i}");
                yield return new WaitForSeconds(2f);
            }

            yield return TransitionProgress.TransitionTo(null, null);

            yield return new WaitForSeconds(5);
        }

        [UnityTest]
        public IEnumerator ProgressMonitoringFromAsync()
        {
            yield return new WaitForSeconds(10);

            yield return TransitionProgress.TransitionFrom();

            // Same as before, but in a worker thread, not in a Coroutine
            Task t = Task.Run(async () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    float progress = (float)i / (float)10;
                    TransitionProgress.Instance.OnProgressChanged(progress, $"Progress {i}");

                    await Task.Delay(2000);
                }
            });

            // Wait for the task to be done.
            yield return new WaitUntil(() => t.IsCompleted);

            yield return TransitionProgress.TransitionTo(null, null);

            yield return new WaitForSeconds(5);
        }

        [UnityTest]
        public IEnumerator TransitionWorld()
        {
            yield return new WaitForSeconds(10);

            yield return UploadTestWorld();

            yield return TransitionProgress.TransitionFrom();

            (AsyncOperationExecutor<Context> ao, Context co) =
                WorldDownloader.PrepareGetWorldTemplate(WorldCid);

            ao.ProgressChanged += TransitionProgress.Instance.OnProgressChanged;

            yield return ao.ExecuteCoroutine(co, (ex, co) =>
            {
                TransitionProgress.Instance.OnProgressChanged(1.0f,
                    co != null ? "Success" : "Failed");
            });

            string file = WorldDownloader.GetWorldDataFile(co);

            Assert.IsNotNull(file);
            Assert.IsTrue(File.Exists(file));

            yield return new WaitForSeconds(2);

            yield return TransitionProgress.TransitionTo(WorldCid, "Would be WorldInfo.Name");

            yield return new WaitForSeconds(5);
        }
    }
}