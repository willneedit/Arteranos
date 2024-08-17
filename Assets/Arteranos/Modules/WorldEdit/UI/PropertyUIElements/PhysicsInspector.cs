/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arteranos.WorldEdit
{
    public class PhysicsInspector : UIBehaviour, IInspector
    {
        public Toggle chk_Collidable;
        public Toggle chk_Grabbable;
        public Toggle chk_ObeysGravity;
        public NumberedSlider sld_ResetDuration;

        public WOCBase Woc { get; set; }
        public PropertyPanel PropertyPanel { get; set; }

        protected override void Awake()
        {
            base.Awake();

            Debug.Assert(Woc != null);
            Debug.Assert(PropertyPanel);

            // ColorPicker.OnColorChanged += GotColorChanged;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Populate();
        }

        public void Populate()
        {
            // ColorPicker.SetColorWithoutNotify((Woc as WOCColor).color);
        }

        //private void GotColorChanged(Color obj)
        //{
        //    (Woc as WOCColor).SetState(obj);

        //    PropertyPanel.CommitModification(this);
        //}
    }
}
