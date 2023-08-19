using System;
using System.Collections.Generic;
using System.Linq;

namespace Arteranos.Core
{
    public class UserID : IEquatable<UserID>
    {
        // Has to be there, for serialization.
        public byte[] PublicKey = null;
        public string Nickname = null;

        public UserID()
        {

        }

        public UserID(byte[] PublicKey, string Nickname)
        {
            this.PublicKey = PublicKey;
            this.Nickname = Nickname;
        }

        public bool Equals(UserID other)
        {
            if(other?.PublicKey == null || PublicKey == null) return false;

            return PublicKey.SequenceEqual(other.PublicKey);
        }

        public static implicit operator byte[](UserID userID) => userID.PublicKey;

        public static implicit operator string(UserID userID) => userID.Nickname;

        public override bool Equals(object obj) => Equals(obj as UserID);
        public override int GetHashCode()
        {
            HashCode hc = new();
            foreach(byte b in PublicKey) hc.Add(b);
            return hc.ToHashCode();
        }

        public static bool operator ==(UserID left, UserID right) => EqualityComparer<UserID>.Default.Equals(left, right);
        public static bool operator !=(UserID left, UserID right) => !(left == right);
    }
}