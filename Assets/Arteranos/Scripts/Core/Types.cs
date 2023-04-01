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
using System.Threading.Tasks;

using Cdm.Authentication.Browser;
using Cdm.Authentication.OAuth2;
using Cdm.Authentication.Clients;
using System.Linq;

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
    class Utils
    {
        /// <summary>
        /// Allows to tack on a Description attribute to enum values, e.g. a display name.
        /// </summary>
        /// <param name="enumVal">The particularvalue of the enum set</param>
        /// <returns>The string in the value's description</returns>
        public static string GetEnumDescription(Enum enumVal)
        {
            MemberInfo[] memInfo = enumVal.GetType().GetMember(enumVal.ToString());
            DescriptionAttribute attribute = CustomAttributeExtensions.GetCustomAttribute<DescriptionAttribute>(memInfo[0]);
            return attribute?.Description ?? "<no description>";
        }

    }
}

namespace Arteranos.Auth
{
    public interface ILoginPackage
    {
        public AuthorizationCodeFlow GetAuthorizationCodeFlow();
        public Task<(string, string)> GetUserIDAsync(AuthenticationSession session);
    }

    public class MockServerPackage : ILoginPackage
    {
        public AuthorizationCodeFlow GetAuthorizationCodeFlow()
        {
            AuthorizationCodeFlow.Configuration configMock = new()
            {
                clientId = "my_client_id",
                clientSecret = "my_client_secret",
#if UNITY_IOS && !UNITY_EDITOR
                redirectUri = "com.cdm.myauthapp:/oauth2",
#else
                redirectUri = "http://localhost:8080/myauthapp/oauth2/",
#endif
                scope = "openid email profile"
            };
            return new MockServerAuth(configMock, "http://localhost:8001");
        }
        public async Task<(string, string)> GetUserIDAsync(AuthenticationSession session) => await Task.Run(() => ("MockServerUser", "MSU#0000"));
    }

    public struct PackageListEntry
    {
        public string name;
        public ILoginPackage pack;
    }

}
