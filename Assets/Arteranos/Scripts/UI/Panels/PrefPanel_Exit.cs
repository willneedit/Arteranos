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
using System.Threading.Tasks;
using UnityEditor;

namespace Arteranos.UI
{
    public class PrefPanel_Exit : UIBehaviour
    {
        public LoginUI LoginUI = null;

        public Button btn_ModeSwitch = null;
        public Button btn_LoginUI = null;
        public Button btn_Exit = null;

        private ClientSettings cs = null;
        private bool dirty = false;

        protected override void Awake()
        {
            base.Awake();

            btn_ModeSwitch.onClick.AddListener(OnModeSwitchClick);
            btn_LoginUI.onClick.AddListener(() => { _ = OnLoginUIClick(); });
            btn_Exit.onClick.AddListener(() => { _ = OnExitClick(); });
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

        private void OnModeSwitchClick()
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

        private async Task OnLoginUIClick()
        {
            btn_LoginUI.interactable = false;
            LoginUI go = Instantiate(LoginUI);
            go.CancelEnabled = true;

            await go.PerformLoginAsync();

            btn_LoginUI.interactable = true;
        }

        private async Task OnExitClick()
        {
            btn_Exit.interactable = false;
            DialogUI go = DialogUI.New();
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
