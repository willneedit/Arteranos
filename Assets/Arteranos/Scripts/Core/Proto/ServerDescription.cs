/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arteranos.Core.Cryptography;
using Ipfs;
using Ipfs.Core.Cryptography.Proto;
using ProtoBuf;

namespace Arteranos.Core
{
    [ProtoContract]
    public partial class _ServerDescription : IEquatable<_ServerDescription>
    {
        [ProtoMember(1)]
        public string Name;

        [ProtoMember(2)]
        public int ServerPort;

        [ProtoMember(3)]
        public int MetadataPort;

        [ProtoMember(4)]
        public string Description;

        [ProtoMember(5)]
        public byte[] Icon;

        [ProtoMember(7)]
        public string Version;

        [ProtoMember(8)]
        public string MinVersion;

        [ProtoMember(9)]
        public ServerPermissions Permissions;

        [ProtoMember(10)]
        public string PrivacyTOSNotice;

        [ProtoMember(11)]
        public string[] AdminNames;

        [ProtoMember(12)]
        public byte[] signature;

        public void Serialize(SignKey serverPrivateKey, Stream stream)
        {
            // Sign the structure with the empty signature field
            using (MemoryStream ms = new())
            {
                signature = null;
                Serializer.Serialize(ms, this);
                ms.Position = 0;
                serverPrivateKey.Sign(ms.ToArray(), out signature);
            }

            Serializer.Serialize(stream, this);
            stream.Flush();
        }

        public static _ServerDescription Deserialize(PublicKey serverPublicKey, Stream stream)
        {   
            _ServerDescription d = Serializer.Deserialize<_ServerDescription>(stream);
            byte[] signature = d.signature;
            using (MemoryStream ms = new())
            {
                d.signature = null;
                Serializer.Serialize(ms, d);
                ms.Position = 0;
                serverPublicKey.Verify(ms.ToArray(), signature);
            }

            return d;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as _ServerDescription);
        }

        public bool Equals(_ServerDescription other)
        {
            return other is not null &&
                   Name == other.Name &&
                   ServerPort == other.ServerPort &&
                   MetadataPort == other.MetadataPort &&
                   Description == other.Description &&
                   EqualityComparer<byte[]>.Default.Equals(Icon, other.Icon) &&
                   Version == other.Version &&
                   MinVersion == other.MinVersion &&
                   EqualityComparer<ServerPermissions>.Default.Equals(Permissions, other.Permissions) &&
                   PrivacyTOSNotice == other.PrivacyTOSNotice &&
                   AdminNames.SequenceEqual(other.AdminNames);
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(Name);
            hash.Add(ServerPort);
            hash.Add(MetadataPort);
            hash.Add(Description);
            hash.Add(Icon);
            hash.Add(Version);
            hash.Add(MinVersion);
            hash.Add(Permissions);
            hash.Add(PrivacyTOSNotice);
            hash.Add(AdminNames);
            return hash.ToHashCode();
        }

        public static bool operator ==(_ServerDescription left, _ServerDescription right)
        {
            return EqualityComparer<_ServerDescription>.Default.Equals(left, right);
        }

        public static bool operator !=(_ServerDescription left, _ServerDescription right)
        {
            return !(left == right);
        }
    }
}