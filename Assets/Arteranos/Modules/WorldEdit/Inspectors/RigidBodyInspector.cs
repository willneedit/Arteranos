/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.UI;
using Arteranos.WorldEdit.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arteranos.WorldEdit
{
    public class RigidBodyInspector : UIBehaviour, IInspector
    {
        public TMP_InputField txt_Mass;
        public TMP_InputField txt_Drag;
        public TMP_InputField txt_AngularDrag;
        public Toggle chk_Gravity;
        public Toggle chk_Grabbable;

        public WOCBase Woc
        {
            get => body;
            set => body = value as WOCRigidBody;
        }

        public PropertyPanel PropertyPanel { get; set; }

        private WOCRigidBody body;

        private void ModifyFloadField(TMP_InputField field, ref float target)
        {
            try
            {
                target = float.Parse(field.text);

                body.SetState();
                PropertyPanel.CommitModification(this);
            }
            catch { }
        }

        private void ModifyToggleField(Toggle field, ref bool target)
        {
            target = field.isOn;

            body.SetState();
            PropertyPanel.CommitModification(this);
        }

        protected override void Awake()
        {
            base.Awake();

            txt_Mass.onValueChanged.AddListener(_ => ModifyFloadField(txt_Mass, ref body.Mass));
            txt_Drag.onValueChanged.AddListener(_ => ModifyFloadField(txt_Drag, ref body.Drag));
            txt_AngularDrag.onValueChanged.AddListener(_ => ModifyFloadField(txt_AngularDrag, ref body.AngularDrag));

            chk_Gravity.onValueChanged.AddListener(_ => ModifyToggleField(chk_Gravity, ref body.ObeysGravity));
            chk_Grabbable.onValueChanged.AddListener(_ => ModifyToggleField(chk_Grabbable, ref body.Grabbable));
        }

        public void Populate()
        {
            txt_Mass.text = body.Mass.ToString("F4");
            txt_Drag.text = body.Drag.ToString("F4");
            txt_AngularDrag.text = body.AngularDrag.ToString("F4");

            chk_Gravity.isOn = body.ObeysGravity;
            chk_Grabbable.isOn = body.Grabbable;
        }
    }
}