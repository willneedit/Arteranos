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
    public class AgreementDialogUI : UIBehaviour
    {
        [SerializeField] private TMP_Text lbl_LicenseText = null;
        [SerializeField] private Button btn_Disagree = null;
        [SerializeField] private Button btn_Agree = null;
        [SerializeField] private Scrollbar scrl_Vertical = null;

        private Action OnDisagree = null;
        private Action OnAgree = null;
        private string rtText = null;

        public static AgreementDialogUI New(string text, Action disagree, Action agree)
        {
            string rtText = LoadLicenseText(text);

            if(rtText == null)
            {
                // Known text, just skip it.
                agree?.Invoke();
                return null;
            }

            GameObject go = Instantiate(Resources.Load<GameObject>("UI/UI_AgreementDialogUI"));
            AgreementDialogUI AgreementDialogUI = go.GetComponent<AgreementDialogUI>();
            AgreementDialogUI.OnDisagree += disagree;
            AgreementDialogUI.OnAgree += agree;
            AgreementDialogUI.rtText = rtText;
            return AgreementDialogUI;
        }

        private static string LoadLicenseText(string text)
        {
            string hashstr = HashText(MD2RichText(text));

            Client client = SettingsManager.Client;

            // Could be already known.
            return client.KnownAgreements.Contains(hashstr) ? null : MD2RichText(text);
        }

        private static string HashText(string text)
        {
            // This is serious enough to warrant a cryptographically strong hash algorithm.
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            using IncrementalHash myHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            myHash.AppendData(bytes);
            return Convert.ToBase64String(myHash.GetHashAndReset());
        }

        private static string MD2RichText(string text)
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

            lbl_LicenseText.text = rtText;

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
                string hashstr = HashText(rtText);

                Client client = SettingsManager.Client;

                client.KnownAgreements.Add(hashstr);
                client.Save();

                OnAgree?.Invoke();
            }
            else
                OnDisagree?.Invoke();

            Destroy(gameObject);
        }
    }
}
