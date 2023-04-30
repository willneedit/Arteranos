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