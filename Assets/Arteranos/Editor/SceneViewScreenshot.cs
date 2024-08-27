/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Arteranos
{
    public static class SceneViewScreenshot
    {
        [MenuItem("Arteranos/Build Scene View Screenshot", false, 1)]
        private static void CreateSveneViewScreenshot()
        {
            string name = $"Arteranos-Editor-{DateTime.Now:yyyyMMddHHmmss}.png";
            string picpath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string path = Path.Combine(picpath, name);
            using Stream fs = File.Create(path);

            EditorUtilities.TakeSceneViewPhoto(fs);
        }
    }
}