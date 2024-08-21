/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;
using Arteranos.Core;
using Arteranos.Services;
using System.Threading.Tasks;
using Ipfs;
using System.Collections.Generic;
using System.IO;
using ProtoBuf;
using System.Threading;
using System.Collections;
using Ipfs.Unity;
using System.Diagnostics;

namespace Arteranos.WorldEdit
{
    public class SaveWorldPanel : UIBehaviour
    {
        public Button btn_ReturnToList;
        public TextMeshProUGUI lbl_Author;
        public TextMeshProUGUI lbl_Template;
        public TMP_InputField txt_WorldName;
        public TMP_InputField txt_WorldDescription;

        public Toggle chk_Violence;
        public Toggle chk_Nudity;
        public Toggle chk_Suggestive;
        public Toggle chk_ExViolence;
        public Toggle chk_ExNudity;

        public Button btn_SaveAsZip;
        public Button btn_SaveInGallery;

        public event Action OnReturnToList;

        private string templatePattern;

        protected override void Awake()
        {
            base.Awake();

            templatePattern = lbl_Template.text;

            btn_ReturnToList.onClick.AddListener(GotRTLClick);
            btn_SaveAsZip.onClick.AddListener(GotSaveAsZipClick);
            btn_SaveInGallery.onClick.AddListener(GotSaveInGalleryClick);

            txt_WorldName.onValueChanged.AddListener(GotWorldNameChange);
            txt_WorldDescription.onValueChanged.AddListener(GotWorldDescriptionChange);

            chk_Violence.onValueChanged.AddListener(
                b => GotCWChanged(b, ref G.WorldEditorData.ContentWarning.Violence));
            chk_Nudity.onValueChanged.AddListener(
                b => GotCWChanged(b, ref G.WorldEditorData.ContentWarning.Nudity));
            chk_Suggestive.onValueChanged.AddListener(
                b => GotCWChanged(b, ref G.WorldEditorData.ContentWarning.Suggestive));
            chk_ExViolence.onValueChanged.AddListener(
                b => GotCWChanged(b, ref G.WorldEditorData.ContentWarning.ExcessiveViolence));
            chk_ExNudity.onValueChanged.AddListener(
                b => GotCWChanged(b, ref G.WorldEditorData.ContentWarning.ExplicitNudes));


            G.NetworkStatus.OnNetworkStatusChanged += GotNetworkStatusChange;
        }

        private void GotCWChanged(bool b, ref bool? cwItem) => cwItem = b;

        protected override void Start()
        {
            base.Start();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            G.NetworkStatus.OnNetworkStatusChanged -= GotNetworkStatusChange;
        }

        private void GotWorldNameChange(string name) => G.WorldEditorData.WorldName = name;

        private void GotWorldDescriptionChange(string description) => G.WorldEditorData.WorldDescription = description;

        private void GotNetworkStatusChange(ConnectivityLevel level1, OnlineLevel level2) 
            => RefreshContentWarning();

        protected override void OnEnable()
        {
            base.OnEnable();

            lbl_Author.text = G.Client.MeUserID;
            txt_WorldName.text = G.WorldEditorData.WorldName;
            txt_WorldDescription.text = G.WorldEditorData.WorldDescription;

            // FIXME Needs to fall back to the template Cid, even if it's a
            // derivation of an already hand-edited world
            if (G.World.Cid == null)
                lbl_Template.text = "None";
            else
                lbl_Template.text = string.Format(templatePattern,
                    G.World.Cid,
                    G.World.Name);

            EnableSaveButtons();

            RefreshContentWarning();
        }

        private void EnableSaveButtons()
        {
            // World needs templates....
            btn_SaveAsZip.interactable = (G.World.Cid != null);
            btn_SaveInGallery.interactable = (G.World.Cid != null);
        }

        private void RefreshContentWarning()
        {
            static void PresetPermission(Toggle tg, bool? perm, ref bool? cw)
            {
                if (perm == null) // Server says, don't care
                {
                    tg.interactable = true;
                    tg.isOn = cw ?? false;
                }
                else if (perm == false) // Server forbids the content
                {
                    tg.interactable = false;
                    tg.isOn = false;
                    cw = false;
                }
                else if (perm == true) // Server allows the content, maybe even likely in use
                {
                    tg.interactable = true;
                    tg.isOn = cw ?? true;
                }
            }

            ServerPermissions p = G.NetworkStatus.GetOnlineLevel() == OnlineLevel.Offline
                ? new()
                {
                    Violence = null,
                    Nudity = null,
                    Suggestive = null,
                    ExcessiveViolence = null,
                    ExplicitNudes = null,
                }
                : SettingsManager.ActiveServerData.Permissions;

            ServerPermissions cw = G.WorldEditorData.ContentWarning;

            // Restrict the permission settings depending on the active server.
            // Like, disallowing to build XXX content on a PG-13 server.
            // If the world builder wants to, he'd have to switch servers.
            // Or set up his own.
            PresetPermission(chk_Violence, p.Violence, ref cw.Violence);
            PresetPermission(chk_Nudity, p.Nudity, ref cw.Nudity);
            PresetPermission(chk_Suggestive, p.Suggestive, ref cw.Suggestive);
            PresetPermission(chk_ExViolence, p.ExcessiveViolence, ref cw.ExcessiveViolence);
            PresetPermission(chk_ExNudity, p.ExplicitNudes, ref cw.ExplicitNudes);
        }

        private void GotSaveInGalleryClick()
        {
            IEnumerator Cor()
            {
                WorldDecoration world = AssembleWorldDecoration();

                IFileSystemNode fsn = null;

                yield return Asyncs.Async2Coroutine(
                    AssembleWorldDirectory(G.World.Cid, world),
                    result => fsn = result);


                // Pin and enter as a favourite
                world.info.WorldCid = fsn.Id;
                WorldInfo wi = new()
                {
                    win = world.info,
                    Updated = DateTime.UtcNow
                };
                wi.Favourite();
                UnityEngine.Debug.Log($"Full world CID: {fsn.Id}");

                EnableSaveButtons();
            }

            StartCoroutine(Cor());
        }

        private void GotSaveAsZipClick()
        {
            throw new NotImplementedException();
        }

        private WorldDecoration AssembleWorldDecoration()
        {
            WorldInfoNetwork wi = new()
            {
                Author = G.Client.MeUserID,
                ContentRating = G.WorldEditorData.ContentWarning,
                Created = DateTime.UtcNow,
                WorldName = G.WorldEditorData.WorldName,
                WorldDescription = G.WorldEditorData.WorldDescription,
                WorldCid = null, // Cannot create a self-reference, delay it to the WorldDownloader
                ScreenshotPNG = null,
                Signature = null,
            };

            WorldDecoration wd = new()
            { 
                info = wi
            };

            wd.TakeSnapshot();

            return wd;
        }

        private async Task<IFileSystemNode> AssembleWorldDirectory(Cid template, WorldDecoration decor, CancellationToken cancel = default)
        {
            List<IFileSystemLink> files = new();

            using MemoryStream ms = new();
            Serializer.Serialize(ms, decor);
            ms.Position = 0;
            IFileSystemNode fsnDecor = await G.IPFSService.AddStream(ms, "Decoration", cancel: cancel).ConfigureAwait(false);
            files.Add(fsnDecor.ToLink());

            IFileSystemNode fsnTemplate = await G.IPFSService.ListFile(template, cancel: cancel).ConfigureAwait(false);
            files.Add(fsnTemplate.ToLink("Template"));

            // TODO Link external resources, like glTF, kits, custom textures
            // or skyboxes, too.
            // Reason: Recursive pinning, and possibility of resource preloading

            return await G.IPFSService.CreateDirectory(files, cancel: cancel).ConfigureAwait(false);
        }

        private void GotRTLClick()
        {
            OnReturnToList?.Invoke();
        }
    }
}
