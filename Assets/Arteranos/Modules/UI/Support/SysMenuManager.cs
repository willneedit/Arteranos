/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using Arteranos.Services;
using System;
using Arteranos.Core;
using UnityEditorInternal;

namespace Arteranos.UI
{

    public class SysMenuManager : MonoBehaviour, ISysMenu
    {
        [SerializeField] private InputActionHandler SystemMenu;

        public const string GADGET_CAMERA_DRONE = "Camera Drone";


        public bool HUDEnabled { get; set; } = true;

        public void Awake()
        {
            SystemMenu.PerformCallback = (InputAction.CallbackContext obj) => OpenSysMenu(MenuKind.System);
            G.SysMenu = this;
        }

        public void OnEnable() => SystemMenu.BindAction();

        public void OnDisable() => SystemMenu.UnbindAction();

        public void OpenSysMenu(MenuKind kind)
        {
            if(ActionRegistry.HasStackedActions)
            {
                ActionRegistry.Back(null);
                return;
            }

            if(IsSysMenuOpen())
            {
                CloseSysMenus();
                return;
            }

            if (!HUDEnabled) return;

            switch(kind)
            {
                case MenuKind.System:
                    ActionRegistry.Call("sysMenu");
                    break;
                case MenuKind.WorldEdit:
                    Instantiate(BP.I.UI.WorldEditor);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public bool IsSysMenuOpen()
        {
            return FindObjectOfType<SysMenuKind>() != null;
        }

        public void CloseSysMenus()
        {
            foreach (SysMenuKind menu in FindObjectsOfType<SysMenuKind>(true))
                Destroy(menu.gameObject);
        }

        public void ShowUserHUD(bool show = true)
        {
            show = show && HUDEnabled;

            UserHUDUI hud = FindObjectOfType<UserHUDUI>(true);
            if(hud != null) hud.gameObject.SetActive(show);
        }

        public void DismissGadget(string name = null)
        {
            foreach(GadgetKind gad in FindObjectsOfType<GadgetKind>())
                if(name == null || name == gad.Name) Destroy(gad.gameObject);
        }

        public GameObject FindGadget(string name)
        {
            foreach(GadgetKind gad in FindObjectsOfType<GadgetKind>())
                if(name == gad.Name) return gad.gameObject;

            return null;
        }

        public T FindGadget<T>(string name) where T : Component
            => FindGadget(name)?.GetComponent<T>();

        public void EnableHUD(bool enable)
        {
            HUDEnabled = enable;
            DismissGadget();
            ShowUserHUD(false);

            if (!enable)
                CloseSysMenus();
            else
                ShowUserHUD();

        }

    }
}
