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
    public class SpawnerInspector : UIBehaviour, IInspector
    {
        public NumberedSlider sld_MaxItems;
        public TMP_InputField txt_Force_X;
        public TMP_InputField txt_Force_Y;
        public TMP_InputField txt_Force_Z;
        public NumberedSlider sld_Lifetime;

        public WOCBase Woc 
        { 
            get => spawner; 
            set => spawner = value as WOCSpawner; 
        }

        public PropertyPanel PropertyPanel { get; set; }

        private WOCSpawner spawner;

        protected override void Awake()
        {
            base.Awake();

            Debug.Assert(Woc != null);
            Debug.Assert(PropertyPanel);

            sld_MaxItems.OnValueChanged += val =>
            {
                spawner.MaxItems = (int) val;

                spawner.SetState();
                PropertyPanel.CommitModification(this);
            };

            sld_Lifetime.OnValueChanged += val =>
            {
                spawner.Lifetime = val;

                spawner.SetState();
                PropertyPanel.CommitModification(this);
            };

            txt_Force_X.onValueChanged.AddListener(GotForceChanged);
            txt_Force_Y.onValueChanged.AddListener(GotForceChanged);
            txt_Force_Z.onValueChanged.AddListener(GotForceChanged);
        }

        private void GotForceChanged(string arg0)
        {
            try
            {
                spawner.Force = (WOVector3)new Vector3(
                    float.Parse(txt_Force_X.text),
                    float.Parse(txt_Force_Y.text),
                    float.Parse(txt_Force_Z.text));

                spawner.SetState();
                PropertyPanel.CommitModification(this);
            }
            catch { }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Populate();
        }

        public void Populate()
        {
            sld_MaxItems.value = spawner.MaxItems;
            txt_Force_X.text = spawner.Force.x.ToString("F1");
            txt_Force_X.text = spawner.Force.y.ToString("F1");
            txt_Force_X.text = spawner.Force.z.ToString("F1");
            sld_Lifetime.value = spawner.Lifetime;
        }
    }
}
