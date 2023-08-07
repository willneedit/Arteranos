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

    public enum VKUsage
    {
        [Description("never")]
        Never = 0,
        [Description("VR only")]
        VROnly,
        [Description("always")]
        Always
    }

    public enum VKLayout
    {
        [Description("de-DE (full)")]
        de_DE_full = 0,
        [Description("en-US (full)")]
        en_US_full
    }

    public enum RayType
    {
        [Description("line")]
        Straight = 0,
        [Description("low arc")]
        LowArc,
        [Description("high arc")]
        HighArc
    }

    public enum TurnType
    {
        [Description("Smooth")]
        Smooth = 0,
        [Description("Snap 90°")]
        Snap90,
        [Description("Snap 45°")]
        Snap45,
        [Description("Snap 30°")]
        Snap30,
        [Description("Snap 22.5°")]
        Snap225
    }

    public enum TeleportType
    {
        [Description("Instant")]
        Instant = 0,
        [Description("Blink")]
        Blink,
        [Description("Zipline")]
        Zipline
    }

    public enum ComfortBlindersType
    {
        [Description("Off")]
        Off = 0,
        [Description("Low")]
        Low,
        [Description("Medium")]
        Medium,
        [Description("High")]
        High,
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
        public int state = SocialState.None;
    }

    public class ControlSettingsJSON
    {
        public VKUsage VK_Usage = VKUsage.VROnly;

        public VKLayout VK_Layout = VKLayout.de_DE_full;

        public float NameplateIn = 0.5f;

        public float NameplateOut = 4.0f;

        public bool controller_left = true;

        public bool controller_right = true;

        public bool controller_active_left = true;

        public bool controller_active_right = true;

        public RayType controller_Type_left = RayType.Straight;

        public RayType controller_Type_right = RayType.Straight;
    }

    public class MovementSettingsJSON
    {
        public bool Flying = false;

        public TurnType Turn = TurnType.Snap45;

        public float SmoothTurnSpeed = 60.0f;

        public TeleportType Teleport = TeleportType.Instant;

        public float ZipLineDuration = 1.0f;

        public ComfortBlindersType ComfortBlinders = ComfortBlindersType.Off;
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
        public virtual List<AvatarDescriptionJSON> AvatarGallery { get; set; } = new();

        // The user's social state to others
        public virtual List<SocialListEntryJSON> SocialList { get; set; } = new();
    }

    public class UserHUDSettingsJSON
    {
        public virtual float AxisX { get; set; } = -1.5f;   // * 10

        public virtual float AxisY { get; set; } = -1.0f;   // * 10

        public virtual float Log2Size { get; set; } = 0;    // 2^x (-2 ... 2 <=> 0.25 ... 4 )

        public virtual float Tightness { get; set; } = 0.1f;

        public virtual float Delay { get; set; } = 2;

        public virtual int ClockDisplay { get; set; } = 2;  // 0 to 2 should be self explanatory, right?
    }

    public class ClientSettingsJSON
    {
        // More personal data
        public virtual UserDataSettingsJSON Me { get; set; } = new();

        // Guides the online and availability state
        public virtual Visibility Visibility { get; set; } = Visibility.Online;

        // VR mode, if available
        public virtual bool VRMode { get; set; } = true;

        // Friends bubble size
        public virtual float SizeBubbleFriends { get; set; } = 1.0f;

        // Strangers bubble size
        public virtual float SizeBubbleStrangers { get; set; } = 1.0f;

        // The audio settings
        public virtual ClientAudioSettingsJSON AudioSettings { get; set; } = new();

        // The content filter preferences for sorting the servers
        public virtual ServerPermissionsJSON ContentFilterPreferences { get; set; } = new();

        // The controls settings
        public virtual ControlSettingsJSON Controls { get; set; } = new();

        // The movement settings
        public virtual MovementSettingsJSON Movement { get; set; } = new();

        public virtual UserHUDSettingsJSON UserHUD { get; set; } = new();

        // The world collection
        public virtual List<string> WorldList { get; set; } = new();

        // The server collection
        public virtual List<string> ServerList { get; set; } = new();

        // The text message templates
        public virtual List<string> PresetStrings { get; set; } = new();
    }

    public class ClientSettings : ClientSettingsJSON
    {
        #region Change events

        public const string PATH_CLIENT_SETTINGS = "UserSettings.json";

        public event Action<string> OnAvatarChanged;
        public event Action<bool> OnVRModeChanged;
        public event Action<float, float> OnPrivacyBubbleChanged;
        public event Action OnXRControllerChanged;
        public event Action<UserHUDSettingsJSON> OnUserHUDSettingsChanged;

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
        public override float SizeBubbleFriends
        {
            get => base.SizeBubbleFriends;
            set
            {
                base.SizeBubbleFriends = value;
                OnPrivacyBubbleChanged?.Invoke(SizeBubbleFriends, SizeBubbleStrangers);
            }
        }

        public override float SizeBubbleStrangers
        {
            get => base.SizeBubbleStrangers;
            set
            {
                base.SizeBubbleStrangers = value;
                OnPrivacyBubbleChanged?.Invoke(SizeBubbleFriends, SizeBubbleStrangers);
            }
        }

        public void PingXRControllersChanged() => OnXRControllerChanged?.Invoke();

        public void PingUserHUDChanged() => OnUserHUDSettingsChanged?.Invoke(UserHUD);

        #endregion
        // ---------------------------------------------------------------
        #region Social States

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
            IEnumerable<SocialListEntryJSON> q = GetFilteredSocialList(userID);
            int state = SocialState.None;
            if(q.Count() > 0)
            {
                state = q.First().state;
                Nickname ??= q.First().Nickname;
            }
            else
            {
                Nickname ??= "(unknown)";
            }

            if(set)
                state |= statusBit;
            else
                state &= ~statusBit;

            SaveSocialStates(userID, Nickname, state);
        }

        #endregion
        // ---------------------------------------------------------------
        #region Save & Load

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

        /// <summary>
        /// Get the filtered relations list, resticted to the current server, or the global UserIDs
        /// </summary>
        /// <param name="userID">The userID</param>
        /// <returns>The scoped or even the global UserID's entry</returns>
        public IEnumerable<SocialListEntryJSON> GetFilteredSocialList(UserID userID = null)
        {
            return GetSocialList(userID, (x) => 
                x.UserID.ServerName == null
                || x.UserID.ServerName == SettingsManager.CurrentServer?.Name);
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

        #endregion
    }
}