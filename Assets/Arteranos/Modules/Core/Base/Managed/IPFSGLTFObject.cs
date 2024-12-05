/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using GLTFast;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


namespace Arteranos.Core.Managed
{
    public class IPFSGLTFObject : ManagedHandle<CancellationTokenSource>
    {
        public string Path { get; set; } = null;
        public bool InitActive { get; set; } = true;
        public GameObject RootObject { get; set; } = null;

        public AsyncLazy<GameObject> GameObject { get; private set; } = null;
        public Bounds? Bounds { get; private set; } = null;

        public IPFSGLTFObject(string path = null, CancellationToken? cancel = null) : base(
            () => null,
            r => Disposer(r)
            )
        {
            Path = path;

            // If we get a (timeout) token, link it with the source for disposing
            Attach(cancel != null 
                ? CancellationTokenSource.CreateLinkedTokenSource(cancel.Value) 
                : new CancellationTokenSource());

            GameObject = new(() => Loader(resource.Token));
        }

        private async Task<GameObject> Loader(CancellationToken cancel)
        {
            GameObject goTmp = null;
            using SemaphoreSlim waiter = new(0, 1);

            IEnumerator LoaderCoroutine(byte[] data)
            {
                try
                {
                    GltfImport gltf = new(deferAgent: new UninterruptedDeferAgent());

                    Task<bool> taskSuccess = gltf.LoadGltfBinary(data, cancellationToken: cancel);
                    yield return new WaitUntil(() => taskSuccess.IsCompleted);
                    bool success = taskSuccess.IsCompletedSuccessfully && taskSuccess.Result;

                    if (!success) yield break;

                    goTmp = RootObject ? RootObject : new();
                    goTmp.SetActive(false);

                    GameObjectBoundsInstantiator instantiator = new(gltf, goTmp.transform);

                    Task task = gltf.InstantiateMainSceneAsync(instantiator);
                    yield return new WaitUntil(() => task.IsCompleted);

                    Bounds = instantiator.CalculateBounds();
                }
                finally
                {
                    if(goTmp != null && InitActive) goTmp.SetActive(InitActive);
                    waiter.Release();
                }
            }

            byte[] data = null;
            try
            {
                data = await G.IPFSService.ReadBinary(Path, cancel: cancel);
            }
            catch (Exception ex)
            {
                if(ex is not TaskCanceledException) Debug.LogException(ex);
                return null;
            }

            // Bounce parts of the loader to the main task,
            // we needt it for the object instantiation
            TaskScheduler.ScheduleCoroutine(() => LoaderCoroutine(data));
            await waiter.WaitAsync(cancel);

            return goTmp;
        }

        // ---------------------------------------------------------------
        private static void Disposer(CancellationTokenSource tokenSource)
        {
            if (tokenSource == null) return;
            tokenSource.Cancel();
            tokenSource.Dispose();
        }
    }
}