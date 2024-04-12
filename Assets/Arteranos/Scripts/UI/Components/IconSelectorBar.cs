/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
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
        public RawImage img_IconImage = null;

        public byte[] IconData { get; set; } = null;

        public event Action<byte[]> OnIconChanged;

        protected override void Awake()
        {
            base.Awake();

            btn_Icon.onClick.AddListener(OnIconClicked);
            txt_IconURL.onSubmit.AddListener(_ => OnIconClicked());
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            TriggerUpdate();
        }

        public void TriggerUpdate()
        {
            StartCoroutine(UpdateIconCoroutine(IconData));
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

            Texture2D tex = null;
            yield return Utils.LoadImageCoroutine(data, _tex => tex = _tex);

            if (tex == null) yield break;

            if (tex.width > 512 || tex.height > 512)
                yield break;

            if (tex.width < 128 || tex.height < 128)
                yield break;

            if(img_IconImage.texture == null)
                img_IconImage.texture = tex;

            if (IconData == null || !IconData.SequenceEqual(data))
            {
                IconData = data;
                img_IconImage.texture = tex;
                OnIconChanged?.Invoke(IconData);
            }
        }
    }
}
