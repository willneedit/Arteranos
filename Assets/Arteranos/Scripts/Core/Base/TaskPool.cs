/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Arteranos.Core
{
    public class TaskPool<T>
    {
        internal struct TaskWorker
        {
            public T RefObject;
            public Func<T, Task> Start;
        }

        public event Action RingIdle = null;

        internal readonly ConcurrentQueue<TaskWorker> ToDos = new();
        internal int InProgress = 0;
        internal int MaxConcurrent = 5;
        internal bool Idle = true;

        public TaskPool(int maxConcurrent = 5, Action RingIdle = null) 
        {
            MaxConcurrent = maxConcurrent;
            if (RingIdle != null) this.RingIdle += RingIdle;
        }

        public void Schedule(T refObject, Func<T, Task> start)
        {
            TaskWorker worker = new()
            {
                RefObject = refObject,
                Start = start,
            };
            ToDos.Enqueue(worker);
        }

        internal async void StartWithCallback(TaskWorker worker)
        {
            await worker.Start(worker.RefObject);
            Interlocked.Decrement(ref InProgress);
        }

        public Task Run()
        {
            if (!Idle) return null;
            Idle = false;
            return GetToWorkAsync();
        }

        internal async Task GetToWorkAsync()
        {

            while(InProgress > 0 || ToDos.Count > 0)
            {
                while(InProgress < MaxConcurrent)
                {
                    if (ToDos.TryDequeue(out TaskWorker worker))
                    {
                        Interlocked.Increment(ref InProgress);
                        StartWithCallback(worker);
                    }
                    else break;
                }

                await Task.Yield();
            }

            Idle = true;
            RingIdle?.Invoke();
        }
    }
}