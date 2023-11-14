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

        // Avatar height in cm
        public float AvatarHeight { get; set; } = 175;

        [JsonIgnore]
        public bool IsCustom => AvatarProvider == AvatarProvider.Raw || AvatarProvider == AvatarProvider.Invalid;

        public override bool Equals(object obj) => obj is AvatarDescriptionJSON jSON && Equals(jSON);
        public bool Equals(AvatarDescriptionJSON other) 
            => AvatarURL == other.AvatarURL 
            && AvatarProvider == other.AvatarProvider 
            && AvatarHeight == other.AvatarHeight;
        public override int GetHashCode() => HashCode.Combine(AvatarURL, AvatarProvider, AvatarHeight);

        public static bool operator ==(AvatarDescriptionJSON left, AvatarDescriptionJSON right) => left.Equals(right);
        public static bool operator !=(AvatarDescriptionJSON left, AvatarDescriptionJSON right) => !(left == right);
    }

    public class ClientAudioSettingsJSON
    {
        // Master Volume, 0 to 100
        public virtual float MasterVolume { get; set; } = 100;

        // Voice volume, 0 to 100
        public virtual float VoiceVolume { get; set; } = 100;

        // Environment volume, 0 to 100
        public virtual float EnvVolume { get; set; } = 100;

        // Mic Input Device, null means system default device
        public virtual string InputDevice { get; set; } = null;

        // Mic Input Gain in dB, -6 to +6, meaning factor 0.5 to 2
        public virtual float MicInputGain { get; set; } = 0;

        // Automatic Gain Control level, from none to high
        public virtual int AGCLevel { get; set; } = 0;
    }

    public class SocialListEntryJSON
    {
        // The user's ID, global for friends, scoped otherwise
        public virtual UserID UserID { get; set; } = null;

        // ORed bits from Social.SocialState
        public virtual ulong State { get; set; } = SocialState.None;
    }

    public class ControlSettingsJSON
    {
        public virtual VKUsage VK_Usage { get; set; } = VKUsage.VROnly;

        public virtual VKLayout VK_Layout { get; set; } = VKLayout.de_DE_full;

        public virtual float NameplateIn { get; set; } = 0.5f;

        public virtual float NameplateOut { get; set; } = 4.0f;

        public virtual bool Controller_left { get; set; } = true;

        public virtual bool Controller_right { get; set; } = true;

        public virtual bool Controller_active_left { get; set; } = true;

        public virtual bool Controller_active_right { get; set; } = true;

        public virtual RayType Controller_Type_left { get; set; } = RayType.Straight;

        public virtual RayType Controller_Type_right { get; set; } = RayType.Straight;
    }

    public class MovementSettingsJSON
    {
        public virtual TurnType Turn { get; set; } = TurnType.Snap45;

        public virtual float SmoothTurnSpeed { get; set; } = 60.0f;

        public virtual TeleportType Teleport { get; set; } = TeleportType.Instant;

        public virtual float ZipLineDuration { get; set; } = 1.0f;

        public virtual ComfortBlindersType ComfortBlinders { get; set; } = ComfortBlindersType.Off;
    }

    public class UserDataSettingsJSON
    {
        [JsonIgnore]
        private bool includeCompleteKey = false;

        [JsonIgnore]
        private byte[] userKey = null;

        [JsonIgnore]
        public bool IncludeCompleteKey
        {
            get => includeCompleteKey;
            set => includeCompleteKey = value;
        }

        // The server's COMPLETE key
        public byte[] UserKey
        {
            // Require explicit enabling the export of the whole key to prevent leaking
            // the key with the server settings
            get => includeCompleteKey ? userKey : null;
            set => userKey = value;
        }

        // The display name of the user. Generate if null
        public virtual string Nickname { get; set; } = null;

        // The user's login data
        public virtual LoginDataJSON Login { get; set; } = new();

        // Current avatar
        public virtual AvatarDescriptionJSON CurrentAvatar { get; set; } = new() 
        {
            AvatarProvider = AvatarProvider.RPM,
            AvatarURL = "6394c1e69ef842b3a5112221",
            AvatarHeight = 175
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

        public virtual bool Seconds { get; set; } = false;
    }

    public class ClientSettingsJSON
    {
        // More personal data
        public virtual UserDataSettingsJSON Me { get; set; } = new();

        // Guides the online and availability state
        // public virtual Visibility Visibility { get; set; } = Visibility.Online;

        // VR mode, if available
        public virtual bool VRMode { get; set; } = true;

        // Friends bubble size
        public virtual float SizeBubbleFriends { get; set; } = 1.0f;

        // Strangers bubble size
        public virtual float SizeBubbleStrangers { get; set; } = 1.0f;

        // Privacy settings
        public virtual UserPrivacy UserPrivacy { get; set; } = new();

        // The audio settings
        public virtual ClientAudioSettingsJSON AudioSettings { get; set; } = new();

        // The content filter preferences for sorting the servers
        public virtual ServerPermissions ContentFilterPreferences { get; set; } = new();

        // The controls settings
        public virtual ControlSettingsJSON Controls { get; set; } = new();

        // The movement settings
        public virtual MovementSettingsJSON Movement { get; set; } = new();

        public virtual UserHUDSettingsJSON UserHUD { get; set; } = new();

        // The world collection
        public virtual List<string> WorldList { get; set; } = new();

        // The server collection
        public virtual List<string> ServerList { get; set; } = new();

        // Server keys we've encountered (host ip/name => Public Key)
        public virtual Dictionary<string, byte[]> ServerKeys { get; set; } = new();

        // The text message templates
        public virtual List<string> PresetStrings { get; set; } = new();
    }

    public class Client : ClientSettingsJSON
    {
        #region Change events

        public const string PATH_CLIENT_SETTINGS = "UserSettings.json";

        public event Action<string, float> OnAvatarChanged;
        public event Action<bool> OnVRModeChanged;
        public event Action<float, float> OnPrivacyBubbleChanged;
        public event Action<ControlSettingsJSON, MovementSettingsJSON, ServerPermissions> OnXRControllerChanged;
        public event Action<UserHUDSettingsJSON> OnUserHUDSettingsChanged;
        public event Action<UserPrivacy> OnUserPrivacyChanged;

        [JsonIgnore]
        public string AvatarURL
        {
            get => Me.CurrentAvatar.AvatarURL;
            set 
            {
                string old = Me.CurrentAvatar.AvatarURL;
                Me.CurrentAvatar.AvatarURL = value;
                if(old != Me.CurrentAvatar.AvatarURL) OnAvatarChanged?.Invoke(Me.CurrentAvatar.AvatarURL, Me.CurrentAvatar.AvatarHeight);
            }
        }

        [JsonIgnore]
        public float AvatarHeight
        {
            get => Me.CurrentAvatar.AvatarHeight;
            set
            {
                float old = Me.CurrentAvatar.AvatarHeight;
                Me.CurrentAvatar.AvatarHeight = value;
                if (old != Me.CurrentAvatar.AvatarHeight) OnAvatarChanged?.Invoke(Me.CurrentAvatar.AvatarURL, Me.CurrentAvatar.AvatarHeight);
            }
        }
        [JsonIgnore]
        private Crypto Crypto = null;

        [JsonIgnore]
        public byte[] UserPublicKey => Crypto.PublicKey;

        public override bool VRMode
        {
            get => !Utils.Unity_Server && base.VRMode;
            set {
                bool old = VRMode;
                base.VRMode = value;
                if(old != VRMode) OnVRModeChanged?.Invoke(VRMode);
            }
        }

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

        public override UserPrivacy UserPrivacy
        {
            get => base.UserPrivacy;
            set
            {
                base.UserPrivacy = value;
                OnUserPrivacyChanged?.Invoke(value);
            }
        }

        public void PingXRControllersChanged() => OnXRControllerChanged?.Invoke(Controls, Movement, ContentFilterPreferences);

        public void PingUserHUDChanged() => OnUserHUDSettingsChanged?.Invoke(UserHUD);

        public void PingUserPrivacyChanged() => OnUserPrivacyChanged?.Invoke(UserPrivacy);

        public void Decrypt<T>(CryptPacket p, out T payload) => Crypto.Decrypt(p, out payload);

        public void Sign(byte[] data, out byte[] signature) => Crypto.Sign(data, out signature);

        public static void TransmitMessage<T>(T data, byte[][] receiverPublicKeys, out CMSPacket packet)
            => SettingsManager.Client.Crypto.TransmitMessage(data, receiverPublicKeys, out packet);

        public static void TransmitMessage<T>(T data, byte[] receiverPublicKey, out CMSPacket packet)
            => SettingsManager.Client.Crypto.TransmitMessage(data, receiverPublicKey, out packet);

        public static void ReceiveMessage<T>(CMSPacket packet, ref byte[] expectedSignatureKey, out T data)
            => SettingsManager.Client.Crypto.ReceiveMessage(packet, ref expectedSignatureKey, out data);

        public string GetFingerprint(string fmt = null) => Crypto.ToString(fmt);

        #endregion
        // ---------------------------------------------------------------
        #region Social States

        public void SaveSocialStates(UserID userID, ulong state)
        {
            // It _should_ be zero or exactly one entries to update
            SocialListEntryJSON[] q = GetSocialList(userID).ToArray();

            for(int i = 0; i < q.Length; ++i) Me.SocialList.Remove(q[i]);

            if(state != SocialState.None)
            {
                Me.SocialList.Add(new()
                {
                    UserID = userID,
                    State = state
                });
            }

            Save();
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

        public void UpdateSocialListEntry(UserID userID, Func<ulong, ulong> modification)
        {
            IEnumerable<SocialListEntryJSON> q = GetSocialList(userID);
            ulong state = (q.Count() > 0) ? q.First().State : SocialState.None;

            state = modification(state);

            SaveSocialStates(userID, state);
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

            return dirty;
        }

        /// <summary>
        /// Get the filtered relations list, resticted to the current server, or the global UserIDs
        /// </summary>
        /// <param name="userID">The userID</param>
        /// <returns>The scoped or even the global UserID's entry</returns>
        [Obsolete]
        public IEnumerable<SocialListEntryJSON> GetFilteredSocialList(UserID userID = null) => GetSocialList(userID, (x) => true);

        public void Save()
        {
            using Guard guard = new(
                () => Me.IncludeCompleteKey = true,
                () => Me.IncludeCompleteKey = false);

            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                FileUtils.WriteTextConfig(PATH_CLIENT_SETTINGS, json);
            }
            catch(Exception e)
            {
                Debug.LogWarning($"Failed to save user settings: {e.Message}");
            }
        }

        public static Client Load()
        {
            Client cs;

            try
            {
                string json = FileUtils.ReadTextConfig(PATH_CLIENT_SETTINGS);
                cs = JsonConvert.DeserializeObject<Client>(json);
            }
            catch(Exception e)
            {
                Debug.LogWarning($"Failed to load user settings: {e.Message}");
                cs = new();
            }

            using(Guard guard = new(
                () => cs.Me.IncludeCompleteKey = true,
                () => cs.Me.IncludeCompleteKey = false))
            {

                try
                {
                    if(cs.Me.UserKey == null)
                    {
                        cs.Crypto = new();
                        cs.Me.UserKey = cs.Crypto.Export(true);

                        cs.Save();
                    }
                    else
                    {
                        cs.Crypto = new(cs.Me.UserKey);
                    }
                }
                catch(Exception e)
                {
                    Debug.LogError($"Internal error while regenerating user key - regenerating...: {e.Message}");
                    cs.Crypto = new();
                    cs.Me.UserKey = cs.Crypto.Export(true);

                    cs.Save();
                }
                // Postprocessing to generate the derived values
                if(cs.RefreshAuthentication())
                    // Save the settings back if the randomized guest login occurs and the login token is deleted.
                    cs.Save();
            }

            return cs;
        }

        #endregion
    }
}