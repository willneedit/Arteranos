/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using Newtonsoft.Json;

namespace Arteranos.Core
{
    public class Version
    {
        public const string VERSION_MIN = "0.5.5";

        public string MMP = "0.0.0";    // Major, minor, patch
        public string MMPB = "0.0.0.0"; // Major, minor, patch, buld
        public string B = "0";          // build
        public string Hash = "0000000"; // Abbreviated commit hash
        public string Tag = "";         // Optional tag
        public string Full = "unknown"; // Full version string

        public static Version Load()
        {
            TextAsset ta = Resources.Load<TextAsset>("Version");
            return (ta != null) ? JsonConvert.DeserializeObject<Version>(ta.text) : null;
        }

        /// <summary>
        /// Checks if this version is more recent or equal to the argument.
        /// </summary>
        /// <param name="version">The requested version</param>
        /// <returns>True if 'this' (the loaded) version is greater or equal</returns>
        public bool IsGE(string version)
        {
            string[] parts = MMPB.Split('.');
            string[] reqparts = version.Split('.');

            if(string.Compare(parts[0], reqparts[0]) < 0) return false;
            if(string.Compare(parts[0], reqparts[0]) > 0) return true;

            if(string.Compare(parts[1], reqparts[1]) < 0) return false;
            if(string.Compare(parts[1], reqparts[1]) > 0) return true;

            if(string.Compare(parts[2], reqparts[2]) < 0) return false;
            if(string.Compare(parts[2], reqparts[2]) > 0) return true;

            return true;
        }
    }
}
