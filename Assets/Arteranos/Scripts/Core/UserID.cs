using System.Collections;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arteranos.Core
{
    public class UserID : IEquatable<UserID>
    {
        public byte[] Hash = null;

        // Has to be there, for serialization.
        public UserID()
        {

        }

        public UserID(byte[] Hash)
        {
            this.Hash = Hash;
        }

        public bool Equals(UserID other)
        {
            if(other == null) return false;

            return Hash.SequenceEqual(other.Hash);
        }

        public override string ToString()
        {
            string hashString = string.Empty;
            foreach(byte x in Hash) hashString += String.Format("{0:x2}", x);
            return hashString;
        }

        public override bool Equals(object obj) => Equals(obj as UserID);
        public override int GetHashCode()
        {
            HashCode hc = new();
            foreach(byte b in Hash) hc.Add(b);
            return hc.ToHashCode();
        }

        public static bool operator ==(UserID left, UserID right) => EqualityComparer<UserID>.Default.Equals(left, right);
        public static bool operator !=(UserID left, UserID right) => !(left == right);
    }
}