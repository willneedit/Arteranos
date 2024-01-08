using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Ipfs.Engine;
using Arteranos.Services;
using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Arteranos.Core;
using Ipfs;
using System.Linq;
using System.Threading;
using Arteranos.Core.Cryptography;
using System.Security.Cryptography;

namespace Arteranos.PlayTest.Structs
{
    public class ServerDescription
    {
        public _ServerDescription sample = null;

        [SetUp]
        public void Setup()
        {
            sample = new _ServerDescription()
            {
                Name = "Test",
                ServerPort = 7777,
                MetadataPort = 7778,
                Permissions = new()
                {
                    Flying = true,
                    ExcessiveViolence = true,
                    ExplicitNudes = null,
                },
                AdminNames = new string[]
                {
                    "willneedit"
                }
            };
        }

        [TearDown]
        public void TearDown()
        {
            sample = null;
        }

        [Test]
        public void Construct()
        {

        }

        [Test]
        public void Serialize()
        {
            SignKey serverKey = SignKey.Generate();

            byte[] bytes = null;
            using (MemoryStream ms = new())
            {
                sample.Serialize(serverKey, ms);
                ms.Position = 0;
                bytes = ms.ToArray();
            }

            Assert.IsNotNull(bytes);
            Assert.IsNotNull(bytes.Length > 0);

            // Debug.Log($"{Convert.ToBase64String(bytes)}");
        }

        [Test]
        public void Deserialize()
        {
            SignKey serverKey = SignKey.Generate();

            byte[] bytes = null;
            using (MemoryStream ms = new())
            {
                sample.Serialize(serverKey, ms);
                ms.Position = 0;
                bytes = ms.ToArray();
            }

            _ServerDescription d = null;
            using (MemoryStream ms = new())
            {
                ms.Write(bytes, 0, bytes.Length);
                ms.Position = 0;
                d = _ServerDescription.Deserialize(serverKey.PublicKey, ms);
            }

            Assert.AreEqual(sample, d);
            Assert.AreNotSame(sample, d);

            // But now it has to be.
            d.Name = "Foo";

            Assert.AreNotEqual(sample, d);
            Assert.AreNotSame(sample, d);
        }

        [Test]
        public void Deserialize_Negative()
        {
            SignKey serverKey = SignKey.Generate();

            byte[] bytes = null;
            using (MemoryStream ms = new())
            {
                sample.Serialize(serverKey, ms);
                ms.Position = 0;
                bytes = ms.ToArray();
            }


            SignKey wrongKey = SignKey.Generate();

            using (MemoryStream ms = new())
            {
                ms.Write(bytes, 0, bytes.Length);
                ms.Position = 0;
                Assert.Throws<InvalidDataException>(() =>
                {
                    _ServerDescription d = _ServerDescription.Deserialize(wrongKey.PublicKey, ms);
                });
            }
        }

    }
}