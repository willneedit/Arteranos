/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 * 
 * Code idea seen in Ready Player Me SDK, modified and extended by willneedit
 */

using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Arteranos.Core
{
    public class Context { }

    public interface IAsyncOperation<T> where T : Context
    {
        int Timeout { get; set; }
        float Weight { get; set; }
        string Caption { get; set; }

        Action<float> ProgressChanged { get; set; }

        Task<T> ExecuteAsync(T context, CancellationToken token);
    }

    public class AsyncOperationExecutor<T> where T : Context
    {
        public event Action<float, string> ProgressChanged;
        public event Action<T> Completed;

        public int Timeout;
        public bool IsCancelled => tokenSource.IsCancellationRequested;

        private readonly IAsyncOperation<T>[] asyncOperations;
        private readonly float totalWeight;

        private CancellationTokenSource tokenSource;
        private float weightSoFar;

        private IAsyncOperation<T> currentOperation;

        public AsyncOperationExecutor(IAsyncOperation<T>[] asyncOperations)
        {
            this.asyncOperations = asyncOperations;

            totalWeight = 0f;
            foreach(IAsyncOperation<T> operation in asyncOperations)
                totalWeight += (operation.Weight == 0) ? 1f : operation.Weight;
        }


        /// <summary>
        /// Execute complex operations with the async/await paradigm.
        /// </summary>
        /// <param name="context">The context to work on</param>
        /// <returns>The awaitable Task containing the updated context</returns>
        public async Task<T> ExecuteAsync(T context)
        {
            tokenSource = new CancellationTokenSource();
            weightSoFar = 0f;

            foreach(IAsyncOperation<T> operation in asyncOperations)
            {
                operation.ProgressChanged += OnProgressChanged;
                operation.Timeout = Timeout;
                currentOperation = operation;

                try
                {
                    OnProgressChanged(0f);
                    context = await operation.ExecuteAsync(context, tokenSource.Token);
                    OnProgressChanged(1f);

                    weightSoFar += operation.Weight;
                }
                catch
                {
                    if(tokenSource.IsCancellationRequested) tokenSource.Dispose();
                    throw;
                }

                operation.ProgressChanged -= OnProgressChanged;
            }

            Completed?.Invoke(context);
            return context;
        }

        /// <summary>
        /// Execute complex operations inside an encapsulating coroutine.
        /// </summary>
        /// <param name="context">The context to work on</param>
        /// <param name="callback">Callback to be notified with the finished context</param>
        /// <returns>The Enumerator to work with the Coroutine framework</returns>
        public IEnumerator ExecuteCoroutine(T context, Action<TaskStatus, T> callback = null)
        {
            Task<T> ao = ExecuteAsync(context);

            yield return new WaitUntil(() => ao.IsCompleted);

            // From good to bad: Either RanToCompletion, Canceled or Faulted
            callback?.Invoke(ao.Status, ao.Result);
        }

        public void Cancel()
        {
            if(!tokenSource.IsCancellationRequested) tokenSource.Cancel();
        }

        private void OnProgressChanged(float progress)
        {
            // Convert the range of the single task's progress of 0.0...1.0 into the greater picture.
            float currentProgress = (weightSoFar + progress * currentOperation.Weight) / totalWeight;
            ProgressChanged?.Invoke(currentProgress, currentOperation.Caption);
        }
    }
}
