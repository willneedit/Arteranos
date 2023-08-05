/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace Arteranos.UI
{
    public class CameraDroneUI : UIBehaviour
    {
        [SerializeField] private Button btn_TakePhoto;
        [SerializeField] private Button btn_Dismiss;

        protected override void Awake()
        {
            base.Awake();

            btn_TakePhoto.onClick.AddListener(OnTakePhotoClicked);
            btn_Dismiss.onClick.AddListener(() => SysMenu.DismissGadget("Camera Drone"));
        }

        private void OnTakePhotoClicked() => throw new NotImplementedException();
    }
}
