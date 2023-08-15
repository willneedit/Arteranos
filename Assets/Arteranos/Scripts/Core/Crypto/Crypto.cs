/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using AsymmetricKey = Arteranos.Core.CSPRSAKey;

namespace Arteranos.Core
{

    public struct CryptPacket
    {
        public byte[] iv;
        public byte[] encryptedSessionKey;
        public byte[] encryptedMessage;
    }

    public class Crypto : IDisposable
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
    }
}
