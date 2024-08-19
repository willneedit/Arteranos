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

        public WOCBase Woc 
        { 
            get => physics; 
            set => physics = value as WOCPhysics; 
        }

        public PropertyPanel PropertyPanel { get; set; }

        private WOCPhysics physics;

        protected override void Awake()
        {
            base.Awake();

            Debug.Assert(Woc != null);
            Debug.Assert(PropertyPanel);

            chk_Collidable.onValueChanged.AddListener(on =>
            {
                physics.Collidable = on;

                ValidateInput();

                physics.SetState();
                PropertyPanel.CommitModification(this);
            });

            chk_Grabbable.onValueChanged.AddListener(on =>
            {
                physics.Grabbable = on;
                physics.SetState();
                PropertyPanel.CommitModification(this);
            });

            chk_ObeysGravity.onValueChanged.AddListener(on =>
            {
                physics.ObeysGravity = on;
                physics.SetState();
                PropertyPanel.CommitModification(this);
            });
        }

        private void ValidateInput()
        {
            // If it's not collidable, it would fall right through the floor.
            chk_ObeysGravity.interactable = physics.Collidable;
            if (!physics.Collidable)
            {
                chk_ObeysGravity.SetIsOnWithoutNotify(false);
                physics.ObeysGravity = false;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Populate();
        }

        public void Populate()
        {
            chk_Collidable.isOn = physics.Collidable;
            chk_Grabbable.isOn = physics.Grabbable;
            chk_ObeysGravity.isOn = physics.ObeysGravity;

            ValidateInput();
        }
    }
}
