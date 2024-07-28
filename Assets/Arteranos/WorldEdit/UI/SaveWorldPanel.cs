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
using Arteranos.Core;

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
        private UserID author;

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

            Client cs = G.Client;
            author = new(cs.UserAgrPublicKey, cs.Me.Nickname);

            lbl_Author.text = author;


            if (G.World.Cid == null)
                lbl_Template.text = "None";
            else
                lbl_Template.text = string.Format(templatePattern, 
                    G.World.Cid,
                    G.World.Name);

            ServerPermissions p = SettingsManager.ActiveServerData.Permissions;

            PresetPermission(chk_Violence, p.Violence);
            PresetPermission(chk_Nudity, p.Nudity);
            PresetPermission(chk_Suggestive, p.Suggestive);
            PresetPermission(chk_ExViolence, p.ExcessiveViolence);
            PresetPermission(chk_ExNudity, p.ExplicitNudes);
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

        private void PresetPermission(Toggle tg, bool? perm)
        {
            if (perm == null) // Server says, don't care
            {
                tg.interactable = true;
                tg.isOn = false;
            }
            else if (perm == false) // Server forbids the content
            {
                tg.interactable = false;
                tg.isOn = false;
            }
            else if (perm == true) // Server allows the content, maybe even likely in use
            {
                tg.interactable = true;
                tg.isOn = true;
            }
        }
    }
}
