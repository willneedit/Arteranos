/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */


using System;
using System.IO;
using System.Security.Cryptography;

namespace Arteranos.Core.Cryptography
{
    public class SymmetricKey : IDisposable
    {
        public byte[] IV { get; set; } = null;
        public byte[] Key => Aes.Key;
        private Aes Aes { get; set; } = null;

        public static SymmetricKey Generate()
        {
            Aes aes = new AesCryptoServiceProvider();
            aes.GenerateIV();
            aes.GenerateKey();

            return new()
            {
                Aes = aes,
                IV = aes.IV,
            };
        }

        public static SymmetricKey Import(byte[] key, byte[] iv)
        {
            Aes aes = new AesCryptoServiceProvider
            {
                Key = key,
                IV = iv
            };

            return new()
            {
                Aes = aes,
                IV = aes.IV,
            };
        }

        public void Encrypt(byte[] plaintext, out byte[] cipher)
        {
            Aes.IV = IV;

            using MemoryStream ciphertext = new();
            using CryptoStream cs = new(ciphertext, Aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(plaintext, 0, plaintext.Length);
            cs.Close();

            cipher = ciphertext.ToArray();
        }

        public void Decrypt(byte[] cipher, out byte[] plain)
        {
            Aes.IV = IV;

            using MemoryStream plaintext = new();
            using CryptoStream cs = new(plaintext, Aes.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(cipher, 0, cipher.Length);
            cs.Close();

            plain = plaintext.ToArray();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Aes?.Dispose();
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }

    }
}