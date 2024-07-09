/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Services;
using Arteranos.WorldEdit;

namespace Arteranos
{
    public static class G
    {
        public static IWorldEditorData WorldEditorData { get; set; }

        public static ISceneLoader SceneLoader { get; set; }
    }
}