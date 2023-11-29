/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Core;
using DERSerializer;
using System.IO;
using UnityEngine;
using Utils = Arteranos.Core.Utils;

namespace Arteranos.Web
{
    public class ServerGalleryImpl : MonoBehaviour, IServerGallery
    {
        private void Awake() => ServerGallery.Instance = this;
        private void OnDestroy() => ServerGallery.Instance = null;

        private static string GetMDFile(string url) => $"{Application.persistentDataPath}/ServerGallery/{Utils.GetURLHash(url)}.asn1";

        public ServerDescription? RetrieveServerSettings(string url)
        {
            try
            {
                byte[] der = File.ReadAllBytes(GetMDFile(url));
                return Serializer.Deserialize<ServerDescription>(der);
            }
            catch { }

            return null;
        }

        public void StoreServerSettings(string url, ServerDescription onlineData)
        {

            string metadataFile = GetMDFile(url);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(metadataFile));
                byte[] der = Serializer.Serialize(onlineData);
                File.WriteAllBytes(metadataFile, der);
            }
            catch { }
        }

        public void DeleteServerSettings(string url)
        {
            try { File.Delete(GetMDFile(url)); } catch { }
        }
    }
}
