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
    public class Deletion : WorldEditFixture
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
        public IEnumerator T001_Deletion()
        {
            yield return Default();

            WOCTransform wot = new()
            {
                position = new Vector3(1, 1, 0),
                rotation = Quaternion.Euler(0, 0, 45),
                scale = Vector3.one
            };

            WOPrimitive wop = new() { primitive = PrimitiveType.Cube };

            WorldObjectDeletion wod = new()
            {
                path = new()
                {
                    wob.id,
                    sphere.id,
                },
            };

            // The parent of the to-be-deleted object (the sphere)
            Transform parent = wob.GameObject.transform;
            Assert.AreEqual(2, parent.childCount);

            yield return wod.Apply();

            yield return new WaitForSeconds(1);

            parent = wob.GameObject.transform;
            Assert.AreEqual(1, parent.childCount);
        }
    }
}