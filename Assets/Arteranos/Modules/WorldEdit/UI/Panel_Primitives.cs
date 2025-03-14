/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Arteranos.UI;
using Arteranos.WorldEdit.Components;

namespace Arteranos.WorldEdit
{
    public enum PrimitiveTypeEx
    {
        Sphere,     // Equals to PrimitiveType
        Capsule,
        Cylinder,
        Cube,
        Plane,
        Quad,
        _SimpleEnd, // Lynchpin
        Cone,       // Additional primitive shapes
        Prism,
        Pyramid,
        Wedge
    }

    [Serializable]
    public struct Primitive
    {
        public PrimitiveTypeEx prim;
        public string name;
        public Texture2D preview;
    }

    public class Panel_Primitives : NewObjectPanel
    {
        public Primitive[] primitives;

        public ObjectChooser Chooser;
        
        protected override void Awake()
        {
            base.Awake();

            Chooser.OnShowingPage += PreparePage;
            Chooser.OnPopulateTile += PopulateTile;
            Chooser.OnAddingItem += RequestToAdd;
        }

        protected override void OnDestroy()
        {
            Chooser.OnShowingPage -= PreparePage;
            Chooser.OnPopulateTile -= PopulateTile;
            Chooser.OnAddingItem -= RequestToAdd;

            base.OnDestroy();
        }

        protected override void Start()
        {
            base.Start();

            Chooser.ShowPage(0);
        }

        private void PreparePage(int obj)
        {
            Chooser.UpdateItemCount(primitives.Length);
        }

        private void PopulateTile(int index, GameObject @object)
        {
            IPFSImage image = @object.GetComponentInChildren<IPFSImage>();
            TMP_Text text = @object.GetComponentInChildren<TMP_Text>();
            Button button = @object.GetComponentInChildren<Button>();

            image.texture = primitives[index].preview;
            text.text = primitives[index].name;

            button.onClick.AddListener(() => OnTileClicked(index));
        }

        private void RequestToAdd(string obj)
        {
            throw new NotSupportedException();
        }

        private void OnTileClicked(int index)
        {
            AddingNewObject(new() 
            {
                asset = new WOPrimitive() { primitive = primitives[index].prim },
                name = primitives[index].name,
                components = new() { new WOCColor() { color = Color.gray } }
            });
        }
    }
}
