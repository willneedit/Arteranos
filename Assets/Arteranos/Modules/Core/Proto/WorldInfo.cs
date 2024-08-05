﻿/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Ipfs;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;


namespace Arteranos.Core
{
    [ProtoContract]
    public class WorldInfoNetwork
    {
        [ProtoMember(1)]
        public string WorldCid;

        [ProtoMember(2)]
        public string WorldName;

        [ProtoMember(3)]
        public string WorldDescription;

        //[ProtoMember(4)]
        //public string AuthorNickname;

        //[ProtoMember(5)]
        //public byte[] AuthorPublicKey;

        [ProtoMember(6)]
        public ServerPermissions ContentRating;

        [ProtoMember(7)]
        public byte[] Signature;

        [ProtoMember(8)]
        public byte[] ScreenshotPNG;

        [ProtoMember(9)]
        public DateTime Created;

        [ProtoMember(10)]
        public UserID Author;

    }

    [ProtoContract]
    public partial class WorldInfo
    {

        // The WorldInfoNetwork stays immutable, because its Cid needs to remain contant.
        [ProtoMember(1)]
        public WorldInfoNetwork win;

        [ProtoMember(3)]
        public DateTime Updated;


        public void Serialize(Stream stream)
            => Serializer.Serialize(stream, this);

        public static WorldInfo Deserialize(Stream stream)
            => Serializer.Deserialize<WorldInfo>(stream);

        public WorldInfo Strip()
        {
            WorldInfo stripped = new()
            { 
                win = win,
                Updated = Updated
            };

            stripped.win = new()
            {
                WorldCid = win.WorldCid,
                WorldName = win.WorldName,
                WorldDescription = win.WorldDescription,
                Signature = win.Signature,
                ScreenshotPNG = null, // Leave the screenshot for brevity
                Created = win.Created,
                Author = win.Author,
                ContentRating = win.ContentRating,
            };

            return stripped;
        }
    }
}