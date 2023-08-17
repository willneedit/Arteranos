/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
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

    public class Crypto : IDisposable, IEquatable<Crypto>
    {
        public byte[] PublicKey => Key.PublicKey;

        public const string FP_SHA256 = "SHA256";          // Full SHA256 hexdump fingerprint (length: 64)
        public const string FP_SHA256_16 = "SHA256_8";     // Leading 16 hex digits (64 Bits entropy)
        public const string FP_SHA256_20 = "SHA256_10";    // Leading 20 hex digits (80 Bits entropy)

        public const string FP_Base64 = "Basde64";         // Full Base64 fingerprint (length: 44)
        public const string FP_Base64_8 = "Base64_8";      // Leading 8 'digits' (48 Bits entropy)
        public const string FP_Base64_10 = "Base64_10";    // Leading 10 'digits' (60 Bits entropy)
        public const string FP_Base64_15 = "Base64_15";    // Leading 15 'digits' (90 Bits entropy)

        public const string FP_Dice_4 = "Dice4";           // Four Diceware words (51 Bits entropy)
        public const string FP_Dice_5 = "Dice5";           // Five Diceware words (64 Bits entropy)

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

        private string WordListSelector(byte[] fpBytes, int howmany)
        {
            // Add a null byte as the MSB and the cleared sign bit.
            byte[] unsignedfpbytes = new byte[fpBytes.Length + 1];
            fpBytes.CopyTo(unsignedfpbytes, 0);

            string[] words = new string[howmany];
            BigInteger fpBI = new(unsignedfpbytes);

            for(int i = 0; i < howmany; i++)
            {
                fpBI = BigInteger.DivRem(fpBI, Words.words.Length, out BigInteger rem);
                words[i] = Words.words[(int) rem];
            }

            return string.Join(" ", words);
        }

        public byte[] Fingerprint {  get
            {
                using IncrementalHash myHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                myHash.AppendData(Key.PublicKey);
                return myHash.GetHashAndReset();
            }
        }
        public override string ToString() => ToString(FP_SHA256);

        public string ToString(string v)
        {
            switch(v)
            {
                case FP_Dice_4:
                    return WordListSelector(Fingerprint, 4);
                case FP_Dice_5:
                    return WordListSelector(Fingerprint, 5);
                case FP_Base64_15:
                    return ToString(FP_Base64)[0..14];
                case FP_Base64_10:
                    return ToString(FP_Base64)[0..9];
                case FP_Base64_8:
                    return ToString(FP_Base64)[0..7];
                case FP_Base64:
                    return Convert.ToBase64String(Fingerprint);
                case FP_SHA256_20:
                    return ToString(FP_SHA256)[0..19];
                case FP_SHA256_16:
                    return ToString(FP_SHA256)[0..15];
                case FP_SHA256:
                default:
                    string hashString = string.Empty;
                    foreach(byte x in Fingerprint) { hashString += String.Format("{0:x2}", x);  }
                    return hashString;
            }
        }

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
