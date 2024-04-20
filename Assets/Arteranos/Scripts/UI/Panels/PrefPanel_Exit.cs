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
        public Toggle chk_DesiredVRMode = null;
        public Button btn_Exit = null;

        private Client cs = null;
        private bool dirty = false;

        protected override void Awake()
        {
            base.Awake();

            chk_DesiredVRMode.onValueChanged.AddListener(OnModeSwitchClick);
            btn_Exit.onClick.AddListener(OnExitClick);
        }

        protected override void Start()
        {
            base.Start();

            cs = SettingsManager.Client;

            chk_DesiredVRMode.isOn = cs.DesiredVRMode;

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


        private void OnModeSwitchClick(bool isOn)
        {
            cs.DesiredVRMode = isOn;
            dirty = true;
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
