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

    public enum LoginProvider
    {
        Invalid = 0,    // Invalid, is guest or unverified
        Native,         // Native. uses user@ser.ver
        Github,         // Github. uses the login name (eg. 'willneedit')
        Discord,        // Discord. uses the user handle (eg. 'iwontsay#0000')
        Mastodon        // Mastodon. uses the user handle (eg. user@mas.to.don)
    }

    public enum AvatarProvider
    {
        Invalid = 0,    // Invalid, use fallback avatar
        Raw,            // Raw URL to download the avatar model
        RPM,            // Ready Player Me avatar URL or Shortcode
    }

    public enum Visibility
    {
        Invalid = 0,
        Invisible,      // Appear offline
        DND,            // Do Not Disturb - no direct messaging
        AFK,            // Been idle / HMD on standby
        Online          // Ready
    }

    [Serializable]
    public class ClientSettingsJSON
    {
        // The display name of the user. Generate if null
        public string Nickname = null;

        // The user name of the user, valid only for the selected login provider
        // Ask for login or as guest if null.
        public string Username = null;

        // The login provider the user logs in to
        public LoginProvider LoginProvider = LoginProvider.Invalid;

        // The bearer token bestowed during the last login. May use to verify unknown user's details
        public string BearerToken = null;

        // Server to connect to, ask if null
        public string ServerIP = null;

        // World repository URL to load and enter the server with, null if user enters the server as-is
        public string WorldURL = null;

        // Avatar designator, valid only for the selected avatar provider
        public string AvatarURL = "https://api.readyplayer.me/v1/avatars/6394c1e69ef842b3a5112221.glb";

        // Avatar provider to get the user's avatar
        public AvatarProvider AvatarProvider = AvatarProvider.RPM;

        // VR mode, if available
        public bool VRMode = true;

        // Microphone device if available, default if null
        public string MicDeviceName = null;

        // Guides the online and availability state
        public Visibility Visibility = Visibility.Online;
    }


    public class ClientSettings
    {
        private readonly ClientSettingsJSON _s = new();

        public event Action<string, string> OnAvatarChanged;
        public event Action<bool> OnVRModeChanged;

        public string ServerIP
        { get => _s.ServerIP; set => _s.ServerIP = value; }

        public string AvatarURL
        {
            get => _s.AvatarURL;
            set {
                string old = _s.AvatarURL;
                _s.AvatarURL = value;
                if(old != _s.AvatarURL) OnAvatarChanged?.Invoke(old, _s.AvatarURL);
            }
        }

        public bool VRMode
        {
            get => _s.VRMode;
            set {
                bool old = _s.VRMode;
                _s.VRMode = value;
                if(old != _s.VRMode) OnVRModeChanged?.Invoke(_s.VRMode);
            }
        }

        public string MicDeviceName
        { get => _s.MicDeviceName; set => _s.MicDeviceName = value; }

        public const string PATH_CLIENT_SETTINGS = "Settings/ClientSettings";

        public static ClientSettings LoadSettings()
        {
            //return Resources.Load<ClientSettings>(PATH_CLIENT_SETTINGS);
            return new();
        }
    }
}