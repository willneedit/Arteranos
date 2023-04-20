/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using UnityEngine;

using Newtonsoft.Json;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel;

namespace Arteranos.Core
{

    //public enum LoginProvider
    //{
    //    [Description("Guest")]
    //    Guest = 0,      // Invalid, is guest
    //    [Description("Arteranos shard")]
    //    Native,         // Native. uses user@ser.ver
    //    [Description("Google")]
    //    Google,         // Google, uses user@gmail.com
    //    [Description("Github")]
    //    Github,         // Github. uses the login name (eg. 'willneedit')
    //    [Description("Discord")]
    //    Discord,        // Discord. uses the user handle (eg. 'iwontsay#0000')
    //    [Description("Mastodon")]
    //    Mastodon        // Mastodon. uses the user handle (eg. user@mas.to.don)
    //}

    public enum AvatarProvider
    {
        Invalid = 0,    // Invalid, use fallback avatar
        [Description("Raw Avatar")]
        Raw,            // Raw URL to download the avatar model
        [Description("Ready Player Me")]
        RPM,            // Ready Player Me avatar URL or Shortcode
    }

    public enum Visibility
    {
        Invalid = 0,
        [Description("Invisible")]
        Invisible,      // Appear offline
        [Description("Do Not Disturb")]
        DND,            // Do Not Disturb - no direct messaging
        [Description("Away")]
        AFK,            // Been idle / HMD on standby
        [Description("Online")]
        Online          // Ready
    }

    public class ClientSettingsJSON
    {
        // The display name of the user. Generate if null
        public virtual string Nickname { get; set; } = null;

        // The user name of the user, valid only for the selected login provider
        // Has a random name if it's a guest.
        public virtual string Username { get; set; } = null;

        // The login provider the user logs in to
        public virtual string LoginProvider { get; set; } = null;

        // The bearer token bestowed during the last login. May use to verify unknown user's details
        public virtual string BearerToken { get; set; } = null;

        // Server to connect to, ask if null
        public virtual string ServerIP { get; set; } = null;

        // World repository URL to load and enter the server with, null if user enters the server as-is
        public virtual string WorldURL { get; set; } = null;

        // Avatar designator, valid only for the selected avatar provider
        public virtual string AvatarURL { get; set; } = "https://api.readyplayer.me/v1/avatars/6394c1e69ef842b3a5112221.glb";

        // Avatar provider to get the user's avatar
        public virtual AvatarProvider AvatarProvider { get; set; } = AvatarProvider.RPM;

        // VR mode, if available
        public virtual bool VRMode { get; set; } = true;

        // Microphone device if available, default if null
        public virtual string MicDeviceName { get; set; } = null;

        // Guides the online and availability state
        public virtual Visibility Visibility { get; set; } = Visibility.Online;
    }

    public class ClientSettings : ClientSettingsJSON
    {
        public const string PATH_CLIENT_SETTINGS = "UserSettings.json";

        public event Action<string> OnAvatarChanged;
        public event Action<bool> OnVRModeChanged;

        [JsonIgnore]
        public byte[] UserHash { get; internal set; } = null;

        [JsonIgnore]
        public string UserID
        {
            get
            {
                string hashString = string.Empty;
                foreach(byte x in UserHash) hashString += String.Format("{0:x2}", x);
                return hashString;
            }
        }

        public override string AvatarURL
        {
            get => base.AvatarURL;
            set {
                string old = base.AvatarURL;
                base.AvatarURL = value;
                if(old != base.AvatarURL) OnAvatarChanged?.Invoke(base.AvatarURL);
            }
        }

        public override bool VRMode
        {
            get => base.VRMode;
            set {
                bool old = base.VRMode;
                base.VRMode = value;
                if(old != base.VRMode) OnVRModeChanged?.Invoke(base.VRMode);
            }
        }

        private void ComputeUserHash()
        {
            string source = $"{LoginProvider}_{Username}";

            byte[] bytes = Encoding.UTF8.GetBytes(source);
            SHA256Managed hash = new();
            byte[] hashBytes = hash.ComputeHash(bytes);
            //string hashString = string.Empty;
            //foreach(byte x in hashBytes) { hashString += String.Format("{0:x2}", x);  }
            UserHash = hashBytes;
        }

        public bool RefreshAuthentication()
        {
            bool dirty = false;

            if(LoginProvider == null)
            {
                int rnd = UnityEngine.Random.Range(100000000, 999999999);
                Username = $"Guest{rnd}";
                BearerToken = null;
                dirty = true;
            }

            ComputeUserHash();

            return dirty;
        }

        public void SaveSettings()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText($"{Application.persistentDataPath}/{PATH_CLIENT_SETTINGS}", json);
            }
            catch(Exception e)
            {
                Debug.LogWarning($"Failed to save user settings: {e.Message}");
            }
        }

        public static ClientSettings LoadSettings()
        {
            ClientSettings cs;

            try
            {
                string json = File.ReadAllText($"{Application.persistentDataPath}/{PATH_CLIENT_SETTINGS}");
                cs = JsonConvert.DeserializeObject<ClientSettings>(json);
            }
            catch(Exception e)
            {
                Debug.LogWarning($"Failed to load user settings: {e.Message}");
                cs = new();
            }

            // Postprocessing to generate the derived values
            if(cs.RefreshAuthentication())
                // Save the settings back if the randomized guest login occurs and the login token is deleted.
                cs.SaveSettings();
            return cs;
        }
    }
}