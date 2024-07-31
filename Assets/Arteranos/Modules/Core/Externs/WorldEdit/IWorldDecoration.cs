/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using System.Collections;

namespace Arteranos.WorldEdit
{
    public interface IWorldDecoration
    {
        WorldInfoNetwork Info { get; set; }

        IEnumerator BuildWorld();
    }
}