using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "ServerSettings", menuName = "Scriptable Objects/Application/Server Settings", order = 1)]
    public class ServerSettings : ScriptableObject
    {
        [Tooltip("Load and show avatars in the server window")]
        public bool ShowAvatars;

        public const string PATH_SERVER_SETTINGS = "Settings/ServerSettings";

        public static ServerSettings LoadSettings()
        {
            return Resources.Load<ServerSettings>(PATH_SERVER_SETTINGS);
        }
    }
}