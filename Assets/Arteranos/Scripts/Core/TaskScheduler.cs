/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Arteranos.Core
{
    public class TaskScheduler : MonoBehaviour
    {
        public static TaskScheduler Instance = null;

        public int PoolSize { get; set; } = 10;

        private ConcurrentQueue<Func<Task>> Queued = new();
        private int Current = 0;

        private void Awake() => Instance = this;

        private void OnDestroy() => Instance = null;

        void Update()
        {
            if(!DequeueTask())
                gameObject.SetActive(false);
        }

        public static void Schedule(Func<Task> task) => Instance.Schedule_(task);

        private void Schedule_(Func<Task> task)
        {
            Queued.Enqueue(task);
            gameObject.SetActive(true);
        }

        private Task ExecuteTask(Func<Task> task)
        {
            return Task.Run(async () => 
            {
                try
                {
                    await (task?.Invoke()).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                Interlocked.Decrement(ref Current);
            });
        }

        private bool DequeueTask()
        {
            if (Current >= PoolSize) return true; // Still needed to check.

            if (!Queued.TryDequeue(out Func<Task> task)) return false; // Running empty, the scheduler itself can be switched off

            Interlocked.Increment(ref Current);

            // Send off the soon-to-be-active task
            _ = ExecuteTask(task).ConfigureAwait(false);

            return true;
        }

    }
}
