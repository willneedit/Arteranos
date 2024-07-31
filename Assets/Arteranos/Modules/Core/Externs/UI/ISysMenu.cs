/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

namespace Arteranos.UI
{
    public enum MenuKind
    {
        System,
        WorldEdit
    }

    public interface ISysMenu
    {
        bool HUDEnabled { get; set; }

        void CloseSysMenus();
        void DismissGadget(string name = null);
        GameObject FindGadget(string name);
        T FindGadget<T>(string name) where T : Component;
        bool IsSysMenuOpen();
        void OpenSysMenu(MenuKind kind);
        void ShowUserHUD(bool show = true);
        void EnableHUD(bool enable);
    }
}