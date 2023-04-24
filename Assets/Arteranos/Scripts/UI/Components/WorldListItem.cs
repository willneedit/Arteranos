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

using Arteranos.Web;
using System.IO;
using UnityEngine.Networking;
using Arteranos.Core;

namespace Arteranos.UI
{
    public class WorldListItem : UIBehaviour
    {
        public string worldURL = null;

        public HoverButton btn_Background = null;
        public Image img_Screenshot = null;
        public TMP_Text lbl_Caption = null;
        public GameObject go_Overlay = null;
        public HoverButton btn_Add = null;
        public HoverButton btn_Visit = null;
        public HoverButton btn_Delete = null;

        private bool ChildControlEntered = false;

        protected override void Awake()
        {
            base.Awake();

            btn_Background.onHover += OnShowControls;
            btn_Background.interactable = false;

            btn_Add.onHover += OnShowChildControls;
            btn_Visit.onHover += OnShowChildControls;
            btn_Delete.onHover += OnShowChildControls;

            btn_Visit.onClick.AddListener(OnVisitClicked);

            go_Overlay.SetActive(false);
        }

        private void OnShowChildControls(bool entered) => ChildControlEntered = entered;

        Coroutine delayco = null;

        private void OnShowControls(bool entered)
        {
            if(ChildControlEntered) return;

            if(delayco != null) StopCoroutine(delayco);

            delayco = StartCoroutine(DelayShowOverlay(entered));
        }

        private IEnumerator DelayShowOverlay(bool entered)
        {
            delayco = null;

            yield return new WaitForSeconds(1);

            if(ChildControlEntered || entered)
                entered = true;

            go_Overlay.SetActive(entered);
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
            throw new ArgumentException("World is neither in cache nor the persistent storage.");
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
                else Debug.Log(www.error);
            }

            string json = File.ReadAllText(metadataFile);
            WorldMetaData md = WorldMetaData.Deserialize(json);

            string lvstr = (md.Updated == DateTime.MinValue)
                ? "Never"
                : md.Updated.ToShortDateString();

            lbl_Caption.text = $"{md.WorldName}\nLast visited: {lvstr}";

            StartCoroutine(GetTexture(screenshotFile));
        }

        private void OnVisitClicked() => WorldTransitionUI.InitiateTransition(worldURL);
    }
}
