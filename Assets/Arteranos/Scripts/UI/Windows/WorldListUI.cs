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

        [SerializeField] private RectTransform lvc_WorldList;
        [SerializeField] private TMP_InputField txt_AddWorldURL;
        [SerializeField] private Button btn_AddWorld;
        [SerializeField] private GameObject grp_TransitionMode;
        [SerializeField] private Spinner spn_TransitionMode;

        private Client cs = null;
        private Server ss = null;

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

            grp_TransitionMode.SetActive(Utils.IsAbleTo(Social.UserCapabilities.CanInitiateWorldTransition, null));

            // Current one on top...
            if(!string.IsNullOrEmpty(ss.WorldURL))
                WorldListItem.New(lvc_WorldList.transform, ss.WorldURL, this);

            // ... and the rest.
            foreach(string url in cs.WorldList)
                if(url != ss.WorldURL) WorldListItem.New(lvc_WorldList.transform, url, this);
        }

        // true if we incite a world transition in the specific server
        public bool InPlaceWorldTransition =>
            grp_TransitionMode.activeSelf && spn_TransitionMode.value != 0;

        private void OnAddWorldClicked() => WorldListItem.New(lvc_WorldList.transform, txt_AddWorldURL.text, this);
    }
}
