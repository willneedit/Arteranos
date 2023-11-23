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
using Arteranos.Web;

namespace Arteranos.UI
{
    public class WorldListUI : UIBehaviour
    {

        [SerializeField] private RectTransform lvc_WorldList;
        [SerializeField] private TMP_InputField txt_AddWorldURL;
        [SerializeField] private Button btn_AddWorld;

        private Client cs = null;

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

            // grp_TransitionMode.SetActive(Utils.IsAbleTo(Social.UserCapabilities.CanInitiateWorldTransition, null));

            // Current one on top...
            if(!string.IsNullOrEmpty(SettingsManager.CurrentWorld))
                WorldListItem.New(lvc_WorldList.transform, SettingsManager.CurrentWorld, this);

            // ... and the rest.
            foreach(string url in cs.WorldList)
                if(url != SettingsManager.CurrentWorld)
                {
                    WorldMetaData wmd = WorldGallery.RetrieveWorldMetaData(url);

                    // Filter out the worlds which go against to _your_ preferences.
                    if (wmd?.ContentRating == null || !wmd.ContentRating.IsInViolation(SettingsManager.Client.ContentFilterPreferences))
                    {
                        WorldListItem.New(lvc_WorldList.transform, url, this);
                    }
                }
        }

        private void OnAddWorldClicked() => WorldListItem.New(lvc_WorldList.transform, txt_AddWorldURL.text, this);
    }
}
