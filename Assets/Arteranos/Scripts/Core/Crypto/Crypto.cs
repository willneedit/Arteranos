/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DERSerializer;

namespace Arteranos.Core
{
    public struct CryptPacket
    {
        public byte[] iv;
        public byte[] encryptedSessionKey;
        public byte[] encryptedMessage;
    }

    public struct CMSPacket
    {
        public byte[] iv;
        public byte[] payloadDER;
        public List<ESKEntry> encryptedSessionKeys;
    }

    public struct ESKEntry
    {
        public byte[] fingerprint;
        public byte[] encryptedSessionKey;
    }

    internal struct CMSPayload
    {
        public byte[] messageDER;
        public byte[] signatureKey;
        public byte[] signature;
    }

    public class Crypto : IDisposable, IEquatable<Crypto>
    {
        public byte[] PublicKey => Key.PublicKey;

        private readonly IFullAsymmetricKey Key;

        public Crypto()
        {
            Key = CreateKey();
        }

        public Crypto(byte[] keyBlob)
        {
            Key = CreateKey(keyBlob);
        }

        private static IFullAsymmetricKey CreateKey(byte[] keyBlob = null)
        {
            // TODO Extend to return ECC keys as default, and to determine the blob's key type
            if (keyBlob == null) return new RSAKey();

            return new RSAKey(keyBlob);
        }

        public byte[] Export(bool includePrivateParameters)
            => includePrivateParameters ? Key.ExportPrivateKey() : Key.PublicKey;

        #region Public Key Fingerprint

        public byte[] Fingerprint { get => CryptoHelpers.GetFingerprint(Key.PublicKey); }
        #endregion

        #region Hashes

        public static byte[] SHA256(byte[] data)
        {
            using IncrementalHash myHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            myHash.AppendData(data);
            return myHash.GetHashAndReset();
        }

        public static byte[] SHA256(string data) 
            => SHA256(Encoding.UTF8.GetBytes(data));

        #endregion

        #region Encrypt and decrypt

        public static void Encrypt(byte[] payload, byte[] otherPublicKey, out CryptPacket p)
        {
            using Aes aes = new AesCryptoServiceProvider();
            p.iv = aes.IV;

            using IKeyWrapKey otherKey = CreateKey(otherPublicKey);
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
        {
            Encrypt(Serializer.Serialize(payload), otherPublicKey, out p);
        }

        public void Decrypt<T>(CryptPacket p, out T payload)
        {
            Decrypt(p, out byte[] json);
            payload = Serializer.Deserialize<T>(json);
        }

        #endregion

        #region Sign and verify

        public static bool Verify(byte[] data, byte[] signature, byte[] otherPublicKey)
        {
            using ISignKey otherKey = CreateKey(otherPublicKey);

            return otherKey.Verify(data, signature);
        }

        public void Sign(byte[] data, out byte[] signature)
            => Key.Sign(data, out signature);

        public static bool Verify<T>(T data, byte[] signature, byte[] otherPublicKey)
            => Verify(Serializer.Serialize(data), signature, otherPublicKey);

        public void Sign<T>(T data, out byte[] signature)
            => Sign(Serializer.Serialize(data), out signature);

        #endregion

        #region Encapsulating

        private void EncapsulateMessage<T>(T message, out CMSPayload payload)
        {
            payload = new()
            {
                messageDER = Serializer.Serialize(message),
                signatureKey = PublicKey,
            };

            // Sign with the corresponding private key.
            Sign(payload.messageDER, out payload.signature);
        }

        private static void DecapsulateMessage<T>(CMSPayload payload, out T message) 
            => message = Serializer.Deserialize<T>(payload.messageDER);

        private static Aes CreateSessionKeys(byte[][] receiverPublicKeys, out List<ESKEntry> entries)
        {
            Aes aes = new AesCryptoServiceProvider();

            entries = new();
            foreach (byte[] key in receiverPublicKeys)
            {
                ESKEntry entry = new()
                {
                    // Use the receiver's public key fingerprints to identify them
                    fingerprint = CryptoHelpers.GetFingerprint(key)
                };
                // Wrap the session key with the receiver's public keys
                using IKeyWrapKey wrapKey = CreateKey(key);
                wrapKey.WrapKey(aes.Key, out entry.encryptedSessionKey);
                entries.Add(entry);
            }

            return aes;
        }

        private Aes FindSessionKey(List<ESKEntry> entries)
        {
            foreach(ESKEntry entry in entries)
            {
                if(entry.fingerprint.SequenceEqual(Fingerprint))
                {
                    Key.UnwrapKey(entry.encryptedSessionKey, out byte[] aesKey);
                    Aes aes = new AesCryptoServiceProvider { Key = aesKey };
                    return aes;
                }
            }

            throw new CryptographicException("Cannot find encrypted session key");
        }

        private static CMSPacket EncryptMessage(byte[][] receiverPublicKeys, CMSPayload payload)
        {
            using Aes aes = CreateSessionKeys(receiverPublicKeys, out List<ESKEntry> entries);

            CMSPacket packet;

            packet.iv = aes.IV;
            packet.encryptedSessionKeys = entries;

            byte[] payloadDER = Serializer.Serialize(payload);

            using MemoryStream ciphertext = new();
            using CryptoStream cs = new(ciphertext, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(payloadDER, 0, payloadDER.Length);
            cs.Close();

            packet.payloadDER = ciphertext.ToArray();

            return packet;
        }

        private static void CheckMessageConsistency(ref byte[] expectedSignatureKey, CMSPayload payload)
        {
            byte[] signatureKey = payload.signatureKey;

            if (expectedSignatureKey == null)
                expectedSignatureKey = signatureKey;

            // Check against a specific sender
            else if (!expectedSignatureKey.SequenceEqual(signatureKey))
            {
                expectedSignatureKey = signatureKey; // Nevertheless, return the key we've got.
                throw new CryptographicException("Sender mismatch");
            }

            // Verify against the sender's public key
            if (!Verify(payload.messageDER, payload.signature, payload.signatureKey))
                throw new CryptographicException("Signature verification failed");
        }

        private CMSPayload DecryptMessage(CMSPacket packet)
        {
            using Aes aes = FindSessionKey(packet.encryptedSessionKeys);
            aes.IV = packet.iv;

            using MemoryStream plaintext = new();
            using CryptoStream cs = new(plaintext, aes.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(packet.payloadDER, 0, packet.payloadDER.Length);
            cs.Close();

            return Serializer.Deserialize<CMSPayload>(plaintext.ToArray());
        }

        public void TransmitMessage<T>(T data, byte[][] receiverPublicKeys, out CMSPacket packet)
        {
            EncapsulateMessage(data, out CMSPayload payload);
            packet = EncryptMessage(receiverPublicKeys, payload);
        }

        public void TransmitMessage<T>(T data, byte[] receiverPublicKey, out CMSPacket packet) 
            => TransmitMessage(data, new byte[][] { receiverPublicKey }, out packet);

        public void ReceiveMessage<T>(CMSPacket packet, ref byte[] expectedSignatureKey, out T data)
        {
            CMSPayload payload = DecryptMessage(packet);
            CheckMessageConsistency(ref expectedSignatureKey, payload);
            DecapsulateMessage(payload, out data);
        }

        #endregion

        #region Boilerplate

        public void Dispose() => Key.Dispose();
        public override bool Equals(object obj) => Equals(obj as Crypto);
        public bool Equals(Crypto other) => other is not null && PublicKey.SequenceEqual(other.PublicKey);
        public override int GetHashCode() => HashCode.Combine(PublicKey);

        public static bool operator ==(Crypto left, Crypto right) => EqualityComparer<Crypto>.Default.Equals(left, right);
        public static bool operator !=(Crypto left, Crypto right) => !(left == right);
        public override string ToString() => ToString(CryptoHelpers.FP_SHA256);
        public string ToString(string v) => CryptoHelpers.ToString(v, Key.PublicKey);

        #endregion
    }
}
