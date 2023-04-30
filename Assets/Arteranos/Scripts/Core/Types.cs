/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using System;

using Mirror;
using System.ComponentModel;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

namespace Arteranos.ExtensionMethods
{
    using Arteranos.NetworkTypes;

    public static class ExtendTransform
    {
        /// <summary>
        /// Finds the transform in the hierarchy tree by name, including searching the
        /// entire subtree below.
        /// </summary>
        /// <param name="t">The transform to begin searching</param>
        /// <param name="name">The transform's name to search for</param>
        /// <returns>The first found transform, otherwise null</returns>
        public static Transform FindRecursive(this Transform t, string name)
        {
            if(t.name == name) return t;

            for(int i = 0, c = t.childCount; i<c; i++)
            {
                Transform res = FindRecursive(t.GetChild(i), name);
                if(res != null) return res;
            }

            return null;
        }
    }

    public static class ExtendNetworkGuid
    {
        
        public static NetworkGuid ToNetworkGuid(this Guid id)
        {
            NetworkGuid networkId = new()
            {
                FirstHalf = BitConverter.ToUInt64(id.ToByteArray(), 0),
                SecondHalf = BitConverter.ToUInt64(id.ToByteArray(), 0)
            };
            return networkId;
        }

        public static Guid ToGuid(this NetworkGuid networkId)
        {
            byte[] bytes = new byte[16];
            Buffer.BlockCopy(BitConverter.GetBytes(networkId.FirstHalf), 0, bytes, 0, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(networkId.SecondHalf), 0, bytes, 8, 8);
            return new Guid(bytes);
        }

        public static void WriteNetworkGuid(this NetworkWriter writer, NetworkGuid value)
        {
            writer.WriteULong(value.FirstHalf);
            writer.WriteULong(value.SecondHalf);
        }

        public static NetworkGuid ReadNetworkGuid(this NetworkReader reader)
        {
            NetworkGuid res = new()
            {
                FirstHalf = reader.ReadULong(),
                SecondHalf = reader.ReadULong()
            };
            return res;
        }

    }
}

namespace Arteranos.NetworkTypes
{
    public class NetworkGuid 
    {
        public ulong FirstHalf;
        public ulong SecondHalf;

    }
}

namespace Arteranos.Core
{
    public class Utils
    {
        /// <summary>
        /// Allows to tack on a Description attribute to enum values, e.g. a display name.
        /// </summary>
        /// <param name="enumVal">The particular value of the enum set</param>
        /// <returns>The string in the value's description, null if there isn't</returns>
        public static string GetEnumDescription(Enum enumVal)
        {
            MemberInfo[] memInfo = enumVal.GetType().GetMember(enumVal.ToString());
            DescriptionAttribute attribute = CustomAttributeExtensions.GetCustomAttribute<DescriptionAttribute>(memInfo[0]);
            return attribute?.Description;
        }

        /// <summary>
        /// Generate a directory of a hashed URL suitable for a cache directory tree.
        /// </summary>
        /// <param name="url">The URL</param>
        /// <returns>the two directory levels, without a root path</returns>
        public static string GetURLHash(string url)
        {
            Hash128 hash = new();
            byte[] bytes = Encoding.UTF8.GetBytes(url);
            hash.Append(bytes);
            string hashstr = hash.ToString();

            string hashed = $"{hashstr[0..2]}/{hashstr[2..]}";
            return hashed;
        }
    }

    /// <summary>
    /// The static server configuration data.
    /// </summary>
    public class ServerSettingsJSON
    {
        // The main server listen port.
        public int ServerPort = 9777;

        // The voice server listen port.
        public int VoicePort = 9778;

        // The server metadata retrieval port.
        public int MetadataPort = 9779;

        // Server listen address. Empty means allowing connections from anywhere.
        public string ListenAddress = string.Empty;

        // Allow viewing avatars in the server mode like in a spectator mode.
        public bool ShowAvatars = true;

        // Allow avatars from a URL outside of the avatar generator's scope.
        public bool AllowCustomAvatars = false;

        // Allow flying
        public bool AllowFlying = false;

        // Allow connections of unverified users
        public bool AllowGuests = true;

        // The server nickname.
        public string Name = string.Empty;

        // The short server description.
        public string Description = string.Empty;

        // The server icon. PNG file bytes, at least 128x128, at most 512x512
        public byte[] Icon = new byte[] { };
    }


    /// <summary>
    /// Public server meta data with the connection data and the privileges
    /// </summary>
    public class ServerMetadataJSON
    {
        public ServerSettingsJSON Settings = null;
        public string CurrentWorld = null;
        public List<string> CurrentUsers = new();
    }

}