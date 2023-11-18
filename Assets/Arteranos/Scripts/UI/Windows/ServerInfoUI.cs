/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */


using Arteranos.Core;
using Arteranos.Web;
using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arteranos.UI
{
    public class ServerInfoUI : UIBehaviour
    {
        public static ServerInfoUI New(ServerPublicData? spd, ServerOnlineData? sod)
        {
            GameObject blueprint = Resources.Load<GameObject>("UI/UI_ServerInfo");
            blueprint.SetActive(false);
            GameObject go = Instantiate(blueprint);
            ServerInfoUI serverInfoUI = go.GetComponent<ServerInfoUI>();
            serverInfoUI.spd = spd;
            serverInfoUI.sod = sod;
            go.SetActive(true);
            return serverInfoUI;
        }

        [SerializeField] private Button btn_Close;
        [SerializeField] private TMP_Text lbl_Name;
        [SerializeField] private TMP_Text lbl_Address;
        [SerializeField] private Image img_Icon;
        [SerializeField] private TMP_Text lbl_LastUpdated;
        [SerializeField] private TMP_Text lbl_LastOnline;
        [SerializeField] private TMP_Text lbl_MatchIndex;
        [SerializeField] private TMP_Text lbl_AdminList;
        [SerializeField] private TMP_Text lbl_World;
        [SerializeField] private TMP_Text lbl_Description;

        private ServerPublicData? spd = null;
        private ServerOnlineData? sod = null;

        protected override void Awake()
        {
            base.Awake();

            btn_Close.onClick.AddListener(() => Destroy(gameObject));
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Populate();
        }

        private void Populate()
        {
            IEnumerator Visualize()
            {
                yield return null;

                if (spd != null)
                {
                    lbl_Name.text = spd.Value.Name;
                    lbl_Address.text = spd.Value.Address;
                    lbl_LastUpdated.text = spd.Value.LastUpdated.HumanReadable();
                    lbl_LastOnline.text = spd.Value.LastOnline.HumanReadable();
                    lbl_MatchIndex.text = spd.Value.Permissions.HumanReadableMI(
                        SettingsManager.Client.ContentFilterPreferences
                        ).ToString();
                    lbl_Description.text = spd.Value.Description.ToString();

                    lbl_AdminList.text = string.Join(", ", spd.Value.AdminNames);
                }

                if (sod != null)
                {
                    string currentWorld = sod.Value.CurrentWorld;

                    Utils.ShowImage(sod.Value.Icon, img_Icon);
                    lbl_World.text = string.IsNullOrEmpty(currentWorld) ? "Unknown" : currentWorld;
                }
            }

            SettingsManager.StartCoroutineAsync(Visualize);
        }
    }
}