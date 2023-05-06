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
using UnityEngine.Events;

namespace Arteranos.UI
{
    public class ContentFilterUI : UIBehaviour
    {
        public Transform tbl_Table;
        public TextMeshProUGUI lbl_HelpText;

        public ServerPermissionsJSON spj = null;

        private readonly List<Button> btns_Help = new();
        private readonly List<Spinner> spns_Config = new();

        public static ContentFilterUI New()
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/UI_ContentFilter"));
            return go.GetComponent<ContentFilterUI>();
        }

        protected override void Awake()
        {
            string[] spnOptions = new string[]
            {
                "Forbidden",
                "Uncertain",
                "Allowed"
            };

            base.Awake();

            UnityAction makeShowHelpText(int row) =>
                () => ShowHelpText(row);

            Action<int, bool> makeOnSpinnerClicked(int row) =>
                (val, up) => OnSpinnerClicked(row, val);

            Transform HelpColumn = tbl_Table.GetChild(1);
            Transform ConfigColumn = tbl_Table.GetChild(2);

            Debug.Assert(HelpColumn.childCount == ConfigColumn.childCount);

            for(int i = 0, c = ConfigColumn.childCount;i < c;i++)
            {
                Button btn = HelpColumn.GetChild(i).GetComponent<Button>();
                btn.onClick.AddListener(makeShowHelpText(i));
                btns_Help.Add(btn);

                Spinner spn = ConfigColumn.GetChild(i).GetComponent<Spinner>();
                spn.OnChanged += makeOnSpinnerClicked(i);
                spn.Options = spnOptions;
                spns_Config.Add(spn);
            }

            lbl_HelpText.text = string.Empty;
        }

        private void OnSpinnerClicked(int _1, int _2) => lbl_HelpText.text = string.Empty;

        private void ShowHelpText(int row)
        {
            string txt = null;
            switch(row)
            {
                case 0: txt =
                        "<b>Explicit Nudity</b>\n" +
                        "Detailed depiction of sexual acts or derivation thereof, " +
                        "be it still or animated.\n" +
                        "<color=#ff8888><b>Not for children at all!</b></color>";
                    break;
                case 1: txt =
                        "<b>Nudity</b>\n" +
                        "Nudity, non-sexual or artistic.\n";
                    break;
                case 2: txt =
                        "<b>(sexually) suggestive</b>\n" +
                        "Not including nudity, but characters depicted in the content look like " +
                        "or behave with the flirtatious actions or in an outright " +
                        "sexual manner.";
                    break;
                case 3: txt =
                        "<b>Cartoon / \"Clean\" violence</b>\n" +
                        "Violence, but no depictions of lasting harm, no blood, no gore.";
                    break;
                case 4: txt =
                        "<b>Realistic/Excessive violence</b>\n" +
                        "1. Violence, depicted in a realistic manner. Blood and/or gore.\n" +
                        "2. Excessive violence. Self-harm, or torture.\n" +
                        "<color=#ff8888><b>Not for children at all!</b></color>";
                    break;
            }

            lbl_HelpText.text = txt;
        }

        bool? Spn2bool(Spinner spn)
        {
            return spn.value switch
            {
                0 => false,
                2 => true,
                _ => null,
            };
        }

        int Bool2spn(bool? b)
        {
            return b switch
            {
                false => 0,
                true => 2,
                _ => 1,
            };
        }

        // I _could_ use Reflections to indirectly access fields of an object,
        // but it's too much boilerplate for just five fields.
        protected override void Start()
        {
            base.Start();

            Debug.Assert(spj != null, "No association of the Content Filter UI");

            spns_Config[0].value = Bool2spn(spj.ExplicitNudes);
            spns_Config[1].value = Bool2spn(spj.Nudity);
            spns_Config[2].value = Bool2spn(spj.Suggestive);
            spns_Config[3].value = Bool2spn(spj.Violence);
            spns_Config[4].value = Bool2spn(spj.ExcessiveViolence);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            Debug.Assert(spj != null, "No association of the Content Filter UI");

            spj.ExplicitNudes = Spn2bool(spns_Config[0]);
            spj.Nudity = Spn2bool(spns_Config[1]);
            spj.Suggestive = Spn2bool(spns_Config[2]);
            spj.Violence = Spn2bool(spns_Config[3]);
            spj.ExcessiveViolence = Spn2bool(spns_Config[4]);
        }
    }
}
