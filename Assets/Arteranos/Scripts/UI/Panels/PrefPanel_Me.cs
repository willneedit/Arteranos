using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Arteranos.Core;
using System.Linq;

namespace Arteranos.UI
{
    public class PrefPanel_Me : UIBehaviour
    {
        public Spinner spn_OnlineStatus = null;
        public TMP_InputField txt_Nickname = null;
        public TMP_Text tro_UserID = null;
        public TMP_InputField txt_AvatarURL = null;
        public TMP_Text tro_AvatarProvider = null;
        public Button btn_AvatarGallery = null;

        private readonly Dictionary<string, Visibility> statusNames = new();
        private readonly Dictionary<string, AvatarProvider> APNames = new();

        private ClientSettings cs = null;
        private bool dirty = false;

        protected override void Awake()
        {
            base.Awake();

            foreach(Visibility v in Enum.GetValues(typeof(Visibility)))
            {
                string name = Utils.GetEnumDescription(v);
                if(name != null)
                    statusNames.Add(name, v);
            }

            spn_OnlineStatus.Options = statusNames.Keys.ToArray();

            foreach(AvatarProvider ap in Enum.GetValues(typeof(AvatarProvider)))
            {
                string name = Utils.GetEnumDescription(ap);
                if(name != null)
                    APNames.Add(name, ap);
            }

            spn_OnlineStatus.OnChanged += OnVisibilityChanged;
            txt_Nickname.onValueChanged.AddListener(OnNicknameChanged);
            txt_AvatarURL.onValueChanged.AddListener(OnAvatarURLChanged);

            btn_AvatarGallery.GetComponentInChildren<TextMeshProUGUI>().text = "-->";

        }

        protected override void Start()
        {
            base.Start();

            cs = SettingsManager.Client;

            spn_OnlineStatus.value = Array.IndexOf(spn_OnlineStatus.Options, Utils.GetEnumDescription(cs.Visibility));
            txt_Nickname.text = cs.Nickname;
            tro_UserID.text = cs.UserID;
            txt_AvatarURL.text = cs.AvatarURL;
            tro_AvatarProvider.text = Utils.GetEnumDescription(cs.AvatarProvider);

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


        private void OnAvatarURLChanged(string current)
        {
            cs.AvatarURL = current;
            dirty = true;
        }

        private void OnNicknameChanged(string current)
        {
            cs.Nickname = current;
            dirty = true;
        }

        private void OnVisibilityChanged(int old, bool current)
        {
            string vname = spn_OnlineStatus.Options[spn_OnlineStatus.value];

            if(statusNames.TryGetValue(vname, out Visibility v))
                cs.Visibility = v;

            dirty = true;
        }
    }
}
