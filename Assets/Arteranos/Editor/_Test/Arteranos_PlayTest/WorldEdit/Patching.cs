/*
 *Copyright(c) 2024, willneedit
 *
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;


using Arteranos.WorldEdit;
using System.IO;
using System;
using System.Collections.Generic;

namespace Arteranos.PlayTest.WorldEdit
{
    public class Patching : WorldEditFixture
    {
        WorldObject wob = null;
        WorldObject sphere = null;

        [UnitySetUp]
        public IEnumerator SetUp1()
        {
            yield return null;

            wob = BuildSample();

            // First, build and instantiate the sample.
            GameObject result = null;
            yield return wob.Instantiate(pl.transform, _go => result = _go);

            // Pick the sphere...
            sphere = wob.children[0];
            Assert.IsNotNull(sphere);
            Assert.IsNotNull(sphere.GameObject);
        }

        [UnityTest]
        public IEnumerator T001_PatchWithWorldObject()
        {
            // First and second components are the transform or the color, respectively.
            WOCTransform t = sphere.GetWComponent<WOCTransform>();
            WOCColor c = sphere.GetWComponent<WOCColor>();
            Assert.IsNotNull(t);
            Assert.IsNotNull(c);

            t.rotation = Quaternion.Euler(0, 0, 45);
            c.color = Color.red;

            yield return new WaitForSeconds(1);

            sphere.Patch();

            yield return new WaitForSeconds(1);

            Assert.AreEqual(2, sphere.components.Count);
        }

        [UnityTest]
        public IEnumerator T002_CreateSnapshot()
        {
            yield return null;

            // Not really a patch, rather the complete snapshot
            WorldObjectPatch wop = sphere.GameObject.MakePatch(true);
            Assert.AreEqual(2, wop.path.Count);
            Assert.AreEqual(wob.id, wop.path[0]);
            Assert.AreEqual(sphere.id, wop.path[1]);

            Assert.AreEqual(2, wop.components.Count);
            Assert.AreEqual(sphere.components[0], wop.components[0]);
            Assert.AreEqual(sphere.components[1], wop.components[1]);
        }

        [UnityTest]
        public IEnumerator T003_ApplyFromScratch() 
        {
            yield return null;

            WOCTransform t = new()
            {
                position = new Vector3(0, 2, 0),
                rotation = Quaternion.Euler(0, 0, 45),
                scale = Vector3.one
            };

            WorldObjectPatch wop = new()
            {
                path = new()
                {
                    wob.id,
                    sphere.id,
                },
                components = new() 
                { 
                    t
                }
            };

            yield return wop.Apply();

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            sphere.GameObject.TryGetComponent(out WorldObjectComponent component);
            Assert.IsNotNull(component);

            component.TryGetWOC(out WOCTransform woct);
            Assert.IsNotNull(woct);

            // Further than 'Equal', because the patched-in component will be taken into
            // effect as-is.
            Assert.AreSame(t, woct);
        }

        [UnityTest]
        public IEnumerator T004_IncrementalPatches()
        {
            yield return null;

            List<Guid> path = new()
            {
                wob.id,
                sphere.id,
            };

            WOCTransform nineoclock = new()
            {
                position = new Vector3(0, 2, 0),
                rotation = Quaternion.Euler(0, 0, 90),
                scale = Vector3.one
            };

            WOCTransform midday = new()
            {
                position = new Vector3(0, 2, 0),
                rotation = Quaternion.Euler(0, 0, 0),
                scale = Vector3.one
            };

            WOCColor red = new()
            {
                color = Color.red,
            };

            WOCColor green = new()
            {
                color = Color.green,
            };

            WorldObjectPatch turningnine = new()
            { path = path, components = new() { nineoclock } };

            WorldObjectPatch goingtowork = new()
            { path = path, components = new() { red } };

            WorldObjectPatch turningmidday = new()
            { path = path, components = new() { midday } };

            WorldObjectPatch goinghome = new()
            { path = path, components = new() { green } };

            turningnine.Apply();
            yield return new WaitForSeconds(1);
            goingtowork.Apply();
            yield return new WaitForSeconds(1);
            turningmidday.Apply();
            yield return new WaitForSeconds(1);
            goinghome.Apply();
            yield return new WaitForSeconds(1);
        }

        [UnityTest]
        public IEnumerator T005_CreateDiff()
        {
            yield return null;

            // No diff.
            WorldObjectPatch wop = sphere.GameObject.MakePatch();
            Assert.AreEqual(0, wop.components.Count);

            // Change just the transform.
            sphere.GameObject.transform.localScale = Vector3.one * 0.5f;

            // IMPORTANT: Change detection needs one successful Update() cycle
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            wop = sphere.GameObject.MakePatch();
            Assert.AreEqual(1, wop.components.Count);
            Assert.AreEqual(sphere.components[0], wop.components[0]);

            // Revert the change
            sphere.GameObject.transform.localScale = Vector3.one;

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            wop = sphere.GameObject.MakePatch();
            Assert.AreEqual(0, wop.components.Count);

            // Change the color
            sphere.GameObject.TryGetComponent(out Renderer renderer);
            renderer.material.color = Color.red;

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            wop = sphere.GameObject.MakePatch();
            Assert.AreEqual(1, wop.components.Count);
            Assert.AreEqual(sphere.components[1], wop.components[0]);
        }
    }
}