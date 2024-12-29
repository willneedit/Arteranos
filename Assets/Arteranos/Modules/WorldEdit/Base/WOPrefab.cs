/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arteranos.WorldEdit
{

    public enum WOPrefabType
    {
        Undefined = 0,
        SpawnPoint,
        TeleportTarget,
        Light
    }

    [ProtoContract]
    public class WOPrefab : WorldObjectAsset, IEquatable<WOPrefab>
    {
        [ProtoMember(1)]
        public WOPrefabType prefab;

        public override GameObject Create()
        {
            GameObject gobbo = GameObject.Instantiate(WBP.I.GetPrefab(prefab).blueprint);
            gobbo.SetActive(false);
            return gobbo;
        }

        public override IEnumerator CreateCoroutine(GameObject gobbo)
        {
            yield return null;
        }

        public override HashSet<AssetReference> GetAssetReferences()
        {
            throw new NotImplementedException();
        }

        // ---------------------------------------------------------------

        public override bool Equals(object obj)
        {
            return Equals(obj as WOPrefab);
        }

        public bool Equals(WOPrefab other)
        {
            return other is not null &&
                   prefab == other.prefab;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(prefab);
        }

        public static bool operator ==(WOPrefab left, WOPrefab right)
        {
            return EqualityComparer<WOPrefab>.Default.Equals(left, right);
        }

        public static bool operator !=(WOPrefab left, WOPrefab right)
        {
            return !(left == right);
        }
    }
}