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
        public HoverButton btn_Add = null;

        public HoverButton btn_Background = null;
        public HoverButton btn_Delete = null;
        public HoverButton btn_Visit = null;
        public GameObject go_Overlay = null;

        private bool ChildControlEntered = false;

        Coroutine delayco = null;

        protected override void Awake()
        {
            base.Awake();

            btn_Background.onHover += OnShowControls;
            btn_Background.interactable = false;

            btn_Add.onHover += OnShowChildControls;
            btn_Visit.onHover += OnShowChildControls;
            btn_Delete.onHover += OnShowChildControls;
        }

        private IEnumerator DelayShowOverlay(bool entered)
        {
            delayco = null;

            yield return new WaitForSeconds(1);

            if(ChildControlEntered || entered)
                entered = true;

            go_Overlay.SetActive(entered);
        }

        private void OnShowChildControls(bool entered) => ChildControlEntered = entered;

        private void OnShowControls(bool entered)
        {
            if(ChildControlEntered) return;

            if(delayco != null) StopCoroutine(delayco);

            delayco = StartCoroutine(DelayShowOverlay(entered));
        }
    }
}
