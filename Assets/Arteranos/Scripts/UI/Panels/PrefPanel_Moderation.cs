/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using Arteranos.Core;
using System.IO;
using Arteranos.Services;
using System.Collections;
using Ipfs;
using Ipfs.Unity;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;
using System.Threading;

namespace Arteranos.UI
{
    public class PrefPanel_Moderation : UIBehaviour
    {
        public TMP_InputField txt_ServerName = null;
        public TMP_InputField txt_ServerPort = null;

        public Toggle chk_UseUPnP = null;

        public TMP_InputField txt_Description = null;
        public IconSelectorBar bar_IconSelector = null;

        public Button btn_WorldGallery = null;
        public Button btn_ContentPermissions = null;

        public Toggle chk_Flying = null;

        public Toggle chk_Public = null;

        public Button btn_ClearCaches = null;

        private Cid ServerIcon = null;
        private ServerPermissions Permissions = null;

        private bool dirty = false;

        protected override void Awake()
        {
            base.Awake();

            txt_ServerName.onValueChanged.AddListener(SetDirty);
            txt_Description.onValueChanged.AddListener(SetDirty);

            txt_ServerPort.onValueChanged.AddListener(SetDirty);
            chk_UseUPnP.onValueChanged.AddListener(SetDirty);

            chk_Public.onValueChanged.AddListener(SetDirty);

            btn_WorldGallery.onClick.AddListener(OnWorldGalleryClicked);
            btn_ContentPermissions.onClick.AddListener(OnContentPermissionsClicked);

            chk_Flying.onValueChanged.AddListener(SetDirty);

            btn_ClearCaches.onClick.AddListener(OnClearCachesClicked);

            bar_IconSelector.OnIconChanged += Bar_IconSelector_OnIconChanged;
        }

        private void Bar_IconSelector_OnIconChanged(byte[] obj)
        {
            IEnumerator UploadIcon(byte[] data)
            {
                using MemoryStream ms = new(obj);
                ms.Position = 0;
                yield return Asyncs.Async2Coroutine(IPFSService.AddStream(ms), _fsn => ServerIcon = _fsn.Id);

                dirty = true;
            }

            StartCoroutine(UploadIcon(obj));
        }

        private void SetDirty(bool _) => dirty = true;
        private void SetDirty(string _) => dirty = true;

        protected override void OnEnable()
        {
            base.OnEnable();

            SettingsManager.OnClientReceivedServerConfigAnswer += UpdateServerConfigDisplay;

            bool networkConfig = G.NetworkStatus.GetOnlineLevel() == OnlineLevel.Offline;

            // Changing these settings remotely can pull the rug under your own feet.
            // Like remotely (mis)configuring a department's firewall on his workplace.
            // Believe me. Fastest car ride he pulled off....
            txt_ServerPort.interactable = networkConfig;
            chk_UseUPnP.interactable = networkConfig;

            // Send the query.
            SettingsManager.EmitToServerCTSPacket(new CTSServerConfig()
            {
                config = null
            });
        }

        private void UpdateServerConfigDisplay(ServerJSON ss)
        {
            ServerIcon = ss.ServerIcon;
            Permissions = ss.Permissions;

            txt_ServerPort.text = ss.ServerPort.ToString();
            chk_UseUPnP.isOn = ss.UseUPnP;

            txt_ServerName.text = ss.Name;
            txt_Description.text = ss.Description;

            chk_Flying.isOn = ss.Permissions.Flying ?? true;

            chk_Public.isOn = ss.Public;

            bar_IconSelector.IconPath = ss.ServerIcon;

            dirty = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SettingsManager.OnClientReceivedServerConfigAnswer -= UpdateServerConfigDisplay;

            if (!dirty) return;
            dirty = false;

            ServerJSON ss = new()
            {
                ServerIcon = ServerIcon,
                Permissions = Permissions,
                ServerPort = int.Parse(txt_ServerPort.text),
                UseUPnP = chk_UseUPnP.isOn,
                Name = txt_ServerName.text,
                Description = txt_Description.text,
                Public = chk_Public.isOn,
            };

            ss.Permissions.Flying = chk_Flying.isOn;

            // Send off the updated config to the server
            SettingsManager.EmitToServerCTSPacket(new CTSServerConfig() { config = ss });
        }

        private void OnWorldGalleryClicked()
        {
            SysMenuStatic.CloseSysMenus();

            WorldPanelUI.New();
        }

        private void OnContentPermissionsClicked()
        {
            SysMenuStatic.CloseSysMenus();

            ContentFilterUI cui = ContentFilterUI.New();

            cui.spj = Permissions;

            cui.OnFinishConfiguring += () => dirty = true;
        }

        private void OnClearCachesClicked()
        {
            async Task SetPin(Cid toPin, bool state)
            {
                try
                {
                    await IPFSService.PinCid(toPin, state);
                }
                catch { }
            }

            IEnumerator ClearCacheCoroutine()
            {
                btn_ClearCaches.interactable = false;

                HashSet<string> toPin = new();

                // Favourited worlds
                List<Cid> pinned = null;
                int favouritedWorlds = 0;
                foreach (Cid cid in WorldInfo.ListFavourites())
                {
                    favouritedWorlds++;
                    toPin.Add(cid);
                }

                // User and server icons
                toPin.Add(SettingsManager.Client.Me.UserIconCid);
                toPin.Add(SettingsManager.Server.ServerIcon);

                // Current avatar
                toPin.Add(SettingsManager.Client.Me.CurrentAvatar.AvatarCidString);

                // Stored avatars
                int storedAvatars = 0;
                foreach(AvatarDescriptionJSON entry in SettingsManager.Client.Me.AvatarGallery)
                {
                    storedAvatars++;
                    toPin.Add(entry.AvatarCidString);
                }

                // Identification file and current server description
                toPin.Add(IPFSService.IdentifyCid);
                toPin.Add(IPFSService.CurrentSDCid);

                // Default avatars
                toPin.Add(SettingsManager.DefaultMaleAvatar);
                toPin.Add(SettingsManager.DefaultFemaleAvatar);

                // World Editor Assets Collections: glTF models
                foreach (WOCEntry item in SettingsManager.Client.WEAC.WorldObjectsGLTF)
                    toPin.Add(item.IPFSPath);

                // World Editor Assets Collections: Kits
                foreach (WOCEntry item in SettingsManager.Client.WEAC.WorldObjectsKits)
                    toPin.Add(item.IPFSPath);

                // Eat up any empty entries
                toPin.Remove(null);

                Debug.Log($"Total needs pinning: {toPin.Count}, out of {favouritedWorlds} worlds and {storedAvatars} stored avatars");

                yield return Asyncs.Async2Coroutine(IPFSService.ListPinned(), _pinned => pinned = _pinned.ToList());

                Debug.Log($"Actual pinned (including indirect): {pinned.Count}");

                // Unpin everything we don't want
                foreach (Cid entry in pinned)
                {
                    if(!toPin.Contains(entry))
                        yield return Asyncs.Async2Coroutine(SetPin(entry, false));
                }

                Debug.Log("Now, garbage collection.");
                // And, scrub...
                yield return Asyncs.Async2Coroutine(IPFSService.RemoveGarbage());

                btn_ClearCaches.interactable = true;
            }

            StartCoroutine(ClearCacheCoroutine());
        }
    }
}
