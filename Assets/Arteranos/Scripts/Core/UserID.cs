using System.Collections;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arteranos.Core
{
    /// <summary>
    /// The user ID.
    /// null ServerName means it's global, only for being distributed to friends, to be
    /// recognizable across the metaverse.
    /// non-null ServerName means it's scoped to a specific server.
    /// </summary>
    public class UserID : IEquatable<UserID>
    {
        // Maybe I have to invent a secret server key to fend off identity theft attacks
        // just by copying the server's name?
        //
        // Reminds me of EvE Online: Using chinese letters to ward
        // off undercover enemies flying under the radar by copying
        // their ship's name to confuse allies.
        public string ServerName = null;
        public byte[] Hash = null;

        // Has to be there, for serialization.
        public UserID()
        {

        }

        public UserID(string LoginProvider, string LoginUsername, string ServerName = null)
        {
            this.ServerName = null;
            Hash = ComputeUserHash(LoginProvider, LoginUsername);

            if(ServerName == null) return;

            Hash = DeriveScopedUserHash(Hash, ServerName);
            this.ServerName = ServerName;
        }

        public UserID(byte[] Hash, string ServerName)
        {
            this.Hash = Hash;
            this.ServerName = null;

            if(ServerName == null) return;

            this.Hash = DeriveScopedUserHash(Hash, ServerName);
            this.ServerName = ServerName;
        }

        private byte[] ComputeUserHash(string LoginProvider, string LoginUsername)
        {
            string source = $"{LoginProvider}_{LoginUsername}";

            byte[] bytes = Encoding.UTF8.GetBytes(source);
            using IncrementalHash myHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            myHash.AppendData(bytes);
            return myHash.GetHashAndReset();

            //string hashString = string.Empty;
            //foreach(byte x in hashBytes) { hashString += String.Format("{0:x2}", x);  }
        }

        private byte[] DeriveScopedUserHash(byte[] UserHash, string ServerName)
        {
            if(ServerName == null) return UserHash;

            byte[] bytes = Encoding.UTF8.GetBytes(ServerName);

            using IncrementalHash myHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            myHash.AppendData(UserHash);
            myHash.AppendData(Encoding.UTF8.GetBytes("_ServerNameDerivation_"));
            myHash.AppendData(bytes);
            return myHash.GetHashAndReset();
        }

        public UserID Derive(string ServerName)
        {
            if(this.ServerName != null && this.ServerName != ServerName)
                return null;

            if(this.ServerName == ServerName) return this;

            return new UserID(Hash, ServerName);
        }

        public bool Equals(UserID other)
        {
            if(other == null) return false;

            // If neccessary, derive the hash with the counterpart's Server name.
            byte[] thisHash = (this.ServerName == null)
                ? DeriveScopedUserHash(this.Hash, other.ServerName)
                : this.Hash;

            byte[] otherHash = (other.ServerName == null)
                ? DeriveScopedUserHash(other.Hash, this.ServerName)
                : other.Hash;

            // And, compare.
            return thisHash.SequenceEqual(otherHash);
        }

        public override string ToString()
        {
            string hashString = (ServerName != null) 
                ? $"{ServerName}:" 
                : string.Empty;
            foreach(byte x in Hash) hashString += String.Format("{0:x2}", x);
            return hashString;
        }

        public override bool Equals(object obj) => Equals(obj as UserID);
        public override int GetHashCode()
        {
            HashCode hc = new();
            if(ServerName != null) foreach(char c in ServerName) hc.Add(c);
            foreach(byte b in Hash) hc.Add(b);
            return hc.ToHashCode();
        }

        public static bool operator ==(UserID left, UserID right) => EqualityComparer<UserID>.Default.Equals(left, right);
        public static bool operator !=(UserID left, UserID right) => !(left == right);
    }
}