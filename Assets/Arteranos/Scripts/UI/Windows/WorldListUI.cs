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
using static UnityEditor.Progress;

namespace Arteranos.UI
{
    public class WorldListUI : UIBehaviour
    {

        public RectTransform lvc_WorldList;
        public TMP_InputField txt_AddWorldURL;
        public Button btn_AddWorld;

        private ClientSettings cs = null;
        private ServerSettings ss = null;

        public static WorldListUI New()
        {
            GameObject go = Instantiate(Resources.Load("UI/UI_WorldList") as GameObject);
            return go.GetComponent<WorldListUI>();
        }

        private WorldListItem CreateWLItem()
        {
            GameObject go = Instantiate(Resources.Load("UI/WorldListItem") as GameObject);
            go.transform.SetParent(lvc_WorldList.transform, false);
            return go.GetComponent<WorldListItem>();
        }

        protected override void Awake()
        {
            base.Awake();

            btn_AddWorld.onClick.AddListener(OnAddWorldClicked);
        }

        protected override void Start()
        {
            cs = SettingsManager.Client;
            ss = SettingsManager.Server;

            base.Start();

            // Current one on top...
            if(!string.IsNullOrEmpty(ss.WorldURL))
            {
                WorldListItem item = CreateWLItem();
                item.worldURL = ss.WorldURL;
            }

            // ... and the rest.
            foreach(string url in cs.WorldList)
            {
                WorldListItem item = CreateWLItem();
                item.worldURL = url;
            }
        }

        private void OnAddWorldClicked()
        {
            WorldListItem item = CreateWLItem();
            item.worldURL = txt_AddWorldURL.text;
        }
    }
}
