using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Ipfs.Engine;
using Arteranos.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Arteranos.Core;
using Ipfs;
using System.Linq;
using System.Threading;
using System.Text;
using Ipfs.Core.Cryptography.Proto;
using Ipfs.Engine.Cryptography;
using Arteranos.Core.Cryptography;
using System.Net;
using UnityEditor;

namespace Arteranos.PlayTest.Services
{
    public class TransitionTest
    {
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

            // Resynchronize with the background IPFS uploading processes
            yield return new WaitForSeconds(5);
            yield return new WaitUntil(() => SettingsManager.DefaultFemaleAvatar != null);
        }

        [UnityTest]
        public IEnumerator InAndOut()
        {
            yield return TransitionProgressReceiver.TransitionFrom();

            yield return new WaitForSeconds(5);

            yield return TransitionProgressReceiver.TransitionTo(null, null);

            yield return new WaitForSeconds(5);
        }

        [UnityTest]
        public IEnumerator ProgressMonitoring()
        {
            yield return TransitionProgressReceiver.TransitionFrom();

            for(int i = 0; i < 10; i++)
            {
                float progress = (float)i / (float)10;
                TransitionProgressReceiver.Instance.OnProgressChanged(progress, $"Progress {i}");
                yield return new WaitForSeconds(2f);
            }

            yield return TransitionProgressReceiver.TransitionTo(null, null);

            yield return new WaitForSeconds(5);
        }
    }
}