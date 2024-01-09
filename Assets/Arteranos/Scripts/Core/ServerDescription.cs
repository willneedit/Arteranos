/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.IO;
using Arteranos.Core.Cryptography;
using Ipfs.Core.Cryptography.Proto;
using ProtoBuf;

namespace Arteranos.Core
{
    public partial class _ServerDescription : IEquatable<_ServerDescription>
    {
        private static string KnownPeersRoot => $"{FileUtils.persistentDataPath}/KnownPeers";
        public static string GetFileName(string id) 
            => $"{KnownPeersRoot}/{Utils.GetURLHash(id)}";


        public bool DBUpdate()
        {
            string fn = GetFileName(PeerID);
            string dir = Path.GetDirectoryName(fn);

            _ServerDescription old = DBLookup(PeerID);

            // The stored entry is more recent than that we just got.
            if (old != null && old.LastModified > LastModified) return false;
            
            if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); }

            if(old != null) File.Delete(fn);

            using Stream stream = File.Create(fn);
            Serialize(stream);

            return true;
        }

        public static _ServerDescription DBLookup(string id)
        {
            string fn = GetFileName(id);

            if (!File.Exists(fn)) return null;

            using Stream stream = File.OpenRead(fn);
            return Deserialize(stream);
        }

        public static void DBDelete(string id)
        {
            string fn = GetFileName(id);

            if (!File.Exists(fn)) return;

            File.Delete(fn);
        }

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

        public static _ServerDescription Deserialize(PublicKey serverPublicKey, Stream stream)
        {
            _ServerDescription d = Serializer.Deserialize<_ServerDescription>(stream);
            byte[] signature = d.signature;
            using (MemoryStream ms = new())
            {
                d.signature = null;
                Serializer.Serialize(ms, d);
                ms.Position = 0;
                serverPublicKey.Verify(ms.ToArray(), signature);
            }

            return d;
        }

    }
}