/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;

namespace Arteranos.Core
{
    public class ServerSettingsJSON
    {
        // The main server listen port.
        public int ServerPort = 9777;

        // The voice server listen port.
        public int VoicePort = 9778;

        // The server metadata retrieval port.
        public int MetadataPort = 9779;

        // Server listen address. Empty means allowing connections from anywhere.
        public string ListenAddress = string.Empty;

        // Allow viewing avatars in the server mode like in a spectator mode.
        public bool ShowAvatars = true;

        // Allow avatars from a URL outside of the avatar generator's scope.
        public bool AllowCustomAvatars = false;

        // Allow flying
        public bool AllowFlying = false;

        // Allow connections of unverified users
        public bool AllowGuests = true;

        // The server nickname.
        public string Name = string.Empty;

        // The short server description.
        public string Description = string.Empty;

        // The server icon. PNG file bytes, at least 128x128, at most 512x512
        public byte[] Icon = new byte[] { };
    }

    public class ServerSettings : ServerSettingsJSON
    {
        public event Action<string> OnWorldURLChanged;

        // The world URL to load
        [JsonIgnore]
        private string m_WorldURL = string.Empty;

        [JsonIgnore]
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


        public const string PATH_SERVER_SETTINGS = "ServerSettings.json";

        public void SaveSettings()
        {
            try
            {
                string json = ExportSettings();
                File.WriteAllText($"{Application.persistentDataPath}/{PATH_SERVER_SETTINGS}", json);
            }
            catch(Exception e)
            {
                Debug.LogWarning($"Failed to save server settings: {e.Message}");
            }
        }

        public string ExportSettings() => JsonConvert.SerializeObject(this, Formatting.Indented);

        public static ServerSettings LoadSettings()
        {
            ServerSettings ss;

            try
            {
                string json = File.ReadAllText($"{Application.persistentDataPath}/{PATH_SERVER_SETTINGS}");
                ss = JsonConvert.DeserializeObject<ServerSettings>(json);
            }
            catch(Exception e)
            {
                Debug.LogWarning($"Failed to load server settings: {e.Message}");
                ss = new();
            }

            return ss;
        }
    }
}