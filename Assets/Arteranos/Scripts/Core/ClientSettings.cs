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
using System.ComponentModel;
using System.Collections.Generic;
using Arteranos.Social;
using System.Linq;

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

    public class LoginDataJSON
    {
        // The login provider the user logs in to
        public virtual string LoginProvider { get; set; } = null;

        // The bearer token bestowed during the last login. May use to verify unknown user's details
        public virtual string LoginToken { get; set; } = null;

        // The user name of the user, valid only for the selected login provider
        // Has a random name if it's a guest.
        public virtual string Username { get; set; } = null;

        [JsonIgnore]
        public bool IsGuest => string.IsNullOrEmpty(LoginProvider);
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

    public class SocialListEntryJSON
    {
        // The user's nickname, at the time of entry or update
        public string Nickname = null;

        // The user's ID, global for friends, scoped otherwise
        public UserID UserID = null;

        // ORed bits from Social.SocialState
        public int state = Social.SocialState.None;
    }


    public class UserDataSettingsJSON
    {
        // The display name of the user. Generate if null
        public virtual string Nickname { get; set; } = null;

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

        // The user's social state to others
        public virtual List<SocialListEntryJSON> SocialList { get; set; } = new();
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
        public UserID UserID { get; private set; } = null;


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

        public bool RefreshAuthentication()
        {
            bool dirty = false;

            LoginDataJSON l = Me.Login;

            if(l.LoginProvider == null)
            {
                int rnd = UnityEngine.Random.Range(100000000, 999999999);
                l.Username = $"Guest{rnd}";

                if(l.LoginToken != null)
                {
                    l.LoginToken = null;
                    dirty = true;
                }
            }

            UserID = new(l.LoginProvider, l.Username);

            return dirty;
        }

        public void SaveSocialStates(UserID userID, string nickname, int state)
        {
            // It _should_ be zero or exactly one entries to update
            SocialListEntryJSON[] q = GetSocialList(userID).ToArray();

            // If there's a global UserID lurking around, find it.
            SocialListEntryJSON[] globalq = (from entry in q
                                             where entry.UserID.ServerName == null
                                             select entry).ToArray();

            // Despite passing around a scoped UserID, keep the equivalent global UserID.
            UserID enteredUserID = (globalq.Count() > 0) ? globalq[0].UserID : userID;

            for(int i = 0; i < q.Length; ++i) Me.SocialList.Remove(q[i]);

            if(state != SocialState.None)
            {
                Me.SocialList.Add(new()
                {
                    Nickname = nickname,
                    UserID = enteredUserID,
                    state = state
                });
            }

            SaveSettings();
        }

        public void UpdateToGlobalUserID(UserID globalUserID)
        {
            // The global UserID is considered equal to all of the scoped UserIDs, too.
            SocialListEntryJSON[] q = GetSocialList(globalUserID).ToArray();

            if(q.Count() > 0 && q[0].UserID.ServerName == null)
            {
                // No point proceeding.
                Debug.Log($"There already is a global UserID");
                return;
            }

            // That would mean that we have a shiny new global User ID? HOW?
            string nickname = "<unknown>";
            int aggregated = SocialState.None;

            for(int i = 0; i < q.Length; ++i)
            {
                Me.SocialList.Remove(q[i]);
                aggregated |= q[i].state;
                nickname = q[i].Nickname;
            }

            Me.SocialList.Add(new()
            {
                Nickname = nickname,
                UserID = globalUserID,
                state = aggregated
            });

            SaveSettings();
        }

        /// <summary>
        /// Get the social relations list
        /// </summary>
        /// <param name="userID">the targeted user, null if everyone</param>
        /// <param name="p">Additional search limitations</param>
        /// <returns>The matching entries with the equivalent UserIDs</returns>
        public IEnumerable<SocialListEntryJSON> GetSocialList(
            UserID userID = null, Func<SocialListEntryJSON, bool> p = null)
        {
            p ??= (x) => true;

            return from entry in Me.SocialList
                   where (userID == null || entry.UserID == userID) && p(entry)
                   select entry;
        }

        public void UpdateSocialListEntry(UserID userID, int statusBit, bool set, string Nickname = null)
        {
            var q = GetFilteredSocialList(userID);
            int state = SocialState.None;
            if(q.Count() > 1)
            {
                state = q.First().state;
                Nickname ??= q.First().Nickname;
            }
            else
                Nickname ??= "(unknown)";

            if(set)
                state |= statusBit;
            else
                state &= ~statusBit;

            SaveSocialStates(userID, Nickname, state);
        }

        /// <summary>
        /// Get the filtered relations list, resticted to the current server, or the global UserIDs
        /// </summary>
        /// <param name="userID">The userID</param>
        /// <returns>The scoped or even the global UserID's entry</returns>
        public IEnumerable<SocialListEntryJSON> GetFilteredSocialList(UserID userID = null)
        {
            return GetSocialList(userID, (x) => 
                x.UserID.ServerName == null
                || x.UserID.ServerName == SettingsManager.CurrentServer.Name);
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