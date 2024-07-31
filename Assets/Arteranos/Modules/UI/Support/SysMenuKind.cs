/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

namespace Arteranos.UI
{
    public class SysMenuKind : MonoBehaviour
    {
        public string Name;

        // Anything resembling the system menu hides the User HUD.
        private void Awake() => G.SysMenu.ShowUserHUD(false);

        // And if the last window is gone, we let the HUD reappear.
        private void OnDestroy()
        {
            SysMenuKind[] obs = FindObjectsOfType<SysMenuKind>();
            if(obs.Length == 0) G.SysMenu.ShowUserHUD(true);
        }
    }
}
