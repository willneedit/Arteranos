/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Arteranos.Web;
using System.IO;
using UnityEngine.Networking;
using Arteranos.Core;
using Arteranos.Services;

namespace Arteranos.UI
{
    public class WorldListItem : ListItemBase
    {
        private HoverButton btn_Add = null;
        private HoverButton btn_Visit = null;
        private HoverButton btn_Delete = null;
        private HoverButton btn_ChangeWorld = null;

        public Image img_Screenshot = null;
        public TMP_Text lbl_Caption = null;

        private string worldURL = null;

        public static WorldListItem New(Transform parent, string url)
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/Components/WorldListItem"));
            go.transform.SetParent(parent, false);
            WorldListItem worldListItem = go.GetComponent<WorldListItem>();
            worldListItem.worldURL = url;
            return worldListItem;
        }

        protected override void Awake()
        {
            base.Awake();

            btn_Add = btns_ItemButton[0];
            btn_Visit= btns_ItemButton[1];
            btn_Delete= btns_ItemButton[2];
            btn_ChangeWorld = btns_ItemButton[3];

            btn_Add.onClick.AddListener(OnAddClicked);
            btn_Visit.onClick.AddListener(() => OnVisitClicked(false));
            btn_Delete.onClick.AddListener(OnDeleteClicked);
            btn_ChangeWorld.onClick.AddListener(() => OnVisitClicked(true));
        }

        protected override void Start()
        {
            base.Start();

            if(!string.IsNullOrEmpty(worldURL)) PopulateWorldData(worldURL);
        }

        private void PopulateWorldData(string worldURL)
        {
            string metadataFile;
            string screenshotFile;

            btn_Add.gameObject.SetActive(true);
            btn_Delete.gameObject.SetActive(true);

            // If we're in Host mode, you're the admin of your own server, so we're able to
            // change the world. And you still have the great responsibility...
            btn_Visit.gameObject.SetActive(NetworkStatus.GetOnlineLevel() != OnlineLevel.Host);
            btn_ChangeWorld.gameObject.SetActive(Utils.IsAbleTo(Social.UserCapabilities.CanInitiateWorldTransition, null));

            // It's stored in the persistent storage?
            (metadataFile, screenshotFile) = WorldGallery.RetrieveWorld(worldURL, false);

            if(metadataFile != null)
            {
                btn_Add.gameObject.SetActive(false);
                VisualizeWorldData(metadataFile, screenshotFile);
                return;
            }

            // Then, it has to be stored in the cache, right?
            (metadataFile, screenshotFile) = WorldGallery.RetrieveWorld(worldURL, true);

            if(metadataFile != null)
            {
                btn_Delete.gameObject.SetActive(false);
                VisualizeWorldData(metadataFile, screenshotFile);
                return;
            }

            // ... right...?
            lbl_Caption.text = $"({worldURL})";

            btn_Add.gameObject.SetActive(true);
            btn_Delete.gameObject.SetActive(true);
        }

        private void VisualizeWorldData(string metadataFile, string screenshotFile)
        {
            WorldMetaData wmd = WorldGallery.RetrieveWorldMetaData(worldURL);
            if(wmd?.ContentRating != null && wmd.ContentRating.IsInViolation(SettingsManager.ActiveServerData.Permissions))
            {
                btn_ChangeWorld.gameObject.SetActive(false);
            }

            IEnumerator GetTexture(string screenshotFile)
            {
                UnityWebRequest www = UnityWebRequestTexture.GetTexture($"file://{screenshotFile}");
                yield return www.SendWebRequest();

                if(www.result == UnityWebRequest.Result.Success)
                {
                    Utils.ShowImage(((DownloadHandlerTexture)www.downloadHandler).texture,
                        img_Screenshot);
                }
                else
                {
                    Debug.Log(www.error);
                }
            }

            string json = File.ReadAllText(metadataFile);
            WorldMetaData md = WorldMetaData.Deserialize(json);

            string lvstr = (md.Updated == DateTime.MinValue)
                ? "Never"
                : md.Updated.ToShortDateString();

            lbl_Caption.text = $"{md.WorldName}\nLast visited: {lvstr}";

            StartCoroutine(GetTexture(screenshotFile));
        }

        private void OnVisitClicked(bool inPlace)
        {
            if(!string.IsNullOrEmpty(worldURL))
            {
                if(inPlace)
                    WorldTransition.EnterWorldAsync(worldURL);
                else
                    ServerSearcher.InitiateServerTransition(worldURL);

                WorldMetaData md = WorldGallery.RetrieveWorldMetaData(worldURL);

                // Only if it's not an ad-hoc entry
                if (md != null)
                {
                    md.Updated = DateTime.Now;
                    WorldGallery.StoreWorldMetaData(worldURL, md);
                }
            }
        }

        private void OnAddClicked()
        {
            Client cs = SettingsManager.Client;

            // Transfer the metadata in our persistent storage.
            WorldGallery.StoreWorld(worldURL);

            // Then, put it down into our bookmark list.
            if(!cs.WorldList.Contains(worldURL))
            {
                cs.WorldList.Add(worldURL);
                cs.Save();
            }

            // And lastly, visualize the changed state.
            PopulateWorldData(worldURL);
        }

        private void OnDeleteClicked()
        {
            Client cs = SettingsManager.Client;

            // Remove the metadata from the persistent storage.
            WorldGallery.DeleteWorld(worldURL);

            // Then, strike it from our list
            if(cs.WorldList.Contains(worldURL))
            {
                cs.WorldList.Remove(worldURL);
                cs.Save();
            }

            // And, zip, gone.
            Destroy(gameObject);
        }
    }
}
