/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Ipfs.Unity;
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Arteranos.UI
{
    public class IconSelectorBar : UIBehaviour
    {
        public TMP_InputField txt_IconURL = null;
        public Button btn_Icon = null;
        public IPFSImage img_IconImage = null;

        public string IconPath
        {
            get => img_IconImage.Path;
            set => img_IconImage.Path = value;
        }

        public byte[] IconData
        {
            get => img_IconImage.ImageData;
            set => img_IconImage.ImageData = value;
        }

        public event Action<byte[]> OnIconChanged;

        protected override void Awake()
        {
            base.Awake();

            btn_Icon.onClick.AddListener(OnIconClicked);
            txt_IconURL.onSubmit.AddListener(_ => OnIconClicked());
        }

        private void OnIconClicked()
        {
            IEnumerator DownloadIcon(string iconURL)
            {
                // Strip quotes
                if (iconURL.StartsWith("\"") && iconURL.EndsWith("\""))
                    iconURL = iconURL[1..^1];

                // No protocol, naked file
                if (!iconURL.Contains("://"))
                    iconURL = "file:///" + iconURL;

                UnityWebRequest www = UnityWebRequestTexture.GetTexture(iconURL);
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    DownloadHandler dh = www.downloadHandler;
                    byte[] data = dh.nativeData.ToArray();

                    yield return UpdateIconCoroutine(data);
                }
            }

            StartCoroutine(DownloadIcon(txt_IconURL.text));
        }

        private IEnumerator UpdateIconCoroutine(byte[] data)
        {
            if (data == null) yield break;

            Texture2D tex = new(2, 2);
            bool result = false;

            yield return Asyncs.Async2Coroutine(
                AsyncImageLoader.LoadImageAsync(tex, IconData),
                _r => result = _r);

            if (tex == null || !result) yield break;

            if (tex.width > 512 || tex.height > 512)
                yield break;

            if (tex.width < 128 || tex.height < 128)
                yield break;

            IconData = data;
            OnIconChanged?.Invoke(IconData);
        }
    }
}
