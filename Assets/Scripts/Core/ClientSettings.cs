using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "ClientSettings", menuName = "Scriptable Objects/Application/Client Settings", order = 1)]
    public class ClientSettings : ScriptableObject
    {
        [Tooltip("Server to connect to")]
        public string ServerIP = "127.0.0.1";
    
        [Tooltip("Complete URL or the RPM avatar shorthand")]
        public string AvatarURL {
            get => _AvatarURL;
            set {
                string old = _AvatarURL;
                _AvatarURL = value;
                if(old != _AvatarURL) OnAvatarChanged(old, _AvatarURL);
            }
        }
        public string _AvatarURL = "https://api.readyplayer.me/v1/avatars/6394c1e69ef842b3a5112221.glb";

        [Tooltip("VR enabled by default")]
        public bool VRMode {
            get => _VRMode;
            set {
                bool old = _VRMode;
                _VRMode = value;
                if(old != _VRMode) OnVRModeChanged(old, _VRMode);
            }
        }
        public bool _VRMode = false;

        public delegate void DelStringChanged(string old, string current);
        public delegate void DelBoolChanged(bool old, bool current);

        public DelStringChanged OnAvatarChanged;
        public DelBoolChanged OnVRModeChanged;

        public const string PATH_CLIENT_SETTINGS = "Settings/ClientSettings";

        public static ClientSettings LoadSettings()
        {
            return Resources.Load<ClientSettings>(PATH_CLIENT_SETTINGS);
        }
    }
}