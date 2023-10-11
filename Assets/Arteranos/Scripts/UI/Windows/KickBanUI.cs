/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Arteranos.Core;
using Arteranos.Avatar;
using Arteranos.XR;
using System.Collections.Generic;

namespace Arteranos.UI
{
    public class KickBanUI : UIBehaviour, IKickBanUI
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

        // TODO Maybe move the list to a more common part of the code.
        internal struct BanReasonEntry
        {
            public string description;
            public ulong reasonBit;
            public string reasonText;

            public BanReasonEntry(string description, ulong reasonBit, string reasonText)
            {
                this.description = description;
                this.reasonBit = reasonBit;
                this.reasonText = reasonText;
            }
        }

        internal static readonly BanReasonEntry[] reasons = new BanReasonEntry[]        
        {
            new("Pick a reason...",     0,                          "Please specify..."),
            new("Underage",             UserState.Underage,         "Below the legal or recommended age, e.g. 13 b/c COPPA"),
            new("Trolling",             UserState.Trolling,         "Disruptive behavior"),
            new("Hate Speech",          UserState.Hating,           "Discrimination/-phobic/hatemongering"),
            new("Loud/Disruptive",      UserState.Loud,             "Incessantly loud, even with repeated muting"),
            new("Bullying",             UserState.Bullying,         "Mobbing/Bullying/Harassment"),
            new("Sexual Harassment",    UserState.SxHarassment,     "Undesired sexual advances or harassment"),
            new("Exploit Use",          UserState.Exploiting,       "Exploit/security leak usage"),
            new("Impersonation",        UserState.Impersonation,    "Impersonation/Deliberately false representation"),
            new("Ban Evasion",          UserState.BanEvading,       "Attempted Ban evasion"),
            new("Other...",             0,                          "Other, please specify the detailed reason")
        };

        public static string FindBanReason(ulong userState)
        {
            if(!UserState.IsBanned(userState)) return null; // Not banned at all.

            IEnumerable<string> q = from entry in reasons 
                                    where entry.reasonBit != 0 && Bit64field.IsAll(userState, entry.reasonBit) 
                                    select entry.description;

            return q.Count() != 0 ? q.First() : null;
        }

        public IAvatarBrain Target { get; set; } = null;

        protected override void Awake()
        {
            base.Awake();

            spn_Reason.Options = (from entry in reasons select entry.description).ToArray();
            spn_Reason.OnChanged += OnReasonChange;
            

            chk_Ban_UID.onValueChanged.AddListener(OnBanMethodChange);
            chk_Ban_Address.onValueChanged.AddListener(OnBanMethodChange);
            chk_Ban_DID.onValueChanged.AddListener(OnBanMethodChange);

            btn_Commit.onClick.AddListener(OnCommitClicked);
            btn_Cancel.onClick.AddListener(OnCancelClicked);

            OnBanMethodChange(false);
        }

        protected override void Start()
        {
            base.Start();

            if(Target == null) throw new ArgumentNullException("target");

            // If you cannot ban the target, hide the portion of the UI
            if(!Utils.IsAbleTo(Social.UserCapabilities.CanBanUser, Target))
            {
                lbl_BanHow.gameObject.SetActive(false);
                grp_BanHow.SetActive(false);
            }

            lbl_Reason.text = string.Format(lbl_Reason.text, Target.Nickname);
        }

        private void OnReasonChange(int arg1, bool arg2)
        {
            txt_RD_Hint.text = reasons[arg1].reasonText;
            OnBanMethodChange(false);
        }

        private void OnBanMethodChange(bool arg0)
        {
            btn_Commit_txt.text = IsBanning()
                ? "Ban"
                : "Kick";
        }

        private bool IsBanning()
        {
            return (chk_Ban_DID.isOn || chk_Ban_UID.isOn || chk_Ban_Address.isOn) &&
                spn_Reason.value != 0;
        }

        private void OnCommitClicked()
        {
            ulong banbits = reasons[spn_Reason.value].reasonBit |
                (IsBanning() ? UserState.Banned : 0);

            // Fill out the pattern of the targeted user action and transmit it
            // to the server to fill the fields and do the final decision.

            KickPacket kickPacket = new()
            {
                UserID = Target.UserID,
                State = new()
                {
                    userID = chk_Ban_UID.isOn ? Target.UserID : null,
                    userState = banbits,
                    address = chk_Ban_Address.isOn ? string.Empty : null,
                    deviceUID = chk_Ban_DID.isOn ? string.Empty : null,
                    remarks = txt_ReasonDetail.text,
                }
            };

            ServerConfig.KickUser(kickPacket);
            Destroy(gameObject);
        }

        private void OnCancelClicked() => Destroy(gameObject);
    }
}