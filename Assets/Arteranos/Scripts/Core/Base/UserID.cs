﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arteranos.Core
{
    [ProtoContract]
    public class UserID : IEquatable<UserID>
    {
        // Has to be there, for serialization.
        [ProtoMember(1)]
        public byte[] SignPublicKey = null;

        [ProtoMember(2)]
        public string Nickname = null;

        public UserID()
        {

        }

        public UserID(byte[] SignPublicKey, string Nickname)
        {
            this.SignPublicKey = SignPublicKey;
            this.Nickname = Nickname;
        }

        public bool Equals(UserID other)
        {
            if(other?.SignPublicKey == null || SignPublicKey == null) return false;

            return SignPublicKey.SequenceEqual(other.SignPublicKey);
        }

        public static implicit operator byte[](UserID userID) => userID?.SignPublicKey;

        public static implicit operator string(UserID userID) => userID?.Nickname;

        public override bool Equals(object obj) => Equals(obj as UserID);
        public override int GetHashCode()
        {
            HashCode hc = new();
            foreach(byte b in SignPublicKey) hc.Add(b);
            return hc.ToHashCode();
        }

        public static bool operator ==(UserID left, UserID right) => EqualityComparer<UserID>.Default.Equals(left, right);
        public static bool operator !=(UserID left, UserID right) => !(left == right);
    }
}