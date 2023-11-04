/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace Arteranos.Core
{

    /// <summary>
    /// Public server meta data with the connection data and the privileges
    /// </summary>
    public class ServerMetadataJSON
    {
        public ServerJSON Settings = null;
        public string CurrentWorld = null;
        public List<byte []> CurrentUsers = new();
    }

    public class WorldMetaData
    {
        public const string PATH_METADATA_DEFAULTS = "MetadataDefaults.json";

        public string WorldName = "Unnamed World";
        public string Author = "Anonymous";
        public DateTime Created = DateTime.MinValue;
        public DateTime Updated = DateTime.MinValue;

        public void SaveDefaults()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText($"{Application.persistentDataPath}/{PATH_METADATA_DEFAULTS}", json);
            }
            catch(Exception ex)
            {
                Debug.LogWarning($"Failed to save the metadata defaults: {ex.Message}");
            }
        }

        public static WorldMetaData LoadDefaults()
        {
            WorldMetaData mdj;
            try
            {
                string json = File.ReadAllText($"{Application.persistentDataPath}/{PATH_METADATA_DEFAULTS}");
                mdj = JsonConvert.DeserializeObject<WorldMetaData>(json);
            }
            catch(Exception ex)
            {
                Debug.LogWarning($"Failed to load the metadata defaults: {ex.Message}");
                mdj = new();
            }

            return mdj;
        }

        public string Serialize() => JsonConvert.SerializeObject(this, Formatting.Indented);

        public static WorldMetaData Deserialize(string json) => JsonConvert.DeserializeObject<WorldMetaData>(json);
    }

    /// <summary>
    /// Suitable for locks/unlocks to properly deallocate resources when the control flow
    /// leaves the scope, be it regular or by an exception.
    /// 
    /// Credits go for C++ :)
    /// Best use with
    ///             using(Guard guard = new(allocate, release)) { ... }
    /// </summary>
    public class Guard : IDisposable
    {
        private readonly Action disengage;

        private bool _disposedValue;

        public Guard(Action engage, Action disengage)
        {
            this.disengage = disengage;
            engage();
        }

        ~Guard() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(_disposedValue) return;

            //if(disposing)
            //{
            //    // Needed? Dispose managed state (managed objects).
            //}

            disengage();
            _disposedValue = true;
        }
    }
}
