/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Security.Cryptography;
using ASN1Utils;

namespace Arteranos.Core
{
    public class RSAKey : IAsymmetricKey, IKeyWrapKey, ISignKey
    {
        public KeyType KeyType => KeyType.RSA;
        public byte[] PublicKey => publicKey;
        public byte[] ExportPrivateKey() => rsaKey.ExportDER(true);

        private readonly RSA rsaKey;
        private readonly byte[] publicKey;

        public RSAKey()
        {
            rsaKey = RSA.Create();
            publicKey = rsaKey.ExportDER(false);
        }

        public RSAKey(byte[] exportedKey)
        {
            KeyImport.ImportDER(exportedKey, out rsaKey);
            publicKey = rsaKey.ExportDER(false);
        }

        public void Dispose() => rsaKey.Dispose();

        public void WrapKey(byte[] symKey, out byte[] wrappedKey)
        {
            RSAPKCS1KeyExchangeFormatter formatter = new(rsaKey);
            wrappedKey = formatter.CreateKeyExchange(symKey, typeof(Aes));
        }

        public void UnwrapKey(byte[] wrappedKey, out byte[] symKey)
        {
            RSAPKCS1KeyExchangeDeformatter deformatter = new(rsaKey);
            symKey = deformatter.DecryptKeyExchange(wrappedKey);
        }

        public void Sign(byte[] data, out byte[] signature)
            => signature = rsaKey.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        public bool Verify(byte[] data, byte[] signature)
            => rsaKey.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }
}
