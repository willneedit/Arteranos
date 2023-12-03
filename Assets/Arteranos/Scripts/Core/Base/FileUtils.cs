/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Arteranos.Core
{
    public static class FileUtils
    {
        // Learned from work:
        // It's better to not to condition out the code, because it still always checks for errors
        // in compilation time, and those errors won't only pop up in the full release builds.
        //
        // Additionally you can switch the switch in runtime for debugging...
#if UNITY_SERVER
        public static bool Unity_Server = true;
#else
        public static bool Unity_Server = false;

#endif

        private static readonly string _persistentDataPath = Application.persistentDataPath;
        private static readonly string _temporaryCachePath = Application.temporaryCachePath;

#pragma warning disable IDE1006 // Benennungsstile
        public static string persistentDataPath
        { 
            get 
            {
                return Unity_Server
                    ? _persistentDataPath + "_DedicatedServer"
                    : _persistentDataPath;
            } 
        }

        public static string temporaryCachePath
        {
            get
            {
                return Unity_Server
                    ? _temporaryCachePath + "_DedicatedServer"
                    : _temporaryCachePath;
            }
        }
#pragma warning restore IDE1006 // Benennungsstile

        public static bool NeedsFallback(string path) 
            => Unity_Server && !File.Exists($"{persistentDataPath}/{path}");

        public static byte[] ReadBytesConfig(string path) => ReadConfig(path, File.ReadAllBytes);

        public static string ReadTextConfig(string path) => ReadConfig(path, File.ReadAllText);

        public static void WriteBytesConfig(string path, byte[] data) => WriteConfig(path, File.WriteAllBytes, data);

        public static Task WriteBytesConfigAsync(string path, byte[] data)
        {
            return WriteConfigAsync(path, (path, data) => File.WriteAllBytesAsync(path, data), data);
        }

        public static void WriteTextConfig(string path, string data) => WriteConfig(path, File.WriteAllText, data);

        public static Task WriteTextConfigAsync(string path, string data)
        {
            return WriteConfigAsync(path, (path, data) => File.WriteAllTextAsync(path, data), data);
        }

        public static T ReadConfig<T>(string path, Func<string, T> reader)
        {
            string fullPath = $"{persistentDataPath}/{path}";

            if (Unity_Server)
            {
                if (File.Exists(fullPath)) return reader(fullPath);
                Debug.LogWarning($"{fullPath} doesn't exist - falling back to regular file.");
            }

            fullPath = $"{Application.persistentDataPath}/{path}";
            return reader(fullPath);
        }

        private static void WriteConfig<T>(string path, Action<string, T> writer, T data)
        {
            string fullPath = $"{persistentDataPath}/{path}";
            if (Unity_Server && !Directory.Exists(persistentDataPath))
                Directory.CreateDirectory(persistentDataPath);

            writer(fullPath, data);
        }

        private static Task WriteConfigAsync<T>(string path, Func<string, T, Task> writer, T data)
        {
            string fullPath = $"{persistentDataPath}/{path}";
            if (Unity_Server && !Directory.Exists(persistentDataPath))
                Directory.CreateDirectory(persistentDataPath);

            return writer(fullPath, data);
        }
    }
}