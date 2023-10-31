/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Arteranos.Core;
using UnityEditor;

namespace Arteranos.UI
{
    public class PrefPanel_Exit : UIBehaviour
    {
        public Button btn_ModeSwitch = null;
        public Button btn_LoginUI = null;
        public Button btn_Exit = null;

        private Client cs = null;
        private bool dirty = false;

        protected override void Awake()
        {
            base.Awake();

            btn_ModeSwitch.onClick.AddListener(OnModeSwitchClick);
            btn_LoginUI.onClick.AddListener(OnLoginUIClick);
            btn_Exit.onClick.AddListener(OnExitClick);
        }

        protected override void Start()
        {
            base.Start();

            cs = SettingsManager.Client;

            cs.OnVRModeChanged += OnVRModeChanged;
            OnVRModeChanged(cs.VRMode);

            // Reset the state as it's the initial state, not the blank slate.
            dirty = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // Might be to disabled before it's really started, so cs may be null yet.
            if(dirty) cs?.Save();
            dirty = false;
        }

        private void OnVRModeChanged(bool current)
        {
            if (btn_ModeSwitch != null)
                btn_ModeSwitch.GetComponentInChildren<TextMeshProUGUI>().text = 
                    current ? "Switch to Desktop" : "Switch to VR";
        }

        private void OnModeSwitchClick()
        {
            IEnumerator SwitchVRMode()
            {
                yield return null;
                cs.VRMode = !cs.VRMode;
                btn_ModeSwitch.interactable = true;

                // Unconditionally save the desktop mode, the VR mode is another problem to
                // deal with.
                if(!cs.VRMode) cs.Save();
            }

            btn_ModeSwitch.interactable = false;
            StartCoroutine(SwitchVRMode());
        }

        private async void OnLoginUIClick()
        {
            btn_LoginUI.interactable = false;
            LoginUI go = LoginUI.New();
            go.CancelEnabled = true;

            await go.PerformLoginAsync();

            btn_LoginUI.interactable = true;
        }

        private async void OnExitClick()
        {
            btn_Exit.interactable = false;
            IDialogUI go = DialogUIFactory.New();
            int rc = await go.PerformDialogAsync(
                "Do you really want to quit?",
                new string[] {"OK", "Cancel"});

            if(rc == 0)
#if UNITY_EDITOR
                EditorApplication.ExitPlaymode();
#else
                UnityEngine.Application.Quit();
#endif
            else
                btn_Exit.interactable = true;
        }


    }
}
