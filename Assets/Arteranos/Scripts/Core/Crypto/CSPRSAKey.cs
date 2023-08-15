/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Security.Cryptography;

namespace Arteranos.Core
{
    public class CSPRSAKey : IAsymmetricKey, IKeyWrapKey, ISignKey
    {
        public KeyType KeyType => KeyType.RSA;
        public ProviderType ProviderType => ProviderType.CSP;
        public byte[] PublicKey => rsaKey.ExportCspBlob(false);
        public byte[] ExportPrivateKey() => rsaKey.ExportCspBlob(true);

        private readonly RSACryptoServiceProvider rsaKey;

        public CSPRSAKey()
        {
            rsaKey = new();
        }

        public CSPRSAKey(byte[] exportedKey)
        {
            rsaKey = new();
            rsaKey.ImportCspBlob(exportedKey);
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
