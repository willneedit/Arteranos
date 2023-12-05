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

using Arteranos.Core;
using System.Text;
using System.Security.Cryptography;

namespace Arteranos.UI
{
    public class AgreementDialogUI : UIBehaviour, IAgreementDialogUI
    {
        [SerializeField] private TMP_Text lbl_LicenseText = null;
        [SerializeField] private Button btn_Disagree = null;
        [SerializeField] private Button btn_Agree = null;
        [SerializeField] private Scrollbar scrl_Vertical = null;

        public Action OnDisagree { get; set; } = null;
        public Action OnAgree { get; set; } = null;
        public ServerInfo ServerInfo { get; set; }

        public string MD2RichText(string text)
        {
            bool monospaced = false;

            List<string> newLines = new();

            foreach(string line in text.Split('\n'))
            {
                string new_line = line;
                if(line.Length > 2 && line[0..3] == "## ")
                {
                    newLines.Add($"<b>{line[3..]}</b>");
                }
                else if(line.Length > 2 && line[0..3] == "```")
                {
                    monospaced = !monospaced;
                    // Seems to be unsupported.
                    // newLines.Add(monospaced ? "<mspace>" : "</mspace>");
                }
                else
                {
                    newLines.Add(line);
                }
            }

            return string.Join("\n", newLines);
        }

        protected override void Start()
        {
            base.Start();

            lbl_LicenseText.text = MD2RichText(ServerInfo.PrivacyTOSNotice);

            btn_Disagree.onClick.AddListener(() => OnReaction(false));
            btn_Agree.onClick.AddListener(() => OnReaction(true));

            scrl_Vertical.onValueChanged.AddListener(OnScrollbarMoved);

            btn_Agree.interactable = false;
        }

        private void OnScrollbarMoved(float arg0)
        {
            // Enable the Agree button if you moved the text down.
            btn_Agree.interactable = (arg0 > 0.9f);
        }

        private void OnReaction(bool agree)
        {
            if (agree)
            {
                Client.UpdateServerPass(ServerInfo, true, null);
                OnAgree?.Invoke();
            }
            else
                OnDisagree?.Invoke();

            Destroy(gameObject);
        }

    }
}
