/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arteranos.WorldEdit
{
    [CreateAssetMenu(fileName = "WorldEditBlueprints", menuName = "Scriptable Objects/Application/World Edit Blueprint Settings")]
    public class WBP : ScriptableObject
    {
        public static WBP I;


        [Serializable]
        public struct Objects_
        {
            public GameObject TranslationGizmo;
            public GameObject RotationGizmo;
            public GameObject ScalingGizmo;
            public Material DefaultWEMaterial;
            public GameObject WorldObjectRoot;
        }

        [Serializable]
        public struct Inspectors_
        {
            public GameObject TransformInspector;
            public GameObject ColorInspector;
            public GameObject PhysicsInspector;
            public GameObject SpawnerInspector;
            public GameObject RigidBodyInspector;
            public GameObject TeleportButtonInspector;
            public GameObject NullInspector;
        }

        [Serializable]
        public struct PrefabEntry
        {
            public WOPrefabType Type;
            public string Description;
            public GameObject GameObject;
        }

        public Objects_ Objects;
        public Inspectors_ Inspectors;
        public PrefabEntry[] Prefabs;

        // ---------------------------------------------------------------

        private Dictionary<WOPrefabType, PrefabEntry> _prefabs = null;

        public (string description, GameObject blueprint) GetPrefab(WOPrefabType type)
        {
            if(_prefabs == null)
            {
                _prefabs = new();
                foreach (PrefabEntry entry in Prefabs) _prefabs[entry.Type] = entry;
            }

            if (!_prefabs.ContainsKey(type)) return (null, null);

            PrefabEntry e = _prefabs[type];
            return (e.Description, e.GameObject);
        }
    }
}