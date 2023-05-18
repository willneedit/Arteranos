/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

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
    public interface IDialogUI
    {
        public string Text { get; set; }
        public string[] Buttons { get; set; }
        public void Close();

        event Action<int> OnDialogDone;

        Task<int> PerformDialogAsync(string text, string[] buttons);
    }
    public class DialogUIFactory : UIBehaviour
    {
        public static IDialogUI New()
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/UI_Dialog"));
            return go.GetComponent<IDialogUI>();
        }

    }
}