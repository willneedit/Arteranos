/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arteranos.UI
{

    public class SysMenu : MonoBehaviour
    {
        public InputActionHandler SystemMenu;

        public void Awake() => SystemMenu.PerformCallback = OnPerformSysMenu;

        public void OnEnable() => SystemMenu.BindAction();

        public void OnDisable() => SystemMenu.UnbindAction();

        private void OnPerformSysMenu(InputAction.CallbackContext obj) => OpenSysMenu();

        public static void OpenSysMenu()
        {
            if(FindObjectOfType<SysMenuKind>() != null)
            {
                SysMenu.CloseSysMenus();
                return;
            }

            Instantiate(Resources.Load<GameObject>("UI/UI_SysMenu"));
        }

        public static void CloseSysMenus()
        {
            foreach(SysMenuKind menu in FindObjectsOfType<SysMenuKind>())
                Destroy(menu.gameObject);
        }
    }
}
