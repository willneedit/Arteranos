/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

using AsymmetricKey = Arteranos.Core.CSPRSAKey;

namespace Arteranos.Core
{
    // Needed for the distictiveness of the legacy key blobs in the future
    // wincrypt.h, BLOBHEADER
    internal struct CspKeyBlobHeader
    {
        public byte type;       // 0: 0x6 or 0x7 (public or private keys)
        public byte version;    // 1: always 2 (in this scope)
        public ushort reserved; // 2: always 0
        public uint keyAlg;     // 4: ALGID_KEY_SIGN (0xa4)
        public ulong magic;     // 8: "RSA1" or "RSA2"
                                // 12: 
    }

    internal struct OurKeyBlobHeader
    {
        public byte type;       // 0: Same as with CspKeyBlobHeader
        public byte version;    // 1: 0x80 (or higher - unsigned)
        public KeyType keyType; // 2: As seen in Interfaces.cs
                                // 4: 
    }

    public struct CryptPacket
    {
        public byte[] iv;
        public byte[] encryptedSessionKey;
        public byte[] encryptedMessage;
    }

    public static class FPPresenter
    {

    }

    public class Crypto : IDisposable, IEquatable<Crypto>
    {
        public byte[] PublicKey => Key.PublicKey;

        private readonly AsymmetricKey Key;

        public Crypto()
        {
            Key = new();
        }

        public Crypto(byte[] rsaKeyBlob)
        {
            Key = new(rsaKeyBlob);
        }

        public byte[] Export(bool includePrivateParameters)
            => includePrivateParameters ? Key.ExportPrivateKey() : Key.PublicKey;

        #region Public Key Fingerprint

        public byte[] Fingerprint { get => CryptoHelpers.GetFingerprint(Key.PublicKey); }
        public override string ToString() => ToString(CryptoHelpers.FP_SHA256);
        public string ToString(string v) => CryptoHelpers.ToString(v, Key.PublicKey);

        #endregion

        #region Encrypt and decrypt

        public static void Encrypt(byte[] payload, byte[] otherPublicKey, out CryptPacket p)
        {
            using Aes aes = new AesCryptoServiceProvider();
            p.iv = aes.IV;

            using AsymmetricKey otherKey = new(otherPublicKey);
            otherKey.WrapKey(aes.Key, out p.encryptedSessionKey);

            using MemoryStream ciphertext = new();
            using CryptoStream cs = new(ciphertext, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(payload, 0, payload.Length);
            cs.Close();

            p.encryptedMessage = ciphertext.ToArray();
        }

        public void Decrypt(CryptPacket p, out byte[] payload)
        {

            using Aes aes = new AesCryptoServiceProvider();
            aes.IV = p.iv;

            Key.UnwrapKey(p.encryptedSessionKey, out byte[] aesKey);
            aes.Key = aesKey;

            using MemoryStream plaintext = new();
            using CryptoStream cs = new(plaintext, aes.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(p.encryptedMessage, 0, p.encryptedMessage.Length);
            cs.Close();

            payload = plaintext.ToArray();
        }

        public static void Encrypt<T>(T payload, byte[] otherPublicKey, out CryptPacket p) 
            => Encrypt(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload)), otherPublicKey, out p);

        public void Decrypt<T>(CryptPacket p, out T payload)
        {
            Decrypt(p, out byte[] json);
            payload = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(json));
        }

        #endregion

        #region Sign and verify

        public static bool Verify(byte[] data, byte[] signature, byte[] otherPublicKey)
        {
            using AsymmetricKey otherKey = new(otherPublicKey);

            return otherKey.Verify(data, signature);
        }

        public void Sign(byte[] data, out byte[] signature)
            => Key.Sign(data, out signature);

        public static bool Verify<T>(T data, byte[] signature, byte[] otherPublicKey)
            => Verify(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)), signature, otherPublicKey);

        public void Sign<T>(T data, out byte[] signature)
            => Sign(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)), out signature);

        #endregion

        public void Dispose() => Key.Dispose();
        public override bool Equals(object obj) => Equals(obj as Crypto);
        public bool Equals(Crypto other) => other is not null && PublicKey.SequenceEqual(other.PublicKey);
        public override int GetHashCode() => HashCode.Combine(PublicKey);

        public static bool operator ==(Crypto left, Crypto right) => EqualityComparer<Crypto>.Default.Equals(left, right);
        public static bool operator !=(Crypto left, Crypto right) => !(left == right);
    }
}
