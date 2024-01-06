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

namespace Arteranos.Core.Cryptography
{
    public class Crypto
    {
        public KeyPair SignKeyPair { get => signKeyPair; }
        public KeyPair KxKeyPair { get => kxKeyPair; }

        private KeyPair signKeyPair = null;
        private KeyPair kxKeyPair = null;

        private AsymmetricKeyParameter signPrivateKey = null;

        public Crypto()
        {
            IAsymmetricCipherKeyPairGenerator g = GeneratorUtilities.GetKeyPairGenerator("Ed25519");
            g.Init(new Ed25519KeyGenerationParameters(new SecureRandom()));
            signPrivateKey = g.GenerateKeyPair().Private;

            signKeyPair = KeyPair.Import(signPrivateKey);
        }

        public Crypto(AsymmetricKeyParameter signPrivateKey)
        {
            this.signPrivateKey = signPrivateKey;

            signKeyPair = KeyPair.Import(signPrivateKey);
        }

        public Crypto(byte[] keyBytes)
        {
            Asn1Sequence seq = Asn1Sequence.GetInstance(keyBytes);
            signPrivateKey =
                PrivateKeyFactory.CreateKey(PrivateKeyInfo.GetInstance(seq));
            signKeyPair = KeyPair.Import(signPrivateKey);
        }

        public void ExportPrivateKey(out byte[] keyBytes)
        {
            var pkcs8 = new Pkcs8Generator(signPrivateKey, null);
            keyBytes = pkcs8.Generate().Content;
        }

        public void ExportSignPublicKey(out byte[] keyBytes) 
            => keyBytes = signKeyPair.PublicKey.Serialize();

        public void Sign(byte[] data, out byte[] signature)
            => signature = signKeyPair.Sign(data);

        public static void Verify(byte[] othersPublicKey, byte[] data, byte[] signature)
        {
            PublicKey bob = PublicKey.Deserialize(othersPublicKey);
            bob.Verify(data, signature);
        }
    }
}