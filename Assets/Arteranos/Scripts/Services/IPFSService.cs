/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Ipfs;
using Ipfs.Engine;
using System.IO;
using Newtonsoft.Json.Linq;
using Arteranos.Core;
using System.Threading.Tasks;
using System.Threading;

namespace Arteranos.Services
{
    public class IPFSService : MonoBehaviour
    {
        const string passphrase = "this is not a secure pass phrase";

        public IpfsEngine Ipfs { get => ipfs; }

        private IpfsEngine ipfs = null;

        private CancellationTokenSource cts = null;

        private async void Start()
        {
            cts = new();

            ipfs = new(passphrase.ToCharArray());

            ipfs.Options.Repository.Folder = Path.Combine(FileUtils.persistentDataPath, "IPFS");
            ipfs.Options.KeyChain.DefaultKeyType = "ed25519";
            ipfs.Options.KeyChain.DefaultKeySize = 2048;
            await ipfs.Config.SetAsync(
                "Addresses.Swarm",
                JToken.FromObject(new string[] { "/ip4/0.0.0.0/tcp/12345" })
            );

            await ipfs.StartAsync().ConfigureAwait(false);
        }

        private async void OnDestroy()
        {
            await ipfs.StopAsync().ConfigureAwait(false);

            cts.Cancel();

            cts.Dispose();
        }
    }
}
