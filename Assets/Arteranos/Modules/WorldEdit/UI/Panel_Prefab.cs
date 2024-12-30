/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using Arteranos.UI;
using System.Collections.Generic;
using Arteranos.WorldEdit.Components;

namespace Arteranos.WorldEdit
{
    public class Panel_Prefab : NewObjectPanel
    {
        struct PrefabDefaults
        {
            public List<WOCBase> components;
            public bool onGroundLevel;

            public PrefabDefaults(List<WOCBase> components, bool onGroundLevel)
            {
                this.components = components;
                this.onGroundLevel = onGroundLevel;
            }
        }

        [SerializeField] private ObjectChooser Chooser;

        private static readonly Dictionary<WOPrefabType, PrefabDefaults> _prefabDefaults = new()
        {
            { WOPrefabType.SpawnPoint, new(
                new List<WOCBase>() { new WOCSpawnPoint() }, 
                true) 
            },
            { WOPrefabType.TeleportTarget, new(
                new List<WOCBase>() { new WOCTeleportMarker() },
                true) 
            },
        };

        protected override void Awake()
        {
            base.Awake();

            Chooser.OnShowingPage += PreparePage;
            Chooser.OnPopulateTile += PopulateTile;
        }

        protected override void OnDestroy()
        {
            Chooser.OnShowingPage -= PreparePage;
            Chooser.OnPopulateTile -= PopulateTile;

            base.OnDestroy();
        }

        protected override void Start()
        {
            base.Start();

            Chooser.ShowPage(0);
        }

        private void PreparePage(int obj)
        {
            Chooser.UpdateItemCount(WBP.I.Prefabs.Length);
        }

        private void PopulateTile(int index, GameObject @object)
        {
            if (!@object.TryGetComponent(out ObjectChooserTile3D oc3d)) return;

            WOPrefabType type = WBP.I.Prefabs[index].Type;

            (string description, GameObject blueprint) = WBP.I.GetPrefab(type);
            oc3d.Label = description;
            oc3d.Object = Instantiate(blueprint);
            oc3d.ClickListener = () => OnTileClicked(type);
            oc3d.Scale = Vector3.one;
        }

        private void OnTileClicked(WOPrefabType type)
        {
            List<WOCBase> components = new();

            // If we want to set the object on the user's feet, negate the eye level offset
            if (_prefabDefaults[type].onGroundLevel)
            {
                WOCTransform woct = new() 
                { 
                    position = -(G.XRControl.heightAdjustment),
                    scale = Vector3.one,
                };
                components.Add(woct);
            }

            // And add the needed components
            components.AddRange(_prefabDefaults[type].components);

            base.BackOut(new WorldObjectInsertion()
            {
                asset = new WOPrefab() { prefab = type },
                name = WBP.I.GetPrefab(type).description,
                components = components
            });
        }
    }
}