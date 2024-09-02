using NUnit.Framework;
using System;
using Arteranos.Core;
using UnityEngine.TestTools;
using System.Threading.Tasks;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;

namespace Arteranos.PlayTest
{
    public class Lifetime
    {
        public static GameObject bp_lt = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Arteranos/Editor/_Test/Arteranos_PlayTest/Base/LifetimeGO.prefab");

        [UnityTest]
        public IEnumerator T001_BothEnabled()
        {
            bp_lt.SetActive(true);
            bp_lt.TryGetComponent(out LifetimeTestComponent bp_lic);
            bp_lic.enabled = true;

            Assert.IsFalse(bp_lic.CalledAwake);
            Assert.IsFalse(bp_lic.CalledOnEnable);
            Assert.IsFalse(bp_lic.CalledStart);

            GameObject go = GameObject.Instantiate(bp_lt);
            go.TryGetComponent(out LifetimeTestComponent lic);

            Assert.IsTrue(lic.CalledAwake);
            Assert.IsTrue(lic.CalledOnEnable);
            Assert.IsFalse(lic.CalledStart);

            yield return new WaitForEndOfFrame();

            Assert.IsTrue(lic.CalledStart);
        }

        [UnityTest]
        public IEnumerator T002_BPDisabled()
        {
            bp_lt.SetActive(false);
            bp_lt.TryGetComponent(out LifetimeTestComponent bp_lic);
            bp_lic.enabled = true;

            Assert.IsFalse(bp_lic.CalledAwake);
            Assert.IsFalse(bp_lic.CalledOnEnable);
            Assert.IsFalse(bp_lic.CalledStart);

            GameObject go = GameObject.Instantiate(bp_lt);
            go.TryGetComponent(out LifetimeTestComponent lic);

            Assert.IsFalse(lic.CalledAwake);
            Assert.IsFalse(lic.CalledOnEnable);
            Assert.IsFalse(lic.CalledStart);

            yield return new WaitForEndOfFrame();

            Assert.IsFalse(lic.CalledStart);
        }

        [UnityTest]
        public IEnumerator T003_CloneDisabled()
        {
            bp_lt.SetActive(true);
            bp_lt.TryGetComponent(out LifetimeTestComponent bp_lic);
            bp_lic.enabled = true;

            Assert.IsFalse(bp_lic.CalledAwake);
            Assert.IsFalse(bp_lic.CalledOnEnable);
            Assert.IsFalse(bp_lic.CalledStart);

            GameObject go = GameObject.Instantiate(bp_lt);
            go.SetActive(false);

            go.TryGetComponent(out LifetimeTestComponent lic);

            Assert.IsTrue(lic.CalledAwake);
            Assert.IsTrue(lic.CalledOnEnable);
            Assert.IsFalse(lic.CalledStart);

            yield return new WaitForEndOfFrame();

            Assert.IsFalse(lic.CalledStart);
        }

        [UnityTest]
        public IEnumerator T004_BPComponentDisabled()
        {
            bp_lt.SetActive(true);
            bp_lt.TryGetComponent(out LifetimeTestComponent bp_lic);
            bp_lic.enabled = false;

            Assert.IsFalse(bp_lic.CalledAwake);
            Assert.IsFalse(bp_lic.CalledOnEnable);
            Assert.IsFalse(bp_lic.CalledStart);

            GameObject go = GameObject.Instantiate(bp_lt);
            go.TryGetComponent(out LifetimeTestComponent lic);

            Assert.IsTrue(lic.CalledAwake);
            Assert.IsFalse(lic.CalledOnEnable);
            Assert.IsFalse(lic.CalledStart);

            yield return new WaitForEndOfFrame();

            Assert.IsFalse(lic.CalledStart);
        }

        [UnityTest]
        public IEnumerator T006_CloneComponentDisabled()
        {
            bp_lt.SetActive(true);
            bp_lt.TryGetComponent(out LifetimeTestComponent bp_lic);
            bp_lic.enabled = true;

            Assert.IsFalse(bp_lic.CalledAwake);
            Assert.IsFalse(bp_lic.CalledOnEnable);
            Assert.IsFalse(bp_lic.CalledStart);

            GameObject go = GameObject.Instantiate(bp_lt);
            go.TryGetComponent(out LifetimeTestComponent lic);
            lic.enabled = false;

            Assert.IsTrue(lic.CalledAwake);
            Assert.IsTrue(lic.CalledOnEnable);
            Assert.IsFalse(lic.CalledStart);

            yield return new WaitForEndOfFrame();

            Assert.IsFalse(lic.CalledStart);
        }


    }
}