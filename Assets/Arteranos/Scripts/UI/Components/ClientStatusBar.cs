/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Services;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arteranos.UI
{
    public class ClientStatusBar : UIBehaviour
    {
        public TMP_Text lbl_Status = null;
        public Button btn_Offline = null;
        public Button btn_Server = null;
        public Button btn_Host = null;

        protected override void Awake()
        {
            base.Awake();

            btn_Offline.onClick.AddListener(OnOfflineClicked);
            btn_Server.onClick.AddListener(OnServerClicked);
            btn_Host.onClick.AddListener(OnHostClicked);
        }

        protected override void Start()
        {
            base.Start();

            NetworkStatus.Instance.OnNetworkStatusChanged += OnNetworkStatusChanged;

            OnNetworkStatusChanged(
                NetworkStatus.GetConnectivityLevel(), 
                NetworkStatus.GetOnlineLevel());
        }

        protected override void OnDestroy() => NetworkStatus.Instance.OnNetworkStatusChanged -= OnNetworkStatusChanged;

        private void OnOfflineClicked() => NetworkStatus.StopHost();

        private void OnServerClicked() => NetworkStatus.StartServer();

        private void OnHostClicked() => NetworkStatus.StartHost();


        private void OnNetworkStatusChanged(ConnectivityLevel conn, 
            OnlineLevel onl)
        {
            string connstr = Core.Utils.GetEnumDescription(conn);
            string onlstr = Core.Utils.GetEnumDescription(onl);

            lbl_Status.text = $"{connstr}, {onlstr}";

            switch(onl)
            {
                case OnlineLevel.Offline:
                    btn_Offline.gameObject.SetActive(false);
                    btn_Server.gameObject.SetActive(true);
                    btn_Host.gameObject.SetActive(true);
                    break;
                case OnlineLevel.Client:
                    btn_Offline.gameObject.SetActive(true);
                    btn_Server.gameObject.SetActive(false);
                    btn_Host.gameObject.SetActive(true);
                    break;
                case OnlineLevel.Server:
                    btn_Offline.gameObject.SetActive(true);
                    btn_Server.gameObject.SetActive(false);
                    btn_Host.gameObject.SetActive(false);
                    break;
                case OnlineLevel.Host:
                    btn_Offline.gameObject.SetActive(true);
                    btn_Server.gameObject.SetActive(false);
                    btn_Host.gameObject.SetActive(false);
                    break;
            }
        }
    }
}
