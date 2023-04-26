/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arteranos
{

    public class PopupSysMenu : MonoBehaviour
    {
        public InputActionHandler SystemMenu;

        public void Awake() => SystemMenu.PerformCallback = OnPerformSysMenu;

        public void OnEnable() => SystemMenu.BindAction();

        public void OnDisable() => SystemMenu.UnbindAction();

        private void OnPerformSysMenu(InputAction.CallbackContext obj)
        {
            if(FindObjectOfType<SysMenuKind>() != null)
            {
                SysMenuKind.CloseSystemMenus();
                return;
            }

            Instantiate(Resources.Load("UI/UI_SysMenu") as GameObject);
        }
    }
}
