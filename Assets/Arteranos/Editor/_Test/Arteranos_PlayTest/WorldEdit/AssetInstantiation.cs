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
    public class AssetHandling : WorldEditFixture
    {
        [UnityTest]
        public IEnumerator T001_Equality()
        {
            WOglTF gltf1 = new() { glTFCid = "gltfcid1" };
            WOglTF gltf2 = new() { glTFCid = "gltfcid2" };
            WOglTF gltf1_1 = new() { glTFCid = "gltfcid1" };

            WOKitItem kitItem1 = new() { kitCid = "kit1", kitName = "kitObject1" };
            WOKitItem kitItem2 = new() { kitCid = "kit1", kitName = "kitObject2" };
            WOKitItem kitItem3 = new() { kitCid = "kit2", kitName = "kitObject1" };
            WOKitItem kitItem1_1 = new() { kitCid = "kit1", kitName = "kitObject1" };

            WOPrimitive prim1 = new() { primitive = PrimitiveType.Sphere };
            WOPrimitive prim2 = new() { primitive = PrimitiveType.Cube };
            WOPrimitive prim1_1 = new() { primitive = PrimitiveType.Sphere };

            Assert.AreEqual(gltf1, gltf1);
            Assert.AreNotEqual(gltf1, gltf2);
            Assert.AreEqual(gltf1, gltf1_1);
            Assert.AreNotSame(gltf1, gltf1_1);

            Assert.AreEqual(kitItem1 , kitItem1);
            Assert.AreNotEqual(kitItem1, kitItem2);
            Assert.AreNotEqual(kitItem1, kitItem3);
            Assert.AreEqual(kitItem1, kitItem1_1);
            Assert.AreNotSame(kitItem1, kitItem1_1);

            Assert.AreEqual(prim1, prim1);
            Assert.AreNotEqual(prim1, prim2);
            Assert.AreEqual(prim1, prim1_1);
            Assert.AreNotSame(prim1, prim1_1);

            yield break;
        }

        [UnityTest]
        public IEnumerator T002_Dictionary()
        {
            WOglTF gltf1 = new() { glTFCid = "gltfcid1" };
            WOglTF gltf2 = new() { glTFCid = "gltfcid2" };
            WOglTF gltf1_1 = new() { glTFCid = "gltfcid1" };

            WOKitItem kitItem1 = new() { kitCid = "kit1", kitName = "kitObject1" };
            WOKitItem kitItem2 = new() { kitCid = "kit1", kitName = "kitObject2" };
            WOKitItem kitItem3 = new() { kitCid = "kit2", kitName = "kitObject1" };
            WOKitItem kitItem1_1 = new() { kitCid = "kit1", kitName = "kitObject1" };

            WOPrimitive prim1 = new() { primitive = PrimitiveType.Sphere };
            WOPrimitive prim2 = new() { primitive = PrimitiveType.Cube };
            WOPrimitive prim1_1 = new() { primitive = PrimitiveType.Sphere };

            Dictionary<WorldObjectAsset, string> dict = new()
            {
                { gltf1, "gltf_1" },
                { kitItem1, "kit_1" },
                { prim1, "prim1" }
            };

            Assert.AreEqual(3, dict.Count);
            Assert.True(dict.ContainsKey(gltf1));
            Assert.True(dict.ContainsKey(kitItem1));
            Assert.True(dict.ContainsKey(prim1));

            Assert.False(dict.ContainsKey(gltf2));
            Assert.False(dict.ContainsKey(prim2));
            Assert.False(dict.ContainsKey(kitItem2));
            Assert.False(dict.ContainsKey(kitItem3));

            Assert.True(dict.ContainsKey(gltf1_1));
            Assert.True(dict.ContainsKey(kitItem1_1));
            Assert.True(dict.ContainsKey(prim1_1));

            // Value equality, attempting to add an existing key
            Assert.Throws<ArgumentException>(() => dict.Add(gltf1_1, "gltf_1_1"));

            yield break;
        }
    }
}