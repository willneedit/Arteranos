/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */


using ProtoBuf;
using System;
using System.Collections.Generic;

namespace Arteranos.WorldEdit
{
    [ProtoContract]
    public struct KitEntryItem
    {
        [ProtoMember(1)]
        public string Name;

        [ProtoMember(2)]
        public Guid GUID;

        public KitEntryItem(string name, Guid guid)
        {
            Name = name;
            GUID = guid;
        }
    }

    [ProtoContract]
    public struct KitEntryList
    {
        [ProtoMember(1)]
        public List<KitEntryItem> Items;
    }

}
