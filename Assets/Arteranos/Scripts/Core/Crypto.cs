/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Arteranos.Core.Crypto
{
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

        public void Encrypt(byte[] payload, byte[] otherPublicKey, out byte[] iv, out byte[] encryptedSessionKey, out byte[] encryptedMessage)
        {
            using Aes aes = new AesCryptoServiceProvider();
            iv = aes.IV;

            using RSACryptoServiceProvider otherKey = new();
            otherKey.ImportCspBlob(otherPublicKey);
            RSAPKCS1KeyExchangeFormatter keyFormatter = new(otherKey);
            encryptedSessionKey = keyFormatter.CreateKeyExchange(aes.Key, typeof(Aes));

            using MemoryStream ciphertext = new();
            using CryptoStream cs = new(ciphertext, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(payload, 0, payload.Length);
            cs.Close();

            encryptedMessage = ciphertext.ToArray();
        }

        public void Decrypt(byte[] iv, byte[] encryptedSessionKey, byte[] encryptedMessage, out byte[] payload)
        {

            using Aes aes = new AesCryptoServiceProvider();
            aes.IV = iv;

            // Decrypt the session key
            RSAPKCS1KeyExchangeDeformatter keyDeformatter = new(rsaKey);
            aes.Key = keyDeformatter.DecryptKeyExchange(encryptedSessionKey);

            // Decrypt the message
            using MemoryStream plaintext = new();
            using CryptoStream cs = new(plaintext, aes.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(encryptedMessage, 0, encryptedMessage.Length);
            cs.Close();

            payload = plaintext.ToArray();
        }

        public void Encrypt(string message, byte[] otherPublicKey, out byte[] iv, out byte[] encryptedSessionKey, out byte[] encryptedMessage)
        {
            byte[] payload = Encoding.UTF8.GetBytes(message);
            Encrypt(payload, otherPublicKey, out iv, out encryptedSessionKey, out encryptedMessage);
        }

        public void Decrypt(byte[] iv, byte[] encryptedSessionKey, byte[] encryptedMessage, out string message)
        {
            Decrypt(iv, encryptedSessionKey, encryptedMessage, out byte[] payload);
            message = Encoding.UTF8.GetString(payload);
        }

        public void Dispose() => rsaKey.Dispose();
    }
}
