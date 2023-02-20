/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using System;

using Mirror;

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

    public static class ExtendAudioSource
    {
        // FIXME Not sure to use the time measurement, not the PCM count, but they're
        //       not available in the current clip, unless I use GetData()... 

        /// <summary>
        /// Returns the normalized play position of the source's current clip.
        /// </summary>
        /// <param name="audioSource">The audio source in question</param>
        /// <returns>The position in the current clip, ranged in [0..1].</returns>
        public static float Position(this AudioSource audioSource) 
            => audioSource.time / audioSource.clip.length;
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
