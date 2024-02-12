using Ipfs.Core.Cryptography.Proto;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Arteranos.Core
{
    [ProtoContract]
    public class UserID : IEquatable<UserID>
    {
        // Has to be there, for serialization.
        [ProtoMember(1)]
        public PublicKey SignPublicKey = null;

        [ProtoMember(2)]
        public string Nickname = null;

        public UserID()
        {

        }

        public UserID(PublicKey SignPublicKey, string Nickname)
        {
            this.SignPublicKey = SignPublicKey;
            this.Nickname = Nickname;
        }

        public byte[] Serialize()
        {
            using MemoryStream ms = new();
            Serializer.Serialize(ms, this);
            return ms.ToArray();
        }

        public static UserID Deserialize(byte[] data)
            => Serializer.Deserialize<UserID>(new MemoryStream(data));

        public bool Equals(UserID other)
        {
            if(other?.SignPublicKey == null || SignPublicKey == null) return false;

            return SignPublicKey == other.SignPublicKey;
        }

        public static implicit operator PublicKey(UserID userID) => userID?.SignPublicKey;

        public static implicit operator string(UserID userID) => userID?.Nickname;

        public override bool Equals(object obj) => Equals(obj as UserID);
        public override int GetHashCode()
        {
            HashCode hc = new();
            foreach(byte b in SignPublicKey.Serialize()) hc.Add(b);
            return hc.ToHashCode();
        }

        public static bool operator ==(UserID left, UserID right) => EqualityComparer<UserID>.Default.Equals(left, right);
        public static bool operator !=(UserID left, UserID right) => !(left == right);
    }
}