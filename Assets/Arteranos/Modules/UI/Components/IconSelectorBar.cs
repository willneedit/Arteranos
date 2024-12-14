/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arteranos.UI
{
    public class IconSelectorBar : UIBehaviour
    {
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
        }

        private void OnIconClicked()
        {
            void GotResult(object result)
            {
                if (result == null) return;

                CommitSelection(result as string);
            }
            ActionRegistry.Call(
                "fileBrowser",
                new FileBrowserData() { Pattern = @".*\.(png|jpg|jfif)" },
                callback: GotResult);
        }

        private void CommitSelection(string fileName)
        {
            IEnumerator DownloadIcon(string iconURL)
            {
                using Stream stream = File.OpenRead(iconURL);
                stream.Seek(0, SeekOrigin.End);
                long length = stream.Position;
                stream.Seek(0, SeekOrigin.Begin);

                byte[] data = new byte[length];
                int n = stream.Read(data);

                if(n != length) yield break;

                yield return UpdateIconCoroutine(data);
            }

            StartCoroutine(DownloadIcon(fileName));
        }

        private IEnumerator UpdateIconCoroutine(byte[] data)
        {
            if (data == null) yield break;

            Texture2D tex = new(2, 2);
            bool result = false;

            // No Async2Coroutine. AsyncImageLoader's initializer needs the main task.
            Task<bool> resultTask = AsyncImageLoader.LoadImageAsync(tex, data);
            yield return new WaitUntil(() => resultTask.IsCompleted);
            result = resultTask.Result;

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
