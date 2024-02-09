/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using KeyPair = Ipfs.Core.Cryptography.KeyPair;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.OpenSsl;
using Ipfs.Core.Cryptography.Proto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Crypto.Agreement;
using System;
using System.Collections.Generic;

namespace Arteranos.Core.Cryptography
{
    public interface IAsymmetricKey
    {
        public KeyPair KeyPair { get; }
        public PublicKey PublicKey { get; }
        byte[] Fingerprint { get; }

        void ExportPrivateKey(out byte[] keyBytes);
        void ExportPublicKey(out byte[] keyBytes);
    }

    public interface ISignKey : IAsymmetricKey
    {
        void Sign(byte[] data, out byte[] signature);
    }

    public interface IEncryptionKey : IAsymmetricKey
    {
        void Decrypt(byte[] cipher, out byte[] data);
    }

    public interface IAgreeKey : IAsymmetricKey
    {
        void Agree(byte[] otherPublicKey, out byte[] sharedSecret);
    }

    public class AsymmetricKey : IDisposable, IEquatable<AsymmetricKey>
    {
        protected AsymmetricKey() { }

        public KeyPair KeyPair { get => keyPair; }
        public PublicKey PublicKey { get => keyPair.PublicKey; }
        public byte[] Fingerprint
        {
            get
            {
                fingerprint ??= CryptoHelpers.GetFingerprint(PublicKey.Serialize());
                return fingerprint;
            }
        }

        protected KeyPair keyPair = null;
        protected AsymmetricKeyParameter PrivateKey = null;
        protected byte[] fingerprint = null;


        protected static T Generate<T>(T c, IAsymmetricCipherKeyPairGenerator g) where T : AsymmetricKey
        {
            c.PrivateKey = g.GenerateKeyPair().Private;
            c.keyPair = KeyPair.Import(c.PrivateKey);
            return c;
        }

        protected static T ImportPrivateKey<T>(T c, AsymmetricKeyParameter signPrivateKey) where T : AsymmetricKey
        {
            c.PrivateKey = signPrivateKey;
            c.keyPair = KeyPair.Import(signPrivateKey);
            return c;
        }

        protected static T ImportPrivateKey<T>(T c, byte[] keyBytes) where T : AsymmetricKey
        {
            Asn1Sequence seq = Asn1Sequence.GetInstance(keyBytes);
            c.PrivateKey =
                PrivateKeyFactory.CreateKey(PrivateKeyInfo.GetInstance(seq));
            c.keyPair = KeyPair.Import(c.PrivateKey);
            return c;
        }


        public void ExportPrivateKey(out byte[] keyBytes)
        {
            Pkcs8Generator pkcs8 = new(PrivateKey, null);
            keyBytes = pkcs8.Generate().Content;
        }

        public void ExportPublicKey(out byte[] keyBytes)
            => keyBytes = PublicKey.Serialize();

        public static void Verify(byte[] othersPublicKey, byte[] data, byte[] signature)
        {
            PublicKey key = PublicKey.Deserialize(othersPublicKey);
            key.Verify(data, signature);
        }

        public static void Encrypt(byte[] othersPublicKey, byte[] data, out byte[] cipher)
        {
            PublicKey key = PublicKey.Deserialize(othersPublicKey);
            AsymmetricKeyParameter publicKey = PublicKeyFactory.CreateKey(key.Data);
            Pkcs1Encoding encryptEngine = new(new RsaEngine());
            encryptEngine.Init(true, publicKey);
            cipher = encryptEngine.ProcessBlock(data, 0, data.Length);
        }


        public override string ToString() => ToString(CryptoHelpers.FP_SHA256);
        public string ToString(string v) => CryptoHelpers.ToString(v, PublicKey.Serialize());

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                keyPair = null;
                PrivateKey = null;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AsymmetricKey);
        }

        public bool Equals(AsymmetricKey other)
        {
            return other is not null &&
                   EqualityComparer<PublicKey>.Default.Equals(PublicKey, other.PublicKey);
        }

        public override int GetHashCode()
        {
            HashCode hc = new();
            foreach(byte b in Fingerprint) hc.Add(b);
            return hc.ToHashCode();
        }

        public static bool operator ==(AsymmetricKey left, AsymmetricKey right)
        {
            return EqualityComparer<AsymmetricKey>.Default.Equals(left, right);
        }

        public static bool operator !=(AsymmetricKey left, AsymmetricKey right)
        {
            return !(left == right);
        }
    }

    public class SignKey : AsymmetricKey, IAsymmetricKey, ISignKey
    {
        private SignKey() { }

        public static SignKey Generate()
        {
            IAsymmetricCipherKeyPairGenerator g = GeneratorUtilities.GetKeyPairGenerator("Ed25519");
            g.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));

            return Generate(new SignKey(), g);
        }

        public static SignKey ImportPrivateKey(AsymmetricKeyParameter signPrivateKey) => ImportPrivateKey(new SignKey(), signPrivateKey);
        public static SignKey ImportPrivateKey(byte[] keyBytes) => ImportPrivateKey(new SignKey(), keyBytes);

        public void Sign(byte[] data, out byte[] signature)
            => signature = keyPair.Sign(data);
    }

    public class EncryptionKey : AsymmetricKey, IAsymmetricKey, IEncryptionKey
    {
        private EncryptionKey() { }

        public static EncryptionKey Generate()
        {
            IAsymmetricCipherKeyPairGenerator g = GeneratorUtilities.GetKeyPairGenerator("RSA");
            g.Init(new RsaKeyGenerationParameters(
                BigInteger.ValueOf(0x10001), new SecureRandom(), 2048, 25));

            return Generate(new EncryptionKey(), g);
        }

        public static EncryptionKey ImportPrivateKey(AsymmetricKeyParameter decryptPrivateKey) => ImportPrivateKey(new EncryptionKey(), decryptPrivateKey);
        public static EncryptionKey ImportPrivateKey(byte[] keyBytes) => ImportPrivateKey(new EncryptionKey(), keyBytes);

        public void Decrypt(byte[] cipher, out byte[] data)
        {
            var encryptEngine = new Pkcs1Encoding(new RsaEngine());
            encryptEngine.Init(false, PrivateKey);
            data = encryptEngine.ProcessBlock(cipher, 0, cipher.Length);
        }
    }

    public class AgreeKey : AsymmetricKey, IAsymmetricKey, IAgreeKey
    {
        private AgreeKey() { }

        public static AgreeKey Generate()
        {
            IAsymmetricCipherKeyPairGenerator g = GeneratorUtilities.GetKeyPairGenerator("ECDH");
            g.Init(new ECKeyGenerationParameters(SecObjectIdentifiers.SecP256k1, new SecureRandom()));

            return Generate(new AgreeKey(), g);
        }

        public static AgreeKey ImportPrivateKey(AsymmetricKeyParameter decryptPrivateKey) => ImportPrivateKey(new AgreeKey(), decryptPrivateKey);
        public static AgreeKey ImportPrivateKey(byte[] keyBytes) => ImportPrivateKey(new AgreeKey(), keyBytes);

#if false // currently unsupported with IPFS's Secp256k1 keys.
        public void Sign(byte[] data, out byte[] signature)
            => signature = keyPair.Sign(data);
#endif

        public void Agree(byte[] otherPublicKey, out byte[] sharedSecret)
        {
            IBasicAgreement agreement = new ECDHBasicAgreement();

            agreement.Init(PrivateKey);
            PublicKey key = PublicKey.Deserialize(otherPublicKey);
            AsymmetricKeyParameter publicKey = PublicKeyFactory.CreateKey(key.Data);

            BigInteger secret = agreement.CalculateAgreement(publicKey);
            sharedSecret = secret.ToByteArrayUnsigned();
        }
    }
}