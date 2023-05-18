/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Arteranos.Web
{
    public interface IConnectionManager
    {
        bool CanDoConnect();
        bool CanGetConnected();
        Task<bool> ConnectToServer(string serverURL);
        void StartHost();
        void StartServer();
        void StopHost();
    }
}

namespace Arteranos.UI
{
    // -------------------------------------------------------------------
    #region UI interfaces
    public interface IDialogUI
    {
        public string Text { get; set; }
        public string[] Buttons { get; set; }
        public void Close();

        event Action<int> OnDialogDone;

        Task<int> PerformDialogAsync(string text, string[] buttons);
    }

    public interface IProgressUI
    {
        bool AllowCancel { get; set; }
        AsyncOperationExecutor<Context> Executor { get; set; }
        Context Context { get; set; }

        event Action<Context> Completed;
        event Action<Exception, Context> Faulted;
    }
    #endregion
    // -------------------------------------------------------------------
    #region UI factories
    public class DialogUIFactory : UIBehaviour
    {
        public static IDialogUI New()
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/UI_Dialog"));
            return go.GetComponent<IDialogUI>();
        }

    }

    public class ProgressUIFactory : UIBehaviour
    {

        public static IProgressUI New()
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/UI_Progress"));
            return go.GetComponent<IProgressUI>();
        }
    }
    #endregion
    // -------------------------------------------------------------------

}