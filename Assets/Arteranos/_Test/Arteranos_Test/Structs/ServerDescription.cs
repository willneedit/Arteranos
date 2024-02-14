using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using Arteranos.Core.Cryptography;

namespace Arteranos.Test.Structs
{


    public class ServerDescription
    {
        public Core.ServerDescription sample = null;

        [SetUp]
        public void Setup()
        {
            sample = new()
            {
                Name = "Snake Oil",
                ServerPort = 1,
                MetadataPort = 2,
                Description = "Snake Oil Inc.",
                Icon = new byte[0],
                Version = "0.0.1",
                MinVersion = "0.0.1",
                Permissions = new(),
                PrivacyTOSNotice = "TODO",      // TODO
                AdminNames = new string[0],     // TODO
                PeerID = "1DfooId",
                LastModified = DateTime.UnixEpoch
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

            Core.ServerDescription d = null;
            using (MemoryStream ms = new())
            {
                ms.Write(bytes, 0, bytes.Length);
                ms.Position = 0;
                d = Core.ServerDescription.Deserialize(serverKey.PublicKey, ms);
            }

            Assert.AreEqual(sample, d);
            Assert.AreNotSame(sample, d);

            // But now it has to be.
            d.Name = "Foo";

            Assert.AreNotEqual(sample, d);
            Assert.AreNotSame(sample, d);
        }

        [Test]
        public void Reserialize()
        {
            SignKey serverKey = SignKey.Generate();

            byte[] bytes = null;
            using (MemoryStream ms = new())
            {
                sample.Serialize(serverKey, ms);
                ms.Position = 0;
                bytes = ms.ToArray();
            }

            Core.ServerDescription d = null;
            using (MemoryStream ms = new())
            {
                ms.Write(bytes, 0, bytes.Length);
                ms.Position = 0;
                d = Core.ServerDescription.Deserialize(serverKey.PublicKey, ms);
            }

            // Try to save the other's server description for posterity.
            byte[] bytes2 = null;
            using (MemoryStream ms = new())
            {
                d.Serialize(ms);
                ms.Position = 0;
                bytes2 = ms.ToArray();
            }

            // Check the authenticity of the redistributed server description
            Core.ServerDescription d2 = null;
            using (MemoryStream ms = new())
            {
                ms.Write(bytes, 0, bytes2.Length);
                ms.Position = 0;
                d2 = Core.ServerDescription.Deserialize(serverKey.PublicKey, ms);
            }

            Assert.AreEqual (bytes, bytes2);
            Assert.AreEqual (sample, d2);
            Assert.AreEqual (d, d2);
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
                    Core.ServerDescription d = Core.ServerDescription.Deserialize(wrongKey.PublicKey, ms);
                });
            }
        }

        [Test]
        public void DBStore()
        {
            try
            {
                Assert.IsTrue(sample.DBUpdate());
            }
            finally
            {
                Core.ServerDescription.DBDelete(sample.PeerID);
            }
        }

        [Test]
        public void DBLookup1()
        {
            try
            {
                Assert.IsNull(Core.ServerDescription.DBLookup(sample.PeerID));

                Assert.IsTrue(sample.DBUpdate());

                Core.ServerDescription anObject = Core.ServerDescription.DBLookup(sample.PeerID);
                Assert.IsNotNull(anObject);
                Assert.AreEqual(sample, anObject);
            }
            finally
            {
                Core.ServerDescription.DBDelete(sample.PeerID);
            }
        }

        [Test]
        public void DBLookup2()
        {
            Core.ServerDescription sample2 = null;

            try
            {
                Assert.IsNull(Core.ServerDescription.DBLookup(sample.PeerID));

                Assert.IsTrue(sample.DBUpdate());

                sample2 = Core.ServerDescription.DBLookup(sample.PeerID);
                Assert.IsNotNull(sample2);

                sample2.PeerID = "1DBar";

                Assert.IsNull(Core.ServerDescription.DBLookup(sample2.PeerID));

                Assert.IsTrue(sample2.DBUpdate());

                Assert.IsNotNull(Core.ServerDescription.DBLookup(sample2.PeerID));
            }
            finally
            {
                Core.ServerDescription.DBDelete(sample.PeerID);

                if(sample2 != null) Core.ServerDescription.DBDelete(sample2.PeerID);
            }
        }

        [Test]
        public void DBUpdateExpired()
        {
            try
            {
                Assert.IsTrue(sample.DBUpdate());

                sample.LastModified = DateTime.MinValue;

                Assert.IsFalse(sample.DBUpdate());

                Core.ServerDescription probe = Core.ServerDescription.DBLookup(sample.PeerID);

                Assert.AreNotEqual(probe, sample);
            }
            finally
            {
                Core.ServerDescription.DBDelete(sample.PeerID);
            }
        }

        [Test]
        public void DBUpdateNotExpired()
        {
            try
            {
                Assert.IsTrue(sample.DBUpdate());

                sample.LastModified = DateTime.MaxValue;

                Assert.IsTrue(sample.DBUpdate());

                Core.ServerDescription probe = Core.ServerDescription.DBLookup(sample.PeerID);

                Assert.AreEqual(sample, probe);
            }
            finally
            {
                Core.ServerDescription.DBDelete(sample.PeerID);
            }
        }

        [Test]
        public void DBList()
        {
            try
            {
                Assert.IsNull(Core.ServerDescription.DBLookup(sample.PeerID));

                var list1 = Core.ServerDescription.DBList();

                Assert.IsFalse(list1.Contains(sample));

                Assert.IsTrue(sample.DBUpdate());

                var list2 = Core.ServerDescription.DBList();

                Assert.IsTrue(list2.Contains(sample));

                var list3 = Core.ServerDescription.DBList();

                Core.ServerDescription.DBDelete(sample.PeerID);

                Assert.IsFalse(list3.Contains(sample));
            }
            finally
            {
                Core.ServerDescription.DBDelete(sample.PeerID);
            }
        }


    }
}