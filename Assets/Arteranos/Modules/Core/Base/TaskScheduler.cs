/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Arteranos.Core
{
    public class TaskScheduler : MonoBehaviour
    {
        public static TaskScheduler Instance = null;

        public int PoolSize { get; set; } = 10;
        public int DequeuesPerUpdate { get; set; } = 10;

        private readonly ConcurrentQueue<Func<Task>> TaskQueued = new();
        private readonly ConcurrentQueue<Action> CallbackQueued = new();
        private int Current = 0;

        private void Awake() => Instance = this;
        private void OnDestroy() => Instance = null;
        void Update()
        {
            DequeueTask();

            DequeueCallback();
        }

        /// <summary>
        /// Queue a task to be executed in an opportune time.
        /// </summary>
        /// <param name="task"><see langword="async"/>function returning <see cref="Task"/>.</param>
        public static void Schedule(Func<Task> task) => Instance.Schedule_(task);

        /// <summary>
        /// Place a callback action from an async task.
        /// </summary>
        /// <param name="callback">The callback to be placed within the next couple of frames</param>
        public static void ScheduleCallback(Action callback)
        {
            if (Instance) Instance.ScheduleCallback_(callback);
        }

        /// <summary>
        /// Place a coroutine from an async task.
        /// </summary>
        /// <param name="coroutine">The coroutine to be placed within the next couple of frames</param>
        public static void ScheduleCoroutine(Func<IEnumerator> coroutine)
        {
            if (Instance) Instance.ScheduleCoroutine_(coroutine);
        }

        private void Schedule_(Func<Task> task) => TaskQueued.Enqueue(task);
        private void ScheduleCallback_(Action callback) => CallbackQueued.Enqueue(callback);
        private void ScheduleCoroutine_(Func<IEnumerator> coroutine)
        {
            void wrapper(Func<IEnumerator> coroutine) 
                => StartCoroutine(coroutine?.Invoke());

            ScheduleCallback_(() => wrapper(coroutine));
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

        private void DequeueTask()
        {
            for (int i = 0; i < DequeuesPerUpdate; i++)
            {
                if (Current >= PoolSize) return;
                if (!TaskQueued.TryDequeue(out Func<Task> task)) return;

                Interlocked.Increment(ref Current);

                // Send off the soon-to-be-active task
                _ = ExecuteTask(task).ConfigureAwait(false);
            }
        }

        private void DequeueCallback()
        {
            for(int i = 0; i < DequeuesPerUpdate; i++)
            {
                if (!CallbackQueued.TryDequeue(out Action callback)) return;

                try
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    callback?.Invoke();
                    if (sw.ElapsedMilliseconds > 2)
                        Debug.LogWarning($"Callback took more than 2 ms from the main thread!");
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}
