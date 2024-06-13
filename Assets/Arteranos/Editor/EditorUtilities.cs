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
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arteranos
{
    public static class EditorUtilities
    {
        [MenuItem("Assets/Create Asset Preview image", true)]
        private static bool CreateAssetPreviewValidation()
        {
            if(!TryGetExportNameAndGameObjects(out _, out GameObject[] objs)) return false;
            foreach (var obj in objs)
                if (AssetDatabase.GetAssetPath(obj) == null) return false;
            return true;
        }

        [MenuItem("Assets/Create Asset Preview image", false, 20)]
        private static void CreateAssetPreviewMenu()
        {
            TryGetExportNameAndGameObjects(out string _, out GameObject[] objs);
            for (int i = 0; i < objs.Length; i++)
            {
                GameObject obj = objs[i];
                CreateAssetPreview(obj);
            }
        }

        private static void CreateAssetPreview(GameObject asset)
        {
            Texture2D assetPreview = null;
            for (int tmo = 0; tmo < 50; ++tmo)
            {
                if ((assetPreview = AssetPreview.GetAssetPreview(asset)) != null) break;
                System.Threading.Thread.Sleep(6);
            }

            // No preview within five minutes, something has to be wrong.
            if (assetPreview == null) return;

            // Turn all background pixels to transparent.
            Color blankPixel = assetPreview.GetPixel(0, 0);
            for (int x = 0; x < assetPreview.width; ++x)
                for (int y = 0; y < assetPreview.height; ++y)
                    if (assetPreview.GetPixel(x, y) == blankPixel)
                        assetPreview.SetPixel(x, y, Color.clear);

            string name = AssetDatabase.GetAssetPath(asset);
            name = $"{Path.GetDirectoryName(name)}/{asset.name}.png";

            byte[] data = assetPreview.EncodeToPNG();
            File.WriteAllBytes(name, data);

            AssetDatabase.ImportAsset(name);

            // THAT's how to modify the texture asset settings!
            TextureImporter ti = AssetImporter.GetAtPath(name) as TextureImporter;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
        }

        // As seen in glTFast.Editor....
        private static bool TryGetExportNameAndGameObjects(out string name, out GameObject[] gameObjects)
        {
            var transforms = Selection.GetTransforms(SelectionMode.Assets | SelectionMode.TopLevel);
            if (transforms.Length > 0)
            {
                name = transforms.Length > 1
                    ? SceneManager.GetActiveScene().name
                    : Selection.activeObject.name;

                gameObjects = transforms.Select(x => x.gameObject).ToArray();
                return true;
            }

            name = null;
            gameObjects = null;
            return false;
        }

    }
}