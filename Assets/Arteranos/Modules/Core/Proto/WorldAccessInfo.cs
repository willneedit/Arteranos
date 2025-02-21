/*
 * Copyright (c) 2025, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using ProtoBuf;
using System;
using System.Collections.Generic;

namespace Arteranos.Core
{
    // Ref. #89 - World access control
    [ProtoContract]
    public class WorldAccessInfo
    {
        [ProtoMember(1)]
        public string Password; // Sent from author to the server, server asks the visitors
        
        [ProtoMember(2)]
        public List<UserID> BannedUsers; // Self explanatory. Overrides everthing.

        [ProtoMember(3)]
        public bool Viewable;           // if false, only users listed below can view

        [ProtoMember(4)]
        public bool FriendsCanPin;      // On creation/Updating: Friends can favourite

        [ProtoMember(5)]
        public bool FriendsCanEdit;     // On creation/Updating: Friends can edit

        [ProtoMember(6)] 
        public bool FriendsCanView;     // On creation/Updating: Friends can view

        [ProtoMember(7)]
        public List<UserID> UserCanPin; // Additional users who can favourite

        [ProtoMember(8)]
        public List<UserID> UserCanEdit;// Additional users who can edit

        [ProtoMember(9)]
        public List<UserID> UserCanView;// Additional users who can view;

        [ProtoMember(10)]
        public UserID AccessAuthor;     // Creator of this data, generally the world creator

        [ProtoMember(11)]
        public byte[] Signature;        // Against AccessAuthor's signing key
    }
}