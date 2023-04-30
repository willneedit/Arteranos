/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Arteranos.Core;

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
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/UI_WorldList"));
            return go.GetComponent<WorldListUI>();
        }

        protected override void Awake()
        {
            base.Awake();

            btn_AddWorld.onClick.AddListener(OnAddWorldClicked);
        }

        protected override void Start()
        {
            base.Start();

            cs = SettingsManager.Client;
            ss = SettingsManager.Server;

            // Current one on top...
            if(!string.IsNullOrEmpty(ss.WorldURL))
                WorldListItem.New(lvc_WorldList.transform, ss.WorldURL);

            // ... and the rest.
            foreach(string url in cs.WorldList)
                if(url != ss.WorldURL) WorldListItem.New(lvc_WorldList.transform, url);
        }

        private void OnAddWorldClicked() => WorldListItem.New(lvc_WorldList.transform, txt_AddWorldURL.text);
    }
}
