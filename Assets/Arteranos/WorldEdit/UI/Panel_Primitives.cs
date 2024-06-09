/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;
using Arteranos.UI;
using UnityEngine.Events;

namespace Arteranos.WorldEdit
{
    [Serializable]
    public struct Primitive
    {
        public PrimitiveType prim;
        public string name;
        public Texture2D texture;
    }

    public class Panel_Primitives : UIBehaviour
    {
        public Primitive[] primitives;

        public WorldObjectList wol { get; set; } = null;

        private ObjectChooser Chooser = null;
        
        protected override void Awake()
        {
            base.Awake();

            Chooser = transform.parent.GetComponentInChildren<ObjectChooser>(true);

            Chooser.OnShowingPage += PreparePage;
            Chooser.OnPopulateTile += PopulateTile;
            Chooser.OnAddingItem += RequestToAdd;
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

            image.texture = primitives[index].texture;
            text.text = primitives[index].name;

            button.onClick.AddListener(() => OnTileClicked(index));
        }

        private void RequestToAdd(string obj)
        {
            throw new NotSupportedException();
        }

        private void OnTileClicked(int index)
        {
            WOPrimitive newWOP = new()
            {
                primitive = primitives[index].prim
            };

            wol.OnAddingWorldObject(new WorldObject(newWOP, primitives[index].name));
        }

    }
}
