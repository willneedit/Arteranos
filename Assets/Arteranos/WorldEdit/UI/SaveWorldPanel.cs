/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;

namespace Arteranos.WorldEdit
{
    public class SaveWorldPanel : UIBehaviour
    {
        public Button btn_ReturnToList;
        public TextMeshProUGUI lbl_Author;
        public TextMeshProUGUI lbl_Template;
        public TMP_InputField txt_WorldName;
        public TMP_InputField txt_WorldDescription;

        public Toggle chk_Violence;
        public Toggle chk_Nudity;
        public Toggle chk_Suggestive;
        public Toggle chk_ExViolence;
        public Toggle chk_ExNudity;

        public Button btn_SaveAsZip;
        public Button btn_SaveInGallery;

        public event Action OnReturnToList;

        private string templatePattern;

        protected override void Awake()
        {
            base.Awake();

            templatePattern = lbl_Template.text;

            btn_ReturnToList.onClick.AddListener(GotRTLClick);
            btn_SaveAsZip.onClick.AddListener(GotSaveAsZipClick);
            btn_SaveInGallery.onClick.AddListener(GotSaveInGalleryClick);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        private void GotSaveInGalleryClick()
        {
            throw new NotImplementedException();
        }

        private void GotSaveAsZipClick()
        {
            throw new NotImplementedException();
        }

        private void GotRTLClick()
        {
            OnReturnToList?.Invoke();
        }
    }
}
