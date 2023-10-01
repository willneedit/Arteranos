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

            GameObject original = Resources.Load<GameObject>("UI/UI_SysMenu");
            // NB: The resource is _cached_, the blueprint itself is modified and couldn't find the component
            // because it's already disabled the second time around!
            original.GetComponentInChildren<ChoiceBook>(true).gameObject.SetActive(false);
            GameObject sysmenu = Instantiate(original);
            ChoiceBook choiceBook = sysmenu.GetComponentInChildren<ChoiceBook>(true);

            choiceBook.gameObject.SetActive(true);

            // For now, restricting working on the remote server.
            // TODO #61: This would need a protocol for the remote server maintenance,
            // like changing the server port is like remotely configuring a firewall...
            if (NetworkStatus.GetOnlineLevel() == OnlineLevel.Client)
                RestrictRemoteServerConfig(choiceBook);

        }

        private static void RestrictRemoteServerConfig(ChoiceBook choiceBook)
        {
            int found = -1;
            for (int i = 0; i < choiceBook.ChoiceEntries.Length; i++)
                if (choiceBook.ChoiceEntries[i].name == "Moderation") found = i;

            choiceBook.SetPageActive(found, false);
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

        public static T FindGadget<T>(string name) where T : Component
            => FindGadget(name)?.GetComponent<T>();
    }
}
