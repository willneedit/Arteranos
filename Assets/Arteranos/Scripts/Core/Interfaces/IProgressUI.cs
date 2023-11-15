/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using System;
using System.Threading.Tasks;

namespace Arteranos.UI
{
    public interface IProgressUI
    {
        bool AllowCancel { get; set; }
        AsyncOperationExecutor<Context> Executor { get; set; }
        Context Context { get; set; }

        event Action<Context> Completed;
        event Action<Exception, Context> Faulted;

        Task<(Exception, Context)> RunProgressAsync();
        void SetupAsyncOperations(Func<(AsyncOperationExecutor<Context>, Context)> setupFunc, bool cancelable = true, string tip = null);
    }
}
