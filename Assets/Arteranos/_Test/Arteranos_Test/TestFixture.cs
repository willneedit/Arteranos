/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Services;
using Ipfs.Engine;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


namespace Arteranos.Test
{
    public class TestFixture
    {
    }

    class TempNode : IpfsEngine
    {
        static int nodeNumber;

        public TempNode(int port = 0)
            : base("xyzzy".ToCharArray())
        {
            Options.Repository.Folder = Path.Combine(Path.GetTempPath(), $"ipfs-{nodeNumber++}");
            Options.KeyChain.DefaultKeyType = "ed25519";

            Config.SetAsync(
                "Addresses.Swarm",
                JToken.FromObject(new string[] { $"/ip4/0.0.0.0/tcp/{port}" })
            ).Wait();

            Options.Discovery.DisableMdns = true;
            Options.Swarm.MinConnections = 0;
            Options.Swarm.PrivateNetworkKey = null;
            Options.Discovery.BootstrapPeers = new Ipfs.MultiAddress[0];
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (Directory.Exists(Options.Repository.Folder))
            {
                Directory.Delete(Options.Repository.Folder, true);
            }
        }
    }

}
