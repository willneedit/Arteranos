/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using UnityEngine.InputSystem;

namespace Arteranos.UI
{

    public class SysMenu : MonoBehaviour
    {
        [SerializeField] private InputActionHandler SystemMenu;

        public const string GADGET_CAMERA_DRONE = "Camera Drone";

        public void Awake() => SystemMenu.PerformCallback = (InputAction.CallbackContext obj) => OpenSysMenu();

        public void OnEnable() => SystemMenu.BindAction();

        public void OnDisable() => SystemMenu.UnbindAction();

        public static void OpenSysMenu()
        {
            if(FindObjectOfType<SysMenuKind>() != null)
            {
                CloseSysMenus();
                return;
            }

            Instantiate(Resources.Load<GameObject>("UI/UI_SysMenu"));
        }

        public static void CloseSysMenus()
        {
            foreach(SysMenuKind menu in FindObjectsOfType<SysMenuKind>())
                Destroy(menu.gameObject);
        }

        public static void ShowUserHUD(bool show = true)
        {
            UserHUDUI hud = FindObjectOfType<UserHUDUI>(true);
            if(hud != null) hud.gameObject.SetActive(show);
        }

        public static void DismissGadget(string name = null)
        {
            foreach(GadgetKind gad in FindObjectsOfType<GadgetKind>())
                if(name == null || name == gad.Name) Destroy(gad.gameObject);
        }

        public static GameObject FindGadget(string name)
        {
            foreach(GadgetKind gad in FindObjectsOfType<GadgetKind>())
                if(name == gad.Name) return gad.gameObject;

            return null;
        }
    }
}
