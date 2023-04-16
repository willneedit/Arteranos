/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Arteranos.Web
{
    public class WorldDownloader : MonoBehaviour
    {
        public async Task<string> UnzipWorldFile(string worldZipFile)
        {
            return await Task.Run(() =>
            {
                string path = Path.ChangeExtension(worldZipFile, "dir");

                if(Directory.Exists(path))
                    Directory.Delete(path, true);
                try
                {
                    ZipFile.ExtractToDirectory(worldZipFile, path);
                }
                catch(Exception ex)
                {
                    Debug.LogException(ex);
                    return null;
                }

                string archPath = "AssetBundles";
                RuntimePlatform p = Application.platform;
                if(p == RuntimePlatform.OSXEditor || p == RuntimePlatform.OSXPlayer)
                    archPath = "Mac";
                if(p == RuntimePlatform.Android)
                    archPath = "Android";

                path += "/" + archPath;

                if(!Directory.Exists(path))
                {
                    Debug.LogWarning($"No {archPath} directory in the zip file. Maybe it's the wrong world zip file or not at all.");
                    return null;
                }

                foreach(string file in Directory.EnumerateFiles(path, "*.unity"))
                    return file;


                Debug.LogWarning("No suitable AssetBundle found in the zipfile.");
                return null;
            });
        }


        public IEnumerator Download(string URL, string targetfile = null, Action<string> callback = null, bool reload = false)
        {
            targetfile ??= "somefile";
            string worldzip = $"{GetCacheDir(URL)}/{targetfile}";

            // reload means to skip the cache check and go straight on to the loading.
            if(!reload)
            {
                bool cacheResult = false;

                // https://stackoverflow.com/questions/20985022/nested-coroutines-using-ienumerator-vs-ienumerable-in-unity3d
                // Nested Coroutines? Hmmm....
                yield return StartCoroutine(CheckCached(URL, targetfile, (x) => cacheResult = x));

                if(cacheResult)
                {
                    callback?.Invoke(worldzip);
                    yield break;
                }  
            }

            DownloadHandlerFile dhf = new(worldzip);
            UnityWebRequest uwr = new(URL, UnityWebRequest.kHttpVerbGET, dhf, null);

            UnityWebRequestAsyncOperation uwr_ao = uwr.SendWebRequest();

            while(!uwr_ao.isDone)
            {
                Debug.Log($"Progress: {uwr_ao.progress}");
                yield return null;
            }

            if(uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.Log($"Download failure: {uwr.result}");

                // delete the 404-page (or any others), too.
                File.Delete(worldzip);
                callback?.Invoke(null);
                yield break;
            }

            callback?.Invoke(worldzip);
        }

        public IEnumerator CheckCached(string URL, string targetfile, Action<bool> callback = null)
        {
            string worldzip = $"{GetCacheDir(URL)}/{targetfile}";
            bool result = File.Exists(worldzip);

            // No local file at all.
            if(!result) 
            {
                callback?.Invoke(false);
                yield break;
            }

            FileInfo fi = new(worldzip);

            DateTime locDT = fi.LastWriteTime;
            long locSize = fi.Length;

            // Last write time is younger than 10 minutes; reduce network load
            // and require a forced cache flush.
            if((DateTime.Now - locDT) < TimeSpan.FromMinutes(10))
            {
                callback?.Invoke(true);
                yield break;
            }

            UnityWebRequest uwr = new(URL, UnityWebRequest.kHttpVerbHEAD);
            yield return uwr.SendWebRequest();

            // No network response, but we have a local file to work with it.
            if(uwr.result != UnityWebRequest.Result.Success)
            {
                callback?.Invoke(true);
                yield break;
            }

            DateTime netDT = DateTime.UnixEpoch;
            long netSize = -1;

            Dictionary<string, string> responseHeader = uwr.GetResponseHeaders();

            if(responseHeader.TryGetValue("Last-Modified", out string lmstr))
                netDT = DateTime.Parse(lmstr);

            if(responseHeader.TryGetValue("Content-Length", out string sizestr))
                netSize = long.Parse(sizestr);

            // It's outdated if it's newer in the net, or the reported size differs.
            bool outdated = (netDT > locDT) || (netSize != locSize);

            callback?.Invoke(!outdated);
        }

        private string GetCacheDir(string URL, bool create = false)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(URL);
            Hash128 hash = new();
            hash.Append(bytes);
            string result = hash.ToString();

            result = $"{Application.temporaryCachePath}/WorldCache/{result[0..2]}/{result[2..]}";

            if(create)
                Directory.CreateDirectory(result);

            return result;
        }

        public IEnumerator DownloadAndUnzip(string URL, Action<string> callback = null, bool reload = false)
        {
            string worldzippath = null;

            Debug.Log("Start downloading...");

            yield return StartCoroutine(Download(
                URL,
                "world.zip",
                (x) => worldzippath = x,
                reload));

            if(string.IsNullOrEmpty(worldzippath))
            {
                Debug.LogWarning("Download failure.");
                callback?.Invoke(null);
                yield break;
            }

            Debug.Log("Got the archived file, unzipping it...");

            Task<string> ao = UnzipWorldFile(worldzippath);

            // How to convert from async/await method to Coroutine usage...
            yield return new WaitUntil(() =>
                ao.Status == TaskStatus.RanToCompletion ||
                ao.Status == TaskStatus.Faulted);

            string result = ao.Result;

            Debug.Log($"Unzipping done, {result}");
            callback?.Invoke(result);
        }
        private void OnEnable()
        {
            StartCoroutine(DownloadAndUnzip(
                "https://github.com/willneedit/willneedit.github.io/blob/master/Abbey.zip?raw=true"
                // "file://C:/Users/willneedit/Desktop/world.zip"
                ));

        }
    }
}
