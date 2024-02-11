/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections.Generic;
using System.IO;
using ProtoBuf;

namespace Arteranos.Core
{
    [ProtoContract]
    public struct ListOfSerialized<T>
    {
        [ProtoMember(1)]
        public List<T> entries;

        public readonly byte[] Serialize()
        {
            using MemoryStream ms = new();
            Serializer.Serialize(ms, this);
            ms.Position = 0;
            return ms.ToArray();
        }

        public static ListOfSerialized<T> Deserialize(byte[] bytes)
        {
            using MemoryStream ms = new(bytes);
            return Serializer.Deserialize<ListOfSerialized<T>>(ms);
        }

    }
}