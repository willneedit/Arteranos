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
using System;

namespace Arteranos.PlayTest.WorldEdit.WorldChange
{
    public class Insertion : WorldEditFixture
    {
        WorldObject wob = null;
        WorldObject sphere = null;

        public IEnumerator Default()
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
        public IEnumerator T001_FromScratch()
        {
            WOCTransform wot = new()
            {
                position = new Vector3(0, 2, 0),
                rotation = Quaternion.Euler(0, 0, 45),
                scale = Vector3.one
            };

            WOPrimitive wop = new() { primitive = PrimitiveType.Cube };

            WorldObjectInsertion woi = new()
            {
                path = null,
                asset = wop,
                name = "First",
                id = Guid.NewGuid(),
                components = new()
                {
                    wot
                }
            };

            yield return woi.Apply();

            yield return new WaitForSeconds(1);

            Assert.AreEqual(1, pl.transform.childCount);
            Assert.IsNotNull(pl.transform.GetChild(0).TryGetComponent(out WorldObjectComponent woc));
            Assert.AreEqual("First", pl.transform.GetChild(0).name);
            Assert.AreEqual(woi.id, woc.Id);
            Assert.AreEqual(1, woc.WOComponents.Count);
            Assert.IsNotNull(woc.TryGetWOC(out WOCTransform r_wot));
            Assert.AreSame(wot, r_wot);
        }

        [UnityTest]
        public IEnumerator T002_Insertion()
        {
            yield return Default();

            WOCTransform wot = new()
            {
                position = new Vector3(1, 1, 0),
                rotation = Quaternion.Euler(0, 0, 45),
                scale = Vector3.one
            };

            WOPrimitive wop = new() { primitive = PrimitiveType.Cube };

            WorldObjectInsertion woi = new()
            {
                path = new()
                {
                    wob.id,
                    sphere.id,
                },
                asset = wop,
                name = "First",
                id = Guid.NewGuid(),
                components = new()
                {
                    wot
                }
            };

            yield return woi.Apply();

            yield return new WaitForSeconds(1);

            // The default test construct's sphere already has the capsule as a child.
            Transform inserted = sphere.GameObject.transform;
            Assert.AreEqual(2, inserted.childCount);
            Assert.IsNotNull(inserted.GetChild(1).TryGetComponent(out WorldObjectComponent woc));
            Assert.AreEqual("First", inserted.GetChild(1).name);
            Assert.AreEqual(woi.id, woc.Id);
            Assert.AreEqual(1, woc.WOComponents.Count);
            Assert.IsNotNull(woc.TryGetWOC(out WOCTransform r_wot));
            Assert.AreSame(wot, r_wot);
        }

        [UnityTest]
        public IEnumerator T003_Nonexistent()
        {
            yield return Default();

            WOCTransform wot = new()
            {
                position = new Vector3(1, 1, 0),
                rotation = Quaternion.Euler(0, 0, 45),
                scale = Vector3.one
            };

            WOPrimitive wop = new() { primitive = PrimitiveType.Cube };

            WorldObjectInsertion woi = new()
            {
                path = new()
                {
                    Guid.NewGuid()
                },
                asset = wop,
                name = "First",
                id = Guid.NewGuid(),
                components = new()
                {
                    wot
                }
            };

            // How to deal with enumerators which could occasionally throw exceptions...
            IEnumerator enumerator = woi.Apply();
            while(true)
            {
                object ret = null;
                try
                {
                    if (!enumerator.MoveNext()) break;
                    ret = enumerator.Current;
                }
                catch (ArgumentException ex)
                {
                    // Catch, parse, ignore, continue or break. Your choice.
                    Debug.Log($"Expected exception: {ex.Message}");
                    // break;
                }
                // the yield statement is outside the try catch block
                yield return ret;
            }
        }

        [UnityTest]
        public IEnumerator T004_Copy()
        {
            yield return Default();

            WorldObjectInsertion woi = sphere.GameObject.MakeInsertion();
            Assert.AreEqual(1, woi.path.Count);
            Assert.AreEqual(wob.id, woi.path[0]);
            Assert.False(woi.collidable);
            Assert.AreNotEqual(sphere.id, woi.id);
            Assert.AreSame(sphere.asset, woi.asset);
            Assert.AreEqual(sphere.name, woi.name);
            Assert.AreEqual(2, woi.components.Count);

        }
        [UnityTest]
        public IEnumerator T005_Copy()
        {
            yield return Default();

            WorldObjectInsertion woi = wob.GameObject.MakeInsertion();
            Assert.AreEqual(0, woi.path.Count);
            Assert.True(woi.collidable);
            Assert.AreNotEqual(wob.id, woi.id);
            Assert.AreSame(wob.asset, woi.asset);
            Assert.AreEqual(wob.name, woi.name);
            Assert.AreEqual(2, woi.components.Count);

        }
    }
}