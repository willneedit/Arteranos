/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */


using Arteranos.Core;
using System;
using Newtonsoft.Json;

namespace Arteranos.WorldEdit
{
    public class KitMetaData
    {
        public string KitName = "Unnamed Kit";
        public string KitDescription = string.Empty;
        public UserID AuthorID = null;
        public DateTime Created = DateTime.MinValue;

        public string Serialize() => JsonConvert.SerializeObject(this, Formatting.Indented);

        public static KitMetaData Deserialize(string json) => JsonConvert.DeserializeObject<KitMetaData>(json);
    }
}
