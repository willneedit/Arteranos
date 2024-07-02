using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Arteranos.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Arteranos.Core;
using Ipfs;
using System.Linq;
using System.Threading;
using System.Text;
using Ipfs.Cryptography.Proto;
using Arteranos.Core.Cryptography;
using System.Net;
using UnityEditor;
using Ipfs.Http;
using Ipfs.Unity;

namespace Arteranos.PlayTest.Services
{
    public class IPFS
    {
        IPFSServiceImpl service = null;

        [UnitySetUp]
        public IEnumerator Setup0()
        {
            TestFixtures.IPFSServiceFixture(ref service);

            yield return TestFixtures.StartIPFSAndWait(service);
        }

        [UnityTest]
        public IEnumerator TestFixtureTest()
        {
            yield return null;

            Assert.True(true);
        }

        // How to construct a directory without using temporary files.
        [UnityTest]
        public IEnumerator CreateDirectory()
        {
            List<IFileSystemLink> list = new();

            {
                using MemoryStream stream = new(Encoding.UTF8.GetBytes("Hello World B"));
                stream.Position = 0;
                yield return Asyncs.Async2Coroutine(IPFSService.AddStream(stream, "Beta.txt"), _fsn => list.Add(_fsn.ToLink()));
            }

            {
                using MemoryStream stream = new(Encoding.UTF8.GetBytes("Hello World A"));
                stream.Position = 0;
                yield return Asyncs.Async2Coroutine(IPFSService.AddStream(stream, "Alpha.txt"), _fsn => list.Add(_fsn.ToLink()));
            }

            Assert.AreEqual(2, list.Count);

            FileSystemNode fsn = null;
            yield return Asyncs.Async2Coroutine(IPFSService.Instance.Ipfs_.FileSystemEx.CreateDirectoryAsync(list), _fsn => fsn = _fsn);

            Assert.IsNotNull(fsn);
            Assert.AreEqual(2, fsn.Links.Count());

            List<IFileSystemLink> list2 = fsn.Links.ToList();
            Assert.AreEqual("Beta.txt", list2[0].Name);
            Assert.AreEqual("Alpha.txt", list2[1].Name);

            Assert.IsNotNull(fsn.Id);

            {
                string result = null;
                yield return Asyncs.Async2Coroutine(IPFSService.ReadBinary(fsn.Id + "/Alpha.txt"), _result => result = Encoding.UTF8.GetString(_result));
                Assert.AreEqual("Hello World A", result);
            }

            {
                string result = null;
                yield return Asyncs.Async2Coroutine(IPFSService.ReadBinary(fsn.Id + "/Beta.txt"), _result => result = Encoding.UTF8.GetString(_result));
                Assert.AreEqual("Hello World B", result);
            }
        }

    }
}