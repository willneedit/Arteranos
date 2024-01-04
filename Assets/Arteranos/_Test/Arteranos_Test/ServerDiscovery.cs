/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Ipfs;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Debug = UnityEngine.Debug;
using Arteranos.Services;
using Ipfs.Engine;
using UnityEngine.TestTools;
using UnityEngine;
using System.Collections;

namespace Arteranos.Test
{
    [TestFixture]
    class ServerDiscovery
    {
        const string V140CID = "Qmb8iAjBmoiKF89VQ4grYRogs4zqYDPMGPwEyX6EwFcidp";

        /// <summary>
        /// The simple file transfer.
        /// </summary>
        [Test]
        public void AdvertiseServerAsync()
        {
            Task.Run(AdvertiseServer).Wait();
        }

        public async Task AdvertiseServer()
        {
            using TempNode server = new();
            using TempNode client = new();

            await server.StartAsync();
            await client.StartAsync();

            // Server puts up the file with the version_min contents
            IFileSystemNode fsn = await server.FileSystem.AddTextAsync(Core.Version.VERSION_MIN);
            Cid cid = fsn.Id;

            MultiAddress address = (await server.LocalPeer).Addresses.First();


            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));

            // Client would see the indicated file (or files)

            // Local clean up
            try
            {
                await client.Block.RemoveAsync(cid);
            } catch { }

            Debug.Log($"Version = {Core.Version.VERSION_MIN}, Cid = {cid}");
            Debug.Log($"Server address = {address}");

            // Version 1.4.0 Cid
            Assert.AreEqual(cid.ToString(), V140CID);

            await client.Swarm.ConnectAsync(address, cts.Token);
            var content = await client.FileSystem.ReadAllTextAsync(cid, cts.Token);
            Assert.AreEqual(content, Core.Version.VERSION_MIN);
        }

        /// <summary>
        /// Find the providers to spot the indicative version files and tell the Arteranos servers apart.
        /// </summary>
        [Test]
        public void DiscoverServersAsync()
        {
            Task.Run(DiscoverServers).Wait();
        }

        public async Task DiscoverServers()
        {
            using TempNode server = new();
            using TempNode somewhere = new();
            using TempNode client = new();

            await server.StartAsync();
            await somewhere.StartAsync();
            await client.StartAsync();

            // Server puts up the file with the version_min contents
            IFileSystemNode fsn = await server.FileSystem.AddTextAsync(Core.Version.VERSION_MIN);
            Cid cid = fsn.Id;

            MultiAddress address = (await server.LocalPeer).Addresses.First();
            var self = await server.LocalPeer;

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));

            // Simulate the indirect connections
            await somewhere.Swarm.ConnectAsync(address, cts.Token);
            MultiAddress address2 = (await somewhere.LocalPeer).Addresses.First();

            await client.Swarm.ConnectAsync(address2, cts.Token);

            IEnumerable<Peer> peers = await client.Dht.FindProvidersAsync(V140CID, limit: 1, cancel: cts.Token);
            Assert.AreEqual(1, peers.Count());
            Assert.AreEqual(peers.First(), self);
        }

        /// <summary>
        /// Find the server only with its ID
        /// </summary>
        [Test]
        public void FindServerByIdAsync()
        {
            Task.Run(FindServerById).Wait();
        }

        public async Task FindServerById()
        {
            using TempNode server = new();
            using TempNode somewhere = new();
            using TempNode client = new();

            await server.StartAsync();
            await somewhere.StartAsync();
            await client.StartAsync();

            MultiAddress address = (await server.LocalPeer).Addresses.First();
            var self = await server.LocalPeer;
            string id = self.Id.ToString();
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));

            // Simulate the indirect connections
            await somewhere.Swarm.ConnectAsync(address, cts.Token);
            MultiAddress address2 = (await somewhere.LocalPeer).Addresses.First();

            await client.Swarm.ConnectAsync(address2, cts.Token);

            Peer servercontact = await client.Dht.FindPeerAsync(id, cts.Token);
            Assert.IsNotNull(servercontact);
            Assert.IsTrue(servercontact.IsValid());

            Assert.AreEqual(servercontact.Id.ToString(), id);

            // server is not directly connected to client
            Assert.IsNull(servercontact.ConnectedAddress);

            // But I have a list of possible contacts
            Assert.IsTrue(servercontact.Addresses.Count() > 0);

            foreach(MultiAddress addr in servercontact.Addresses)
                Debug.Log($"{addr}");
        }

        [Test]
        public void ConnectFromOutsideAsync()
        {
            Task.Run(ConnectFromOutside).Wait();
        }

        public async Task ConnectFromOutside()
        {
            using TempNode server = new(12345); // Fixed port, no hole punching as of yet
            await server.StartAsync();

            using TempNode outsider = new();
            await outsider.StartAsync();

            Peer self = await server.LocalPeer;
            Peer other = await outsider.LocalPeer;

            // External IP and port, beyond the NAT
            string outsideAddress = $"/ip4/10.72.172.2/tcp/12345/ipfs/{self.Id}";

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));

            await outsider.Swarm.ConnectAsync(outsideAddress, cts.Token);

            Peer[] serverspeers = (await server.Swarm.PeersAsync()).ToArray();

            Assert.AreEqual(1, serverspeers.Length);
            Assert.AreEqual(serverspeers[0].Id, other.Id);

            foreach (MultiAddress address in serverspeers[0].Addresses)
                Debug.Log($"Outsider's addresses, as seen by server: {address}");

            foreach (MultiAddress address in other.Addresses)
                Debug.Log($"Outsider's addresses, as seen by itself: {address}");
        }
    }
}