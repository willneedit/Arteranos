/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;

namespace Arteranos.Core
{
    [CreateAssetMenu(fileName = "ClientSettings", menuName = "Scriptable Objects/Application/Client Settings", order = 1)]
    public class ClientSettings : ScriptableObject
    {
        [Tooltip("Server to connect to")]
        public string ServerIP = "127.0.0.1";

        [SerializeField]
        [Tooltip("Complete URL or the RPM avatar shorthand")]
        private string _AvatarURL = "https://api.readyplayer.me/v1/avatars/6394c1e69ef842b3a5112221.glb";

        [SerializeField]
        [Tooltip("VR enabled by default")]
        private bool _VRMode = true;

        public d_VarChanged<string> OnAvatarChanged;
        public d_VarChanged<bool> OnVRModeChanged;

        public delegate void d_VarChanged<T>(T old, T current);

        public string AvatarURL {
            get => _AvatarURL;
            set {
                string old = _AvatarURL;
                _AvatarURL = value;
                if(old != _AvatarURL && OnAvatarChanged != null) OnAvatarChanged(old, _AvatarURL);
            }
        }

        public bool VRMode {
            get => _VRMode;
            set {
                bool old = _VRMode;
                _VRMode = value;
                if(old != _VRMode && OnVRModeChanged != null) OnVRModeChanged(old, _VRMode);
            }
        }

        public const string PATH_CLIENT_SETTINGS = "Settings/ClientSettings";

        public static ClientSettings LoadSettings()
        {
            return Resources.Load<ClientSettings>(PATH_CLIENT_SETTINGS);
        }
    }
}