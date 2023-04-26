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

namespace Arteranos.UI
{
    public class WorldListItem : ListItemBase
    {
        public string worldURL = null;
        public Image img_Screenshot = null;
        public TMP_Text lbl_Caption = null;

        public static WorldListItem New(Transform parent, string url)
        {
            GameObject go = Instantiate(Resources.Load("UI/WorldListItem") as GameObject);
            go.transform.SetParent(parent, false);
            WorldListItem worldListItem = go.GetComponent<WorldListItem>();
            worldListItem.worldURL = url;
            return worldListItem;
        }

        protected override void Awake()
        {
            base.Awake();

            btn_Add.onClick.AddListener(OnAddClicked);
            btn_Visit.onClick.AddListener(OnVisitClicked);
            btn_Delete.onClick.AddListener(OnDeleteClicked);

            go_Overlay.SetActive(false);
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
            IEnumerator GetTexture(string screenshotFile)
            {
                UnityWebRequest www = UnityWebRequestTexture.GetTexture($"file://{screenshotFile}");
                yield return www.SendWebRequest();

                if(www.result == UnityWebRequest.Result.Success)
                {
                    Texture2D screenshot = ((DownloadHandlerTexture) www.downloadHandler).texture;
                    img_Screenshot.sprite = Sprite.Create(screenshot,
                        new Rect(0, 0, screenshot.width, screenshot.height),
                        new Vector2(0, 0));
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

        private void OnVisitClicked()
        {
            if(!string.IsNullOrEmpty(worldURL)) WorldTransitionUI.InitiateTransition(worldURL);
        }

        private void OnAddClicked()
        {
            ClientSettings cs = SettingsManager.Client;

            // Transfer the metadata in our persistent storage.
            WorldGallery.StoreWorld(worldURL);

            // Then, put it down into our bookmark list.
            if(!cs.WorldList.Contains(worldURL))
            {
                cs.WorldList.Add(worldURL);
                cs.SaveSettings();
            }

            // And lastly, visualize the changed state.
            PopulateWorldData(worldURL);
        }

        private void OnDeleteClicked()
        {
            ClientSettings cs = SettingsManager.Client;

            // Remove the metadata from the persistent storage.
            WorldGallery.DeleteWorld(worldURL);

            // Then, strike it from our list
            if(cs.WorldList.Contains(worldURL))
            {
                cs.WorldList.Remove(worldURL);
                cs.SaveSettings();
            }

            // And, zip, gone.
            Destroy(gameObject);
        }
    }
}