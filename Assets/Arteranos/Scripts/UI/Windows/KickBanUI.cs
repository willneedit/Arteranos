/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Arteranos.Core;
using UnityEngine.Events;

using System.Reflection;
using Arteranos.Avatar;
using Arteranos.XR;

namespace Arteranos.UI
{
    public class KickBanUI : UIBehaviour
    {
        [SerializeField] private TMP_Text lbl_Reason = null;
        [SerializeField] private Spinner spn_Reason = null;
        [SerializeField] private TMP_InputField txt_ReasonDetail = null;
        [SerializeField] private TMP_Text txt_RD_Hint = null;
        [SerializeField] private TMP_Text lbl_BanHow = null;
        [SerializeField] private GameObject grp_BanHow = null;
        [SerializeField] private Toggle chk_Ban_UID = null;
        [SerializeField] private Toggle chk_Ban_Address = null;
        [SerializeField] private Toggle chk_Ban_DID = null;
        [SerializeField] private TMP_Text lbl_Ban_UID = null;
        [SerializeField] private TMP_Text lbl_Ban_Address = null;
        [SerializeField] private TMP_Text lbl_Ban_DID = null;
        [SerializeField] private Button btn_Cancel = null;
        [SerializeField] private Button btn_Commit = null;
        [SerializeField] private TMP_Text btn_Commit_txt = null;

        internal struct BanReasonEntry
        {
            public string description;
            public ulong reasonBit;
            public string reasonText;
        }

        internal readonly BanReasonEntry[] reasons = new BanReasonEntry[]
        {
            new()
            {
                description = "Pick a reason...",
                reasonBit = 0,
                reasonText = "Please specify..."
            },
            new()
            {
                description = "Underage",
                reasonBit = UserState.Underage,
                reasonText = "Below the legal or recommended age, e.g. 13 b/c COPPA"
            },
            new()
            {
                description = "Trolling",
                reasonBit = UserState.Trolling,
                reasonText = "Disruptive behavior"
            },
            new()
            {
                description = "Hate Speech",
                reasonBit = UserState.Hating,
                reasonText = "Discrimination/-phobic/hatemongering"
            },
            new()
            {
                description = "Loud/Disruptive",
                reasonBit = UserState.Loud,
                reasonText = "Incessantly loud, even with repeated muting"
            },
            new()
            {
                description = "Bullying",
                reasonBit = UserState.Bullying,
                reasonText = "Mobbing/Bullying/Harassment"
            },
            new()
            {
                description = "Sexual Harassment",
                reasonBit = UserState.SxHarassment,
                reasonText = "Undesired sexual advances or harassment"
            },
            new()
            {
                description = "Exploit Use",
                reasonBit = UserState.Exploiting,
                reasonText = "Exploit/security leak usage"
            },
            new()
            {
                description = "Impersonation",
                reasonBit = UserState.Impersonation,
                reasonText = "Impersonation/Deliberately false representation"
            },
            new()
            {
                description = "Ban Evasion",
                reasonBit = UserState.BanEvading,
                reasonText = "Attempted Ban evasion"
            },
            new()
            {
                description = "Other...",
                reasonBit = 0,
                reasonText = "Other, please specify the detailed reason"
            }
        };

        public IAvatarBrain target = null;

        protected override void Awake()
        {
            base.Awake();

            spn_Reason.Options = (from entry in reasons select entry.description).ToArray();
            spn_Reason.OnChanged += (int arg1, bool arg2) => txt_RD_Hint.text = reasons[arg1].reasonText;

            chk_Ban_UID.onValueChanged.AddListener(OnBanMethodChange);
            chk_Ban_Address.onValueChanged.AddListener(OnBanMethodChange);
            chk_Ban_DID.onValueChanged.AddListener(OnBanMethodChange);

            btn_Commit.onClick.AddListener(OnCommitClicked);
        }

        private void OnCommitClicked()
        {
            bool banning = chk_Ban_DID.isOn || chk_Ban_UID.isOn || chk_Ban_Address.isOn;
            ulong banbits = reasons[spn_Reason.value].reasonBit |
                (banning ? UserState.Banned : 0);

            // Fill out the pattern of the targeted user action and transmit it
            // to the server to fill the fields and do the final decision.
            ServerUserState banPacket = new()
            {
                userID = chk_Ban_UID.isOn ? target.UserID : null,
                userState = banbits,
                address = chk_Ban_Address.isOn ? string.Empty : null,
                deviceUID = chk_Ban_DID.isOn ? string.Empty : null,
                remarks = txt_ReasonDetail.text,
            };

            // XRControl.Me.AttemptKickUser(target, banPacket);
            throw new NotImplementedException();
        }

        protected override void Start()
        {
            base.Start();

            if(target == null) throw new ArgumentNullException("target");
        }
        private void OnBanMethodChange(bool arg0)
        {
            btn_Commit_txt.text = chk_Ban_DID.isOn || chk_Ban_UID.isOn || chk_Ban_Address.isOn 
                ? "Ban" 
                : "Kick";
        }
    }
}