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
#pragma warning disable IDE0044 // Modifizierer "readonly" hinzufügen
        [SerializeField] private ObjectChooser Chooser;
#pragma warning restore IDE0044 // Modifizierer "readonly" hinzufügen

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
            WBP.PrefabDefaults_ pd = WBP.PrefabDefaults[type];
            if (pd.onGroundLevel || pd.rotateDown)
            {
                Transform ct = Camera.main.transform;

                WOCTransform woct = new() 
                { 
                    // below your feet or in front of your eyes
                    position = pd.onGroundLevel 
                    ? -(G.XRControl.heightAdjustment) 
                    : ct.rotation * Vector3.forward * 2.5f,

                    // dipping below your sightline or straight away from you
                    rotation = pd.rotateDown 
                    ? new Vector3(45, 0, 0) 
                    : Vector3.zero,

                    scale = Vector3.one,
                };
                components.Add(woct);
            }

            // And add the needed components
            foreach (WOCBase _entry in WBP.PrefabDefaults[type].components)
            {
                WOCBase entry = _entry.Clone() as WOCBase;
                entry.Reset();
                components.Add(entry);
            }

            base.BackOut(new WorldObjectInsertion()
            {
                asset = new WOPrefab() { prefab = type },
                name = WBP.I.GetPrefab(type).description,
                components = components
            });
        }
    }
}