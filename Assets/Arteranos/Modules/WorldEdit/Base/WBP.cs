/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.WorldEdit.Components;
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
            public GameObject LightInspector;
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

        public struct PrefabDefaults_
        {
            public List<WOCBase> components;
            public bool onGroundLevel;

            public PrefabDefaults_(List<WOCBase> components, bool onGroundLevel)
            {
                this.components = components;
                this.onGroundLevel = onGroundLevel;
            }
        }

        public struct ComponentUIData_
        {
            public string name;
            public GameObject inspector;

            public ComponentUIData_(string name, GameObject inspector)
            {
                this.name = name;
                this.inspector = inspector;
            }
        }

        public static readonly Dictionary<WOPrefabType, PrefabDefaults_> PrefabDefaults = new()
        {
            { WOPrefabType.SpawnPoint, new(
                new() { new WOCSpawnPoint() },
                true)
            },
            { WOPrefabType.TeleportTarget, new(
                new() { new WOCTeleportMarker() },
                true)
            },
            { WOPrefabType.Light, new(
                new() { new WOCLight() },
                false)
            }
        };

        public Dictionary<Type, ComponentUIData_> ComponentUIs
        {
            get
            {
                _componentUIs ??= new()
                    {
                        { typeof(WOCColor), new("Appearance", I.Inspectors.ColorInspector) },
                        { typeof(WOCLight), new("Light", I.Inspectors.LightInspector) },
                        { typeof(WOCPhysics), new("Physics", I.Inspectors.PhysicsInspector) },
                        { typeof(WOCRigidBody), new("Rigid Body", I.Inspectors.RigidBodyInspector) },
                        { typeof(WOCSpawner), new("Spawner", I.Inspectors.SpawnerInspector) },
                        { typeof(WOCSpawnPoint), new("Spawn Point", I.Inspectors.NullInspector) },
                        { typeof(WOCTeleportButton), new("Teleport Button", I.Inspectors.TeleportButtonInspector) },
                        { typeof(WOCTeleportMarker), new("Teleport Marker", I.Inspectors.NullInspector) },
                        { typeof(WOCTeleportSurface), new("Teleport target surface", I.Inspectors.NullInspector) },
                        { typeof(WOCTransform), new("Transform", I.Inspectors.TransformInspector) }
                    };
                return _componentUIs;
            }
        }

        public static Dictionary<string, WOCBase> OptionalComponents
        {
            get
            {
                if(_optionalComponents == null)
                {
                    WOCBase[] oclist =
                    {
                        new WOCLight(),
                        new WOCSpawnPoint(),
                        new WOCTeleportMarker(),
                        new WOCTeleportButton(),
                        new WOCTeleportSurface(),
                        new WOCSpawner(),
                        new WOCRigidBody()
                    };

                    _optionalComponents = new();
                    foreach (var o in oclist)
                        _optionalComponents.Add(I.ComponentUIs[o.GetType()].name, o);
                }
                return _optionalComponents;
            }
        }


        private Dictionary<Type, ComponentUIData_> _componentUIs = null;
        private Dictionary<WOPrefabType, PrefabEntry> _prefabs = null;
        private static Dictionary<string, WOCBase> _optionalComponents = null;

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