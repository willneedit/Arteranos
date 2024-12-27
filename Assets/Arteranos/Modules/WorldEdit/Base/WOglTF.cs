/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core.Managed;
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Arteranos.WorldEdit
{
    [ProtoContract]
    public class WOglTF : WorldObjectAsset, IEquatable<WOglTF>
    {
        [ProtoMember(1)]
        public string glTFCid;  // 1. Single glTF file

        public override HashSet<AssetReference> GetAssetReferences() => new() { new("glTF", glTFCid) };

        public override GameObject Create()
        {
            GameObject gobbo = new GameObject("Unleaded glTF world object"); // :)
            gobbo.SetActive(false);

            return gobbo;
        }

        public override IEnumerator CreateCoroutine(GameObject LoadedObject)
        {
            using CancellationTokenSource cts = new(60000);
            using IPFSGLTFObject obj = new(glTFCid, cts.Token)
            {
                RootObject = LoadedObject,
                InitActive = false
            };

            yield return obj.GameObject.WaitFor();

            // Add a box collider with with the approximated bounds.
            Bounds? b = obj.Bounds;
            if (b.HasValue)
            {
                BoxCollider bc = LoadedObject.AddComponent<BoxCollider>();
                bc.center = b.Value.center;
                bc.size = b.Value.size;
            }

            LoadedObject.name = $"glTF {glTFCid}";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as WOglTF);
        }

        public bool Equals(WOglTF other)
        {
            return other is not null &&
                   glTFCid == other.glTFCid;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(glTFCid);
        }

        public static bool operator ==(WOglTF left, WOglTF right)
        {
            return EqualityComparer<WOglTF>.Default.Equals(left, right);
        }

        public static bool operator !=(WOglTF left, WOglTF right)
        {
            return !(left == right);
        }
    }
}
