/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Ipfs.Unity;
using Arteranos.Services;
using System.Threading;
using GLTFast;

namespace Arteranos.WorldEdit
{
    public class glTFChooserTile : UIBehaviour
    {
        public string GLTFObjectPath { get; set; } = null;
        public GameObject LoadedObject { get; private set; } = null;

        public Button btn_PaneButton;
        public GameObject grp_ObjectAnchor;

        protected override void Start()
        {
            base.Start();

            IEnumerator Cor()
            {
                using CancellationTokenSource cts = new(10000);
                byte[] data = null;
                yield return Asyncs.Async2Coroutine(
                    IPFSService.ReadBinary(GLTFObjectPath, cancel: cts.Token),
                    _data => data = _data);

                if (data == null)
                    yield break;

                GltfImport gltf = new(deferAgent: new UninterruptedDeferAgent());

                bool success = false;

                yield return Asyncs.Async2Coroutine(
                    gltf.LoadGltfBinary(data, cancellationToken: cts.Token),
                    _success => success = _success);

                if(success)
                {
                    LoadedObject = new();
                    LoadedObject.SetActive(false);


                    GameObjectBoundsInstantiator instantiator = new(gltf, LoadedObject.transform);

                    yield return Asyncs.Async2Coroutine(
                        gltf.InstantiateMainSceneAsync(instantiator));

                    Bounds? b = instantiator.CalculateBounds();

                    LoadedObject.transform.SetParent(grp_ObjectAnchor.transform);
                    LoadedObject.SetActive(true);
                }
            }

            StartCoroutine(Cor());
        }
    }
}
