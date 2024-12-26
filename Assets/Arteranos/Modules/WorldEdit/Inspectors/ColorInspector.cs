/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Arteranos.WorldEdit.Components;
using Ipfs.Unity;
using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TaskScheduler = Arteranos.Core.TaskScheduler;

namespace Arteranos.WorldEdit
{
    public class ColorInspector : UIBehaviour, IInspector
    {
        public ColorPicker ColorPicker;
        public Button btn_PickTexture;
        public Button btn_ClearTexture;

        public WOCBase Woc { get; set; }
        public PropertyPanel PropertyPanel { get; set; }

        protected override void Awake()
        {
            base.Awake();

            Debug.Assert(Woc != null);
            Debug.Assert(PropertyPanel);

            ColorPicker.OnColorChanged += GotColorChanged;
            btn_PickTexture.onClick.AddListener(GetPickTextureClick);
            btn_ClearTexture.onClick.AddListener(GotClearTextureClick);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Populate();
        }

        public void Populate()
        {
            ColorPicker.SetColorWithoutNotify((Woc as WOCColor).color);
        }

        private void GotColorChanged(Color obj)
        {
            (Woc as WOCColor).SetState(obj);

            PropertyPanel.CommitModification(this);
        }

        private void GotClearTextureClick()
        {
            (Woc as WOCColor).SetState(null);

            PropertyPanel.CommitModification(this);
        }

        private void GetPickTextureClick()
        {
            ActionRegistry.Call(
                "fileBrowser",
                new FileBrowserData() { Pattern = @".*\.(png|jpg|jfif)" },
                callback: GotTexturePickResult);
        }

        private void GotTexturePickResult(object obj)
        {
            if (obj == null) return;

            CommitTextureSelection(obj as string);
        }

        private void CommitTextureSelection(string texFile)
        {
            IEnumerator DownloadTexture(string texFile)
            {
                using Stream stream = File.OpenRead(texFile);
                stream.Seek(0, SeekOrigin.End);
                long length = stream.Position;
                stream.Seek(0, SeekOrigin.Begin);

                byte[] data = new byte[length];
                int n = stream.Read(data);

                if (n != length) yield break;

                Texture2D tex = new(2, 2);
                bool result = false;

                // No Async2Coroutine. AsyncImageLoader's initializer needs the main task.
                Task<bool> resultTask = AsyncImageLoader.LoadImageAsync(tex, data);
                yield return new WaitUntil(() => resultTask.IsCompleted);
                result = resultTask.Result;

                if (tex == null || !result) yield break;

                if (tex.width > 4096 || tex.height > 4096) yield break;

                string texCid = null;
                using MemoryStream ms = new(data);
                ms.Position = 0;
                yield return Asyncs.Async2Coroutine(() => G.IPFSService.AddStream(ms), _fsn => texCid = _fsn.Id);

                (Woc as WOCColor).SetState(texCid);

                PropertyPanel.CommitModification(this);
            }

            TaskScheduler.ScheduleCoroutine(() => DownloadTexture(texFile));
        }
    }
}
