using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Ipfs.Engine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Ipfs;
using Ipfs.Core.Cryptography.Proto;
using Ipfs.Core.Cryptography;
using Arteranos.PlayTest;
using System.Threading;
using System.Linq;

using Arteranos.Core.Cryptography;
using System.Text;

namespace Arteranos.PlayTest
{
    public class Cryptography
    {
        [Test]
        public void CreateSignKey()
        {
            SignKey crypto = SignKey.Generate();

            Assert.IsNotNull(crypto.KeyPair);
            Assert.IsNotNull(crypto.PublicKey);
        }

        [Test]
        public void ExportSignKey()
        {
            SignKey crypto = SignKey.Generate();

            crypto.ExportPrivateKey(out byte[] exported);
            Assert.IsNotNull(exported);
            Assert.IsTrue(exported.Length > 0);

            SignKey clone = SignKey.ImportPrivateKey(exported);

            Assert.AreEqual(crypto.PublicKey, clone.PublicKey);
        }

        [Test]
        public void SignVerify()
        {
            byte[] toSign = Encoding.UTF8.GetBytes("This is a text to be signed.");

            SignKey alice = SignKey.Generate();

            alice.Sign(toSign, out byte[] signature);
            alice.ExportPublicKey(out byte[] alicePubKey);

            // Bob gets Alice's Public Key and the document's signature.
            Assert.DoesNotThrow(() =>
            {
                SignKey.Verify(alicePubKey, toSign, signature);
            });
        }

        [Test]
        public void SignVerifyWithSerialized()
        {
            byte[] toSign = Encoding.UTF8.GetBytes("This is a text to be signed.");

            SignKey alice = SignKey.Generate();
            alice.ExportPublicKey(out byte[] alicePubKey);

            // Alice saves her session and restores it later.
            alice.ExportPrivateKey(out byte[] exported);
            SignKey clone = SignKey.ImportPrivateKey(exported);

            clone.Sign(toSign, out byte[] signature);

            // Bob gets Alice's Public Key and the document's signature.
            Assert.DoesNotThrow(() =>
            {
                SignKey.Verify(alicePubKey, toSign, signature);
            });
        }

        [Test]
        public void CreateEncryptionKey() 
        {
            EncryptionKey crypto = EncryptionKey.Generate();

            Assert.IsNotNull(crypto.KeyPair);
            Assert.IsNotNull(crypto.PublicKey);
        }

        [Test]
        public void ExportDecryptionKey() 
        {
            EncryptionKey crypto = EncryptionKey.Generate();

            crypto.ExportPrivateKey(out byte[] exported);
            Assert.IsNotNull(exported);
            Assert.IsTrue(exported.Length > 0);

            EncryptionKey clone = EncryptionKey.ImportPrivateKey(exported);

            Assert.AreEqual(crypto.PublicKey, clone.PublicKey);
        }

        [Test]
        public void EncryptDecrypt() 
        {
            byte[] toEncrypt = Encoding.UTF8.GetBytes("This is a text to be signed.");

            EncryptionKey bob = EncryptionKey.Generate();
            bob.ExportPublicKey(out byte[] exported);

            AsymmetricKey.Encrypt(exported, toEncrypt, out byte[] cipher);

            bob.Decrypt(cipher, out byte[] returned);

            Assert.AreEqual(toEncrypt, returned);
        }

        [Test]
        public void EncryptEncryptWithSerialized() 
        {
            byte[] toEncrypt = Encoding.UTF8.GetBytes("This is a text to be signed.");

            EncryptionKey bob = EncryptionKey.Generate();
            bob.ExportPublicKey(out byte[] exported);

            bob.ExportPrivateKey(out byte[] exportedkp);
            EncryptionKey clone = EncryptionKey.ImportPrivateKey(exportedkp);

            AsymmetricKey.Encrypt(exported, toEncrypt, out byte[] cipher);

            clone.Decrypt(cipher, out byte[] returned);

            Assert.AreEqual(toEncrypt, returned);
        }
    }
}