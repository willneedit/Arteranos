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
using UnityEngine.Events;

namespace Arteranos.UI
{
    public class TextMessageUI : UIBehaviour
    {
        public TMP_InputField txt_Message = null;
        public Button btn_Preset_Sample = null;
        public Button btn_AddPreset = null;
        public Button btn_DelPreset = null;

        private ClientSettings cs = null;
        private bool dirty = false;

        private readonly List<string> presetStrings = new()
        {
            "Hi!",
            "Hello",
            "Bye",
            "You're muted",
            "You have noises"
        };

        protected override void Awake()
        {
            base.Awake();

            btn_Preset_Sample.gameObject.SetActive(false);

            btn_AddPreset.onClick.AddListener(OnAddPresetClicked);
            btn_DelPreset.onClick.AddListener(OnDelPresetClicked);

            txt_Message.onSubmit.AddListener(OnSubmit);
        }

        protected override void Start()
        {
            base.Start();

            cs = SettingsManager.Client;


            foreach(string preset in presetStrings)
                AddPresetButtonLL(preset);

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

        private UnityAction makePresetButtonClicked(string preset)
            => () => OnPresetButtonClicked(preset);

        private void AddPresetButtonLL(string preset)
        {
            Button newbtn = Instantiate(btn_Preset_Sample, btn_Preset_Sample.transform.parent);
            newbtn.onClick.AddListener(makePresetButtonClicked(preset));
            newbtn.GetComponentInChildren<TMP_Text>().text = preset;
            newbtn.gameObject.SetActive(true);
        }

        private void AddPresetButton(string preset)
        {
            int index = presetStrings.IndexOf(preset);
            if(index >= 0) return;

            presetStrings.Add(preset);
           
            AddPresetButtonLL(preset);
        }

        private void DelPresetButton(string preset)
        {
            int index = presetStrings.IndexOf(preset);
            if(index < 0) return;

            presetStrings.Remove(preset);

            Transform transform1 = btn_Preset_Sample.transform.parent;

            Destroy(transform1.GetChild(index + 1).gameObject);
        }

        private void OnPresetButtonClicked(string preset) => txt_Message.text = preset;

        private void OnAddPresetClicked() => AddPresetButton(txt_Message.text);

        private void OnDelPresetClicked() => DelPresetButton(txt_Message.text);

        private void OnSubmit(string arg0) => throw new NotImplementedException();

    }
}
