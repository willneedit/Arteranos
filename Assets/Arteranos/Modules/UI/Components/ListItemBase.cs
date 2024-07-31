/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Arteranos.UI
{
    public class ListItemBase : UIBehaviour
    {
        public HoverButton btn_Background = null;
        public GameObject go_Overlay = null;

        public HoverButton[] btns_ItemButton = new HoverButton[0];

        private bool ChildControlEntered = false;

        Coroutine delayco = null;

        protected override void Awake()
        {
            base.Awake();

            btn_Background.onHover += OnShowControls;
            btn_Background.interactable = false;

            foreach(HoverButton button in btns_ItemButton)
                button.onHover += OnShowChildControls;

            go_Overlay.SetActive(false);
        }

        private IEnumerator DelayShowOverlay(bool entered)
        {
            delayco = null;

            yield return new WaitForSeconds(0.5f);

            if(ChildControlEntered || entered)
                entered = true;

            go_Overlay.SetActive(entered);
        }

        protected void OnShowChildControls(bool entered) => ChildControlEntered = entered;

        protected void OnShowControls(bool entered)
        {
            if(ChildControlEntered) return;

            if(delayco != null) StopCoroutine(delayco);

            delayco = StartCoroutine(DelayShowOverlay(entered));
        }
    }
}
