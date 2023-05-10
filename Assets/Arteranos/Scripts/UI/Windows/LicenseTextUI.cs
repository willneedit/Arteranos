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

namespace Arteranos.UI
{
    public class LicenseTextUI : UIBehaviour
    {
        public TMP_Text lbl_LicenseText = null;

        private const string LICENSEASSETNAME = "Third Party Notices";
        public static LicenseTextUI New()
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/UI_LicenseText"));
            return go.GetComponent<LicenseTextUI>();
        }

        private string LoadLicenseText()
        {
            TextAsset ta = Resources.Load<TextAsset>(LICENSEASSETNAME);

            if(ta == null)
                throw new Exception("3rd Party Notices missing - UNLICENSED!");

            return MD2RichText(ta.text);
        }

        private string MD2RichText(string text)
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

        protected override void Awake()
        {
            base.Awake();

            lbl_LicenseText.text = LoadLicenseText();
        }

    }
}
