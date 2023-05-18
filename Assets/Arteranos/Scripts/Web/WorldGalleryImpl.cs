/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using System.IO;
using UnityEngine;

namespace Arteranos.Web
{
    public class WorldGalleryImpl : MonoBehaviour, IWorldGallery
    {
        private static string GetRootPath(string url, bool cached)
        {
            return cached
                ? $"{Application.temporaryCachePath}/WorldCache/{Utils.GetURLHash(url)}/world.dir"
                : $"{Application.persistentDataPath}/WorldGallery/{Utils.GetURLHash(url)}";
        }

        private void Awake() => WorldGallery.Instance = this;
        private void OnDestroy() => WorldGallery.Instance = null;

        public (string, string) RetrieveWorld(string url, bool cached = false)
        {
            string rootPath = GetRootPath(url, cached);

            if(!Directory.Exists(rootPath)) return (null, null);

            string metadataFile = $"{rootPath}/Metadata.json";

            if(!File.Exists(metadataFile)) metadataFile = null;

            string screenshotFile = null;
            foreach(string file in Directory.EnumerateFiles(rootPath, "Screenshot.*"))
            {
                // In Unity, we use forward slash.
                screenshotFile = $"{rootPath}/{Path.GetFileName(file)}";
                break;
            }    

            return (metadataFile, screenshotFile);
        }

        public WorldMetaData RetrieveWorldMetaData(string url)
        {
            string metadatafile;

            (metadatafile, _) = RetrieveWorld(url, false);

            if(metadatafile == null)
                (metadatafile, _) = RetrieveWorld(url, true);

            if(metadatafile == null)
                return null;

            string json = File.ReadAllText(metadatafile);
            return WorldMetaData.Deserialize(json);
        }

        public void StoreWorldMetaData(string url, WorldMetaData worldMetaData)
        {
            string metadatafile;

            (metadatafile, _) = RetrieveWorld(url, false);

            if(metadatafile == null)
                (metadatafile, _) = RetrieveWorld(url, true);

            if(metadatafile == null)
                throw new FileNotFoundException("Unknown worls URL for the given data");

            string json = worldMetaData.Serialize();
            File.WriteAllText(metadatafile, json);
        }

        public bool StoreWorld(string url)
        {
            string metadataFile;
            string screenshotFile;

            (metadataFile, screenshotFile) = RetrieveWorld(url, true);

            // Nothing at all?
            if(string.IsNullOrEmpty(metadataFile) && string.IsNullOrEmpty(screenshotFile)) return false;

            string rootPath = GetRootPath(url, false);

            Directory.CreateDirectory(rootPath);

            if(metadataFile != null)
                File.Copy(metadataFile, $"{rootPath}/Metadata.json");

            if(screenshotFile != null)
                File.Copy(screenshotFile, $"{rootPath}/{Path.GetFileName(screenshotFile)}");

            return true;
        }

        public void DeleteWorld(string url)
        {
            string rootPath = GetRootPath(url, false);

            if(Directory.Exists(rootPath)) Directory.Delete(rootPath, true);

            rootPath = GetRootPath(url, true);

            if(Directory.Exists(rootPath)) Directory.Delete(rootPath, true);
        }
    }
}
