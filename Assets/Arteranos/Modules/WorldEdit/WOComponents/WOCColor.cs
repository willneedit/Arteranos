/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using Ipfs.Unity;
using ProtoBuf;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using TaskScheduler = Arteranos.Core.TaskScheduler;

namespace Arteranos.WorldEdit.Components
{
    [ProtoContract]
    public class WOCColor : WOCBase
    {
        [ProtoMember(1)]
        public WOColor color;

        [ProtoMember(2)]
        public string texturePNGCid;

        private Renderer renderer = null;

        public override GameObject GameObject
        {
            get => base.GameObject;
            set
            {
                base.GameObject = value;
                GameObject.TryGetComponent(out renderer);
            }
        }

        public override bool IsRemovable => false;

        public override void CommitState()
        {
            base.CommitState();

            if (renderer != null)
            {
                renderer.material.color = color;
                if (texturePNGCid != null)
                    TaskScheduler.ScheduleCoroutine(LoadTextureFromIPFS);
                else
                    renderer.material.mainTexture = null;
            }

            Dirty = false;
        }

        private IEnumerator LoadTextureFromIPFS()
        {
            if (texturePNGCid == null || renderer == null) yield break;

            using CancellationTokenSource cts = new(8000);
            byte[] data = null;
            yield return Asyncs.Async2Coroutine(
                () => G.IPFSService.ReadBinary(texturePNGCid, cancel: cts.Token),
                _data => data = _data);

            if (data?.Length <= 0) yield break;

            bool result = false;
            Texture2D tex = new(2, 2);

            Task<bool> resultTask = AsyncImageLoader.LoadImageAsync(tex, data);
            yield return new WaitUntil(() => resultTask.IsCompleted);
            result = resultTask.Result;

            if(result) renderer.material.mainTexture = tex;
        }

        public void SetState(Color color)
        {
            this.color = color;
            Dirty = true;
        }

        public void SetState(string tex)
        {
            this.texturePNGCid = tex;
            Dirty = true;
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }

        public override (string name, GameObject gameObject) GetUI()
            => ("Appearance", BP.I.WorldEdit.ColorInspector);

        public override void ReplaceValues(WOCBase wOCBase)
        {
            WOCColor c = wOCBase as WOCColor;

            color = c.color;
            texturePNGCid = c.texturePNGCid;
        }

        public override HashSet<AssetReference> GetAssetReferences()
        {
            if (texturePNGCid == null) return base.GetAssetReferences();

            return new() { new("Textures", texturePNGCid) };
        }
    }
}
