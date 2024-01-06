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
            Crypto crypto = new Crypto();

            Assert.IsNotNull(crypto.SignKeyPair);
            Assert.IsNotNull(crypto.SignKeyPair.PublicKey);
        }

        [Test]
        public void ExportSignKey()
        {
            Crypto crypto = new Crypto();

            crypto.ExportPrivateKey(out byte[] exported);
            Assert.IsNotNull(exported);
            Assert.IsTrue(exported.Length > 0);

            Crypto clone = new Crypto(exported);

            Assert.AreEqual(crypto.SignKeyPair.PublicKey, clone.SignKeyPair.PublicKey);
        }

        [Test]
        public void SignVerify()
        {
            byte[] toSign = Encoding.UTF8.GetBytes("This is a text to be signed.");

            Crypto alice = new Crypto();

            alice.Sign(toSign, out byte[] signature);
            alice.ExportSignPublicKey(out byte[] alicePubKey);

            // Bob gets Alice's Public Key and the document's signature.
            Assert.DoesNotThrow(() =>
            {
                Crypto.Verify(alicePubKey, toSign, signature);
            });
        }

        [Test]
        public void SignVerifyWithSerialized()
        {
            byte[] toSign = Encoding.UTF8.GetBytes("This is a text to be signed.");

            Crypto alice = new Crypto();
            alice.ExportSignPublicKey(out byte[] alicePubKey);

            // Alice saves her session and restores it later.
            alice.ExportPrivateKey(out byte[] exported);
            Crypto clone = new Crypto(exported);

            clone.Sign(toSign, out byte[] signature);

            // Bob gets Alice's Public Key and the document's signature.
            Assert.DoesNotThrow(() =>
            {
                Crypto.Verify(alicePubKey, toSign, signature);
            });
        }

    }
}