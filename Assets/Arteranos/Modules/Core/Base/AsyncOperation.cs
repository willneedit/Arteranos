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
        string Caption { get; }

        Action<float> ProgressChanged { get; set; }

        Task<T> ExecuteAsync(T context, CancellationToken token);
    }

    public class AsyncOperationExecutor<T> where T : Context
    {
        public event Action<float, string> ProgressChanged;
        public event Action<T> Completed;

        public int Timeout = 600;

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
        private async Task<T> ExecuteAsync(T context)
        {
            tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(Timeout));
            weightSoFar = 0f;

            foreach(IAsyncOperation<T> operation in asyncOperations)
            {
                operation.ProgressChanged += OnProgressChanged;
                operation.Timeout = Timeout;
                currentOperation = operation;

                try
                {
                    // Even if particular operations would be technically synced, we have to
                    // look for that occasion.
                    if (tokenSource.IsCancellationRequested)
                        throw new OperationCanceledException();

                    OnProgressChanged(0f);
                    context = await operation.ExecuteAsync(context, tokenSource.Token); //.ConfigureAwait(false);
                    OnProgressChanged(1f);

                    weightSoFar += operation.Weight;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
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
        public IEnumerator ExecuteCoroutine(T context, Action<AggregateException, T> callback = null)
        {
            Task<T> ao = Task.Run(async () =>
            {
                return await ExecuteAsync(context);
            });

            yield return new WaitUntil(() => ao.IsCompleted);

            // From good to bad: Either RanToCompletion, Canceled or Faulted
            // ao.Result throws if it's not completed successfully.
            callback?.Invoke(
                ao.IsCompletedSuccessfully ? default : ao.Exception, 
                ao.IsCompletedSuccessfully ? ao.Result : default);
        }

        public void Cancel()
        {
            if(!tokenSource.IsCancellationRequested) tokenSource.Cancel();
        }

        private void OnProgressChanged(float progress)
        {
            // Convert the range of the single task's progress of 0.0...1.0 into the greater picture.
            float currentProgress = (weightSoFar + progress * currentOperation.Weight) / totalWeight;
            TaskScheduler.ScheduleCallback(() => ProgressChanged?.Invoke(currentProgress, currentOperation.Caption));
        }
    }
}
