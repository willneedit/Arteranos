/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using UnityEngine;

namespace Arteranos.Core
{
    [CreateAssetMenu(fileName = "ServerSettings", menuName = "Scriptable Objects/Application/Server Settings", order = 1)]
    public class ServerSettings : ScriptableObject
    {
        public event Action<string> OnWorldURLChanged;

        [Tooltip("Load and show avatars in the server window")]
        public bool ShowAvatars = true;

        [Tooltip("Address to listen on")]
        public string ListenAddress = string.Empty;

        [Tooltip("Guests allowed?")]
        public bool AllowGuests = true;

        [Tooltip("Custom/Homebaked avatars allowed?")]
        public bool AllowCustomAvatars = false;

        [Tooltip("Allow flying?")]
        public bool AllowFlying = false;

        // The world URL to load
        [NonSerialized]
        private string m_WorldURL = string.Empty;

        public string WorldURL
        {
            get => m_WorldURL;
            set
            {
                string old = m_WorldURL;
                m_WorldURL = value;
                if(old != m_WorldURL) OnWorldURLChanged?.Invoke(m_WorldURL);
            }
        }


        public const string PATH_SERVER_SETTINGS = "Settings/ServerSettings";

        public static ServerSettings LoadSettings() => Resources.Load<ServerSettings>(PATH_SERVER_SETTINGS);
    }
}