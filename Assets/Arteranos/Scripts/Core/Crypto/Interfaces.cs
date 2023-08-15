/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;

namespace Arteranos.Core
{
    public enum KeyType
    {
        RSA = 0,
        EC = 1
    }

    public enum ProviderType
    {
        CSP = 0,
        CNG = 1,
        OpenSSL = 2
    }

    /// <summary>
    /// Asymmetric key. Provides functions for creating, exporting and importing such keys
    /// </summary>
    internal interface IAsymmetricKey : IDisposable
    {
        public KeyType KeyType { get; }
        public ProviderType ProviderType { get; }

        //public IAsymmetricKey Create();
        //public IAsymmetricKey Create(byte[] exportedPrivateKey);

        public byte[] PublicKey { get; }
        public byte[] ExportPrivateKey();
    }

    /// <summary>
    /// Provides functions for wrapping keys
    /// </summary>
    internal interface IKeyWrapKey
    {
        public void WrapKey(byte[] symKey, out byte[] wrappedKey);
        public void UnwrapKey(byte[] wrappedKey, out byte[] symKey);
    }

    /// <summary>
    /// Provides signing any data with this key
    /// </summary>
    internal interface ISignKey
    {
        public void Sign(byte[] data, out byte[] signature);
        public bool Verify(byte[] data, byte[] signature);
    }

}
