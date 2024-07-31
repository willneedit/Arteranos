/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;
using System.IO;
using Arteranos.Core.Cryptography;
using Ipfs.Cryptography.Proto;
using ProtoBuf;

namespace Arteranos.Core
{
    public partial class ServerDescription : FlatFileDB<ServerDescription>
    {
        public ServerDescription() 
        {
            _KnownPeersRoot = $"{FileUtils.persistentDataPath}/KnownPeers";
            _GetFileName = id => $"{FileUtils.persistentDataPath}/KnownPeers/{Utils.GetURLHash(id)}.description";
            _SearchPattern = "*.description";
            _Deserialize = Deserialize;
            _Serialize = Serialize;
        }

        public bool DBUpdate() 
            => _DBUpdate(PeerID, old => old.LastModified <= LastModified);

        public static ServerDescription DBLookup(string id) 
            => new ServerDescription()._DBLookup(id);

        public static void DBDelete(string id) 
            => new ServerDescription()._DBDelete(id);

        public static IEnumerable<ServerDescription> DBList() 
            => new ServerDescription()._DBList();

        public void Serialize(SignKey serverPrivateKey, Stream stream)
        {
            // Sign the structure with the empty signature field
            using (MemoryStream ms = new())
            {
                signature = null;
                Serializer.Serialize(ms, this);
                ms.Position = 0;
                serverPrivateKey.Sign(ms.ToArray(), out signature);
            }

            Serializer.Serialize(stream, this);
            stream.Flush();
        }

        public static ServerDescription Deserialize(PublicKey serverPublicKey, Stream stream)
        {
            ServerDescription d = Serializer.Deserialize<ServerDescription>(stream);
            byte[] signature = d.signature;
            using (MemoryStream ms = new())
            {
                d.signature = null;
                Serializer.Serialize(ms, d);
                ms.Position = 0;
                serverPublicKey.Verify(ms.ToArray(), signature);

                // Restore the signature, to re-serialize without PeerID's private key.
                d.signature = signature;
            }

            return d;
        }

    }
}