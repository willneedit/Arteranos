using System.Collections.Generic;
using NUnit.Framework;
using System;
using System.IO;
using Arteranos.Core;
using Arteranos.Core.Cryptography;
using Ipfs.Core.Cryptography.Proto;

namespace Arteranos.PlayTest.Structs
{
    public class UserIDs
    {
        [Test]
        public void Construct()
        {
            SignKey aliceKey = SignKey.Generate();
            UserID alice = new(aliceKey.PublicKey, "Alice");

            Assert.IsNotNull(alice);

            // Explicit lookups
            Assert.AreEqual("Alice", alice.Nickname);
            Assert.AreEqual(aliceKey.PublicKey, alice.SignPublicKey);

            // Implicit conversions
            string aliceName = alice;
            Assert.AreEqual("Alice", aliceName);

            PublicKey alicePubKey = alice;
            Assert.AreEqual(aliceKey.PublicKey, alicePubKey);

        }

        [Test]
        public void SerializabilityJSON()
        {
            SignKey aliceKey = SignKey.Generate();
            UserID alice = new(aliceKey.PublicKey, "Alice");

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(alice);

            UserID aliceRestored = Newtonsoft.Json.JsonConvert.DeserializeObject<UserID>(json);

            Assert.IsNotNull(aliceRestored);
            Assert.AreEqual(alice.Nickname, aliceRestored.Nickname);
            Assert.AreEqual(alice.SignPublicKey, aliceRestored.SignPublicKey);
        }

        [Test]
        public void SerializabilityProtobuf()
        {
            SignKey aliceKey = SignKey.Generate();
            UserID alice = new(aliceKey.PublicKey, "Alice");

            using MemoryStream ms1 = new();
            ProtoBuf.Serializer.Serialize(ms1, alice);
            byte[] bytes = ms1.ToArray();

            using MemoryStream ms2 = new(bytes);
            UserID aliceRestored = ProtoBuf.Serializer.Deserialize<UserID>(ms2);

            Assert.IsNotNull(aliceRestored);
            Assert.AreEqual(alice.Nickname, aliceRestored.Nickname);
            Assert.AreEqual(alice.SignPublicKey, aliceRestored.SignPublicKey);
        }

        [Test]
        public void Equality()
        {
            SignKey aliceKey = SignKey.Generate();
            UserID alice = new(aliceKey.PublicKey, "Alice");

            SignKey bobKey = SignKey.Generate();
            UserID bob = new(bobKey.PublicKey, "Bob");

            // Two distinct people
            Assert.AreNotEqual(alice, bob);

            // The twins (clones)
            UserID aliceClone = new(aliceKey.PublicKey, "Alice");
            Assert.AreEqual(alice, aliceClone);
            Assert.AreNotSame(alice, aliceClone);

            // Changed nickname, but still the same person
            UserID aliceDisguised = new(aliceKey.PublicKey, "Meghan");
            Assert.AreEqual(alice, aliceDisguised);
            Assert.AreNotSame(alice, aliceDisguised);

            // Bob attempts to look like Alice
            UserID bobDisguised = new(bobKey.PublicKey, "Alice");
            Assert.AreNotEqual(alice, bobDisguised);
            Assert.AreNotSame(alice, bobDisguised);
        }

        [Test]
        public void Dictionary()
        {
            SignKey aliceKey = SignKey.Generate();
            UserID alice = new(aliceKey.PublicKey, "Alice");

            SignKey bobKey = SignKey.Generate();
            UserID bob = new(bobKey.PublicKey, "Bob");

            Dictionary<UserID, int> keyValuePairs = new();

            keyValuePairs.Clear();
            keyValuePairs.Add(alice, 10);

            Assert.AreEqual(1, keyValuePairs.Count);
            Assert.AreEqual(10, keyValuePairs[alice]);

            keyValuePairs.Add(bob, 20);
            Assert.AreEqual(2, keyValuePairs.Count);
            Assert.AreEqual(20, keyValuePairs[bob]);

            Assert.Throws<ArgumentException>(() =>
            {
                keyValuePairs.Add(alice, 25);
            });

            keyValuePairs.Remove(alice);
            keyValuePairs.Add(alice, 30);

            Assert.AreEqual(2, keyValuePairs.Count);
            Assert.AreEqual(30, keyValuePairs[alice]);

            // Changed nickname, but still the same person.
            // Nickname have only informational value, only the Pubkey matters
            UserID aliceDisguised = new(aliceKey.PublicKey, "Meghan");
            keyValuePairs[aliceDisguised] = 40;

            Assert.AreEqual(2, keyValuePairs.Count);
            Assert.AreEqual(40, keyValuePairs[alice]);

            keyValuePairs.Remove(alice);
            Assert.AreEqual(1, keyValuePairs.Count);

            keyValuePairs.Remove(new(bobKey.PublicKey, null));
            Assert.AreEqual(0, keyValuePairs.Count);
        }
    }
}