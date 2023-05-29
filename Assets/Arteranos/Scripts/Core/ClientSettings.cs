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
using System.Collections.Generic;

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

    public class LoginDataJSON : IEquatable<LoginDataJSON>
    {
        // The login provider the user logs in to
        public virtual string LoginProvider { get; set; } = null;

        // The bearer token bestowed during the last login. May use to verify unknown user's details
        public virtual string LoginToken { get; set; } = null;

        [JsonIgnore]
        public bool IsGuest => string.IsNullOrEmpty(LoginProvider);

        public override bool Equals(object obj) => Equals(obj as LoginDataJSON);
        public bool Equals(LoginDataJSON other) => other is not null && LoginProvider == other.LoginProvider && LoginToken == other.LoginToken;
        public override int GetHashCode() => HashCode.Combine(LoginProvider, LoginToken);

        public static bool operator ==(LoginDataJSON left, LoginDataJSON right) => EqualityComparer<LoginDataJSON>.Default.Equals(left, right);
        public static bool operator !=(LoginDataJSON left, LoginDataJSON right) => !(left == right);
    }

    public class AvatarDescriptionJSON : IEquatable<AvatarDescriptionJSON>
    {
        // Avatar designator, valid only for the selected avatar provider
        public string AvatarURL { get; set; }

        // Avatar provider to get the user's avatar
        public AvatarProvider AvatarProvider { get; set; }

        [JsonIgnore]
        public bool IsCustom => AvatarProvider == AvatarProvider.Raw || AvatarProvider == AvatarProvider.Invalid;

        public override bool Equals(object obj) => obj is AvatarDescriptionJSON jSON && Equals(jSON);
        public bool Equals(AvatarDescriptionJSON other) => AvatarURL == other.AvatarURL && AvatarProvider == other.AvatarProvider;
        public override int GetHashCode() => HashCode.Combine(AvatarURL, AvatarProvider);

        public static bool operator ==(AvatarDescriptionJSON left, AvatarDescriptionJSON right) => left.Equals(right);
        public static bool operator !=(AvatarDescriptionJSON left, AvatarDescriptionJSON right) => !(left == right);
    }

    public class ClientAudioSettingsJSON
    {
        // Master Volume, 0 to 100
        public float MasterVolume = 100;

        // Voice volume, 0 to 100
        public float VoiceVolume = 100;

        // Environment volume, 0 to 100
        public float EnvVolume = 100;

        // Mic Input Device, null means system default device
        public string InputDevice = null;

        // Mic Input Gain in dB, -6 to +6, meaning factor 0.5 to 2
        public float MicInputGain = 0;

        // Automatic Gain Control level, from none to high
        public int AGCLevel = 0;
    }

    public class UserDataSettingsJSON
    {
        // The display name of the user. Generate if null
        public virtual string Nickname { get; set; } = null;

        // The user name of the user, valid only for the selected login provider
        // Has a random name if it's a guest.
        public virtual string Username { get; set; } = null;

        // The user's login data
        public virtual LoginDataJSON Login { get; set; } = new();

        // Current avatar
        public virtual AvatarDescriptionJSON CurrentAvatar { get; set; } = new() 
        {
            AvatarProvider = AvatarProvider.RPM,
            AvatarURL = "6394c1e69ef842b3a5112221" 
        };

        // Avatar storage
        public List<AvatarDescriptionJSON> AvatarGallery { get; set; } = new();
    }

    public class ClientSettingsJSON
    {
        // More personal data
        public virtual UserDataSettingsJSON Me { get; set; } = new();

        // Guides the online and availability state
        public virtual Visibility Visibility { get; set; } = Visibility.Online;

        // VR mode, if available
        public virtual bool VRMode { get; set; } = true;

        // The user's audio settings
        public virtual ClientAudioSettingsJSON AudioSettings { get; set; } = new();

        // The user's content filter preferences for sorting the servers
        public virtual ServerPermissionsJSON ContentFilterPreferences { get; set; } = new();

        // The user's world collection
        public virtual List<string> WorldList { get; set; } = new();

        // The user's server collection
        public virtual List<string> ServerList { get; set; } = new();
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

        [JsonIgnore]
        public string AvatarURL
        {
            get => Me.CurrentAvatar.AvatarURL;
            set {
                string old = Me.CurrentAvatar.AvatarURL;
                Me.CurrentAvatar.AvatarURL = value;
                if(old != Me.CurrentAvatar.AvatarURL) OnAvatarChanged?.Invoke(Me.CurrentAvatar.AvatarURL);
            }
        }

#if UNITY_SERVER
        public override bool VRMode => false;
#else
        public override bool VRMode
        {
            get => base.VRMode;
            set {
                bool old = base.VRMode;
                base.VRMode = value;
                if(old != base.VRMode) OnVRModeChanged?.Invoke(base.VRMode);
            }
        }
#endif

        private void ComputeUserHash()
        {
            string source = $"{Me.Login.LoginProvider}_{Me.Username}";

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

            if(Me.Login.LoginProvider == null)
            {
                int rnd = UnityEngine.Random.Range(100000000, 999999999);
                Me.Username = $"Guest{rnd}";
                Me.Login.LoginToken = null;
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