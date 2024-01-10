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
using Ipfs.Core.Cryptography.Proto;
using ProtoBuf;

namespace Arteranos.Core
{
    public partial class ServerDescription
    {
        private static string KnownPeersRoot => $"{FileUtils.persistentDataPath}/KnownPeers";
        public static string GetFileName(string id) 
            => $"{KnownPeersRoot}/{Utils.GetURLHash(id)}.description";


        public bool DBUpdate()
        {
            string fn = GetFileName(PeerID);
            string dir = Path.GetDirectoryName(fn);

            ServerDescription old = DBLookup(PeerID);

            // The stored entry is more recent than that we just got.
            if (old != null && old.LastModified > LastModified) return false;
            
            if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); }

            if(old != null) File.Delete(fn);

            using Stream stream = File.Create(fn);
            Serialize(stream);

            return true;
        }

        public static ServerDescription DBLookup(string id)
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

        public static IEnumerable<ServerDescription> DBList()
        {
            IEnumerable<string> files = Directory.EnumerateFiles(KnownPeersRoot, "*.description", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                ServerDescription sd = null;
                using Stream stream = File.OpenRead(file);
                sd = Deserialize(stream);

                if (sd != null) yield return sd;
            }
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