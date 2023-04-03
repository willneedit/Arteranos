/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Arteranos.Core;
using System.Threading.Tasks;
using UnityEditor;

namespace Arteranos.UI
{
    public class PrefPanel_Exit : UIBehaviour
    {
        public DialogUI DialogUI = null;
        public LoginUI LoginUI = null;

        public Button btn_ModeSwitch = null;
        public Button btn_LoginUI = null;
        public Button btn_Exit = null;

        private ClientSettings cs = null;
        private bool dirty = false;

        protected override void Awake()
        {
            base.Awake();

            btn_ModeSwitch.onClick.AddListener(onModeSwitchClick);
            btn_LoginUI.onClick.AddListener(() => { _ = onLoginUIClick(); });
            btn_Exit.onClick.AddListener(() => { _ = onExitClick(); });
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
            if(dirty) cs?.SaveSettings();
            dirty = false;
        }

        private void OnVRModeChanged(bool current)
        {
            btn_ModeSwitch.GetComponentInChildren<TextMeshProUGUI>().text =
                current ? "Switch to Desktop" : "Switch to VR";
        }

        private void onModeSwitchClick()
        {
            IEnumerator SwitchVRMode()
            {
                yield return null;
                cs.VRMode = !cs.VRMode;
                btn_ModeSwitch.interactable = true;
            }

            btn_ModeSwitch.interactable = false;
            StartCoroutine(SwitchVRMode());
        }

        private async Task onLoginUIClick()
        {
            gameObject.SetActive(false);
            LoginUI = Instantiate(LoginUI);

            await LoginUI.PerformLoginAsync();

            gameObject.SetActive(true);
        }

        private async Task onExitClick()
        {
            gameObject.SetActive(false);
            DialogUI go = Instantiate(DialogUI);
            int rc = await go.PerformDialogAsync(
                "Do you really want to quit?",
                new string[] {"OK", "Cancel"});

            if(rc == 0)
#if UNITY_EDITOR
                EditorApplication.ExitPlaymode();
#else
                Application.Quit();
#endif
            else
                gameObject.SetActive(true);
        }


    }
}
