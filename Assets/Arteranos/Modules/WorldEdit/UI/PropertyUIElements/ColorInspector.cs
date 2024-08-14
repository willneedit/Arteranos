/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Arteranos.WorldEdit
{
    public class ColorInspector : UIBehaviour, IInspector
    {
        public ColorPicker ColorPicker;

        public WOCBase Woc { get; set; }
        public PropertyPanel PropertyPanel { get; set; }

        protected override void Awake()
        {
            base.Awake();

            Debug.Assert(Woc != null);
            Debug.Assert(PropertyPanel);

            ColorPicker.OnColorChanged += GotColorChanged;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Populate();
        }

        public void Populate()
        {
            ColorPicker.SetColorWithoutNotify((Woc as WOCColor).color);
        }

        private void GotColorChanged(Color obj)
        {
            (Woc as WOCColor).SetState(obj);

            PropertyPanel.CommitModification(this);
        }
    }
}
