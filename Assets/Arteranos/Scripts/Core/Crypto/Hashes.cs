/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Security.Cryptography;
using System.Text;

namespace Arteranos.Core.Cryptography
{
    public static class Hashes
    {
        public static byte[] SHA256(byte[] data)
        {
            using IncrementalHash myHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            myHash.AppendData(data);
            return myHash.GetHashAndReset();
        }

        public static byte[] SHA256(string data)
            => SHA256(Encoding.UTF8.GetBytes(data));
    }
}