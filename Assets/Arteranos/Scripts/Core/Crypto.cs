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
        public byte[] PublicKey => this.publicKey;


        private readonly byte[] publicKey;
        private readonly RSACryptoServiceProvider rsaKey;

        public Crypto()
        {
            rsaKey = new();
            publicKey = rsaKey.ExportCspBlob(false);

        }

        public Crypto(byte[] rsaKeyBlob)
        {
            rsaKey = new();
            rsaKey.ImportCspBlob(rsaKeyBlob);
            publicKey = rsaKey.ExportCspBlob(false);
        }

        public byte[] Export(bool includePrivateParameters)
        {
            if(!includePrivateParameters) return publicKey;

            return rsaKey.ExportCspBlob(true);
        }

        public static void Encrypt(byte[] payload, byte[] otherPublicKey, out CryptPacket p)
        {
            using Aes aes = new AesCryptoServiceProvider();
            p.iv = aes.IV;

            using RSACryptoServiceProvider otherKey = new();
            otherKey.ImportCspBlob(otherPublicKey);
            RSAPKCS1KeyExchangeFormatter keyFormatter = new(otherKey);
            p.encryptedSessionKey = keyFormatter.CreateKeyExchange(aes.Key, typeof(Aes));

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

            // Decrypt the session key
            RSAPKCS1KeyExchangeDeformatter keyDeformatter = new(rsaKey);
            aes.Key = keyDeformatter.DecryptKeyExchange(p.encryptedSessionKey);

            // Decrypt the message
            using MemoryStream plaintext = new();
            using CryptoStream cs = new(plaintext, aes.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(p.encryptedMessage, 0, p.encryptedMessage.Length);
            cs.Close();

            payload = plaintext.ToArray();
        }

        public static void Encrypt(string message, byte[] otherPublicKey, out CryptPacket p) 
            => Encrypt(Encoding.UTF8.GetBytes(message), otherPublicKey, out p);

        public void Decrypt(CryptPacket p, out string message)
        {
            Decrypt(p, out byte[] payload);
            message = Encoding.UTF8.GetString(payload);
        }

        public static void Encrypt<T>(T payload, byte[] otherPublicKey, out CryptPacket p) 
            => Encrypt(JsonConvert.SerializeObject(payload), otherPublicKey, out p);

        public void Decrypt<T>(CryptPacket p, out T payload)
        {
            Decrypt(p, out string json);
            payload = JsonConvert.DeserializeObject<T>(json);
        }

        public void Dispose() => rsaKey.Dispose();
    }
}
