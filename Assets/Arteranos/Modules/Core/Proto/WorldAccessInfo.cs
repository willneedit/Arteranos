/*
 * Copyright (c) 2025, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using ProtoBuf;
using System.Collections.Generic;
using System.IO;

namespace Arteranos.Core
{
    // Ref. #89 - World access control
    [ProtoContract]
    public class WorldAccessInfo
    {
        // Set on changing world on server
        // Cleared on propagating from server to visitors
        // NOT INCLUDED in signature, because it's only between the uploader and the server
        [ProtoMember(1)]
        public string Password; // Sent from author to the server, server asks the visitors
        
        [ProtoMember(2)]
        public List<UserID> BannedUsers; // Self explanatory. Overrides everthing.

        [ProtoMember(3)]
        public bool EveryoneCanPin;     // On creation/Updating: Everyone can favourite

        [ProtoMember(4)]
        public bool EveryoneCanEdit;    // On creation/Updating: Everyone can edit

        [ProtoMember(5)] 
        public bool EveryoneCanView;    // On creation/Updating: Everyone can view

        [ProtoMember(6)]
        public List<UserID> UserCanAdmin; // Users who can change access rights.

        [ProtoMember(7)]
        public List<UserID> UserCanPin; // Users who can favourite

        [ProtoMember(8)]
        public List<UserID> UserCanEdit;// Users who can edit

        [ProtoMember(9)]
        public List<UserID> UserCanView;// Users who can view;

        [ProtoMember(10)]
        public UserID AccessAuthor;     // Creator of this data, the world creator or the delegates

        [ProtoMember(11)]
        public byte[] Signature;        // Against AccessAuthor's signing key

        [ProtoMember(12)]
        public UserID WorldAuthor;      // The world creator.

        public void Serialize(Stream stream, bool changeAuthor = false)
        {
            // No self lock-out, no malicious admin hijacking the author's works.
            if (UserCanAdmin == null)
                UserCanAdmin = new();

            if(UserCanAdmin.Contains(WorldAuthor)) 
                UserCanAdmin.Add(WorldAuthor);

            if (changeAuthor)
            {
                string tmpPassword = Password;
                AccessAuthor = G.Client.MeUserID;
                Signature = null;
                Password = null;

                using (MemoryStream ms = new())
                {
                    Serializer.Serialize(ms, this);
                    ms.Position = 0;
                    Client.Sign(ms.ToArray(), out Signature);
                }

                Password = tmpPassword;
            }
            else if (AccessAuthor == null)
                throw new InvalidDataException("World Access Info without Author ID");

            Serializer.Serialize(stream, this);
            stream.Flush();
        }

        public static WorldAccessInfo Deserialize(byte[] data)
        {
            WorldAccessInfo wai = Serializer.Deserialize<WorldAccessInfo>(new MemoryStream(data));

            using (MemoryStream ms = new()) 
            {
                byte[] signature = wai.Signature;
                string tmpPassword = wai.Password;
                wai.Signature = null;
                wai.Password = null;

                Serializer.Serialize(ms, wai);
                ms.Position = 0;

                // Throw if invalid signature
                wai.AccessAuthor.SignPublicKey.Verify(ms.ToArray(), signature);

                // Throw if key is not on approved list
                if (!wai.UserCanAdmin.Contains(wai.AccessAuthor))
                    throw new InvalidDataException("World Access data is owned by neither the world creator nor users who can admin.");

                wai.Password = tmpPassword;
            }

            return wai;
        }
    }
}