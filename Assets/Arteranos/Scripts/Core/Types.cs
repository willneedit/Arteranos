/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using UnityEngine;
using System;

using System.ComponentModel;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace Arteranos.Core
{
    public static class Extensions
    {
        /// <summary>
        /// Returns a relevance index for the comparison.
        /// </summary>
        /// <param name="setting">The server settings</param>
        /// <param name="user">The user's search filter</param>
        /// <returns>5 for an exact determinate match, 1 for an inexact match, 0 for a mismatch</returns>
        public static int FuzzyEq(this bool? setting, bool? user)
        {
            if(setting == null) return 1;

            return !setting != user ? 5 : 0;
        }
    }

    public static class TransformExtensions
    {
        /// <summary>
        /// Finds the transform in the hierarchy tree by name, including searching the
        /// entire subtree below.
        /// </summary>
        /// <param name="t">The transform to begin searching</param>
        /// <param name="name">The transform's name to search for</param>
        /// <returns>The first found transform, otherwise null</returns>
        public static Transform FindRecursive(this Transform t, string name)
        {
            if(t.name == name) return t;

            for(int i = 0, c = t.childCount; i < c; i++)
            {
                Transform res = FindRecursive(t.GetChild(i), name);
                if(res != null) return res;
            }

            return null;
        }

        /// <summary>
        /// Performs a movement over time to a dedicated transform, like with a camera movement
        /// in a cutscene.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="targetTransform"></param>
        /// <param name="duration"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task LerpTransform(this Transform transform,
            Transform targetTransform, float duration, CancellationToken token)
        {
            // ctx?.Cancel();
            // ctx = new CancellationTokenSource();

            float time = 0f;
            Vector3 startPosition = transform.localPosition;
            Quaternion startRotation = transform.localRotation;
            Vector3 startScale = transform.localScale;

            while(time < duration && !token.IsCancellationRequested)
            {
                float t = time / duration;

                t = 0.5f - (float) Mathf.Cos(t * Mathf.PI) * 0.5f;
                transform.localPosition = Vector3.Lerp(startPosition, targetTransform.localPosition, t);
                transform.localRotation = Quaternion.Lerp(startRotation, targetTransform.localRotation, t);
                transform.localScale = Vector3.Lerp(startScale, targetTransform.localScale, t);
                time += Time.deltaTime;
                await Task.Yield();
            }

            if(!token.IsCancellationRequested)
            {
                transform.localPosition = targetTransform.localPosition;
                transform.localRotation = targetTransform.localRotation;
                transform.localScale = targetTransform.localScale;
            }
        }
    }

    public static class Utils
    {
        /// <summary>
        /// Allows to tack on a Description attribute to enum values, e.g. a display name.
        /// </summary>
        /// <param name="enumVal">The particular value of the enum set</param>
        /// <returns>The string in the value's description, null if there isn't</returns>
        public static string GetEnumDescription(Enum enumVal)
        {
            MemberInfo[] memInfo = enumVal.GetType().GetMember(enumVal.ToString());
            DescriptionAttribute attribute = CustomAttributeExtensions.GetCustomAttribute<DescriptionAttribute>(memInfo[0]);
            return attribute?.Description;
        }

        /// <summary>
        /// Generate a directory of a hashed URL suitable for a cache directory tree.
        /// </summary>
        /// <param name="url">The URL</param>
        /// <returns>the two directory levels, without a root path</returns>
        public static string GetURLHash(string url)
        {
            Hash128 hash = new();
            byte[] bytes = Encoding.UTF8.GetBytes(url);
            hash.Append(bytes);
            string hashstr = hash.ToString();

            string hashed = $"{hashstr[0..2]}/{hashstr[2..]}";
            return hashed;
        }

        /// <summary>
        /// Simulate a RC circuit (a capacitor and resistor) to measure the capacitor's charge,
        /// used in for example a VU meter. 
        /// </summary>
        /// <param name="value">Current input voltage</param>
        /// <param name="charge">The resulting charge</param>
        /// <param name="kCharge">The charging factor</param>
        /// <param name="kDischarge">The discharging factor</param>
        public static void CalcVU(float value, ref float charge, float kCharge = 0.1f, float kDischarge = 0.05f)
        {
            value = Mathf.Abs(value);

            if(value > charge)
                charge = (charge * (1 - kCharge)) + (value * kCharge);
            else
                charge *= (1 - kDischarge);
        }

        /// <summary>
        /// Fout = 10^(Q/20) * Fin
        /// </summary>
        /// <param name="dBvalue"></param>
        /// <returns>Ife plain factor.</returns>
        public static float LoudnessToFactor(float dBvalue) => MathF.Pow(10.0f, dBvalue / 10.0f);

        public static Uri ProcessUriString(string urilike,
                        string scheme = null,
                        string host = null,
                        int? port = null,
                        string path = null,
                        string query = null,
                        string fragment = null
)
        {
            urilike = urilike.Trim();

            if(!urilike.Contains("://"))
                urilike = "unknown://" + urilike;

            Uri uri = new(urilike);

            if(uri.Port >= 0)
                port = uri.Port;

            if(port == null)
                throw new ArgumentNullException("No port");

            string sb = string.IsNullOrEmpty(host ?? uri.Host)
                ? $"{scheme ?? uri.Scheme}://"
                : string.IsNullOrEmpty(uri.UserInfo)
                    ? $"{scheme ?? uri.Scheme}://{host ?? uri.Host}:{port}"
                    : $"{scheme ?? uri.Scheme}://{uri.UserInfo}@{host ?? uri.Host}:{port}";

            sb += uri.AbsolutePath == "/"
                ? path ?? "/"
                : uri.AbsolutePath;

            sb += string.IsNullOrEmpty(uri.Query)
                ? query ?? string.Empty
                : uri.Query;

            sb += string.IsNullOrEmpty(uri.Fragment)
                ? fragment ?? string.Empty
                : uri.Fragment;

            return new(sb);
        }
    }

    /// <summary>
    /// Public server meta data with the connection data and the privileges
    /// </summary>
    public class ServerMetadataJSON
    {
        public ServerSettingsJSON Settings = null;
        public string CurrentWorld = null;
        public List<string> CurrentUsers = new();
    }

    public class WorldMetaData
    {
        public const string PATH_METADATA_DEFAULTS = "MetadataDefaults.json";

        public string WorldName = "Unnamed World";
        public string Author = "Anonymous";
        public DateTime Created = DateTime.MinValue;
        public DateTime Updated = DateTime.MinValue;

        public void SaveDefaults()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText($"{Application.persistentDataPath}/{PATH_METADATA_DEFAULTS}", json);
            }
            catch(Exception ex)
            {
                Debug.LogWarning($"Failed to save the metadata defaults: {ex.Message}");
            }
        }

        public static WorldMetaData LoadDefaults()
        {
            WorldMetaData mdj;
            try
            {
                string json = File.ReadAllText($"{Application.persistentDataPath}/{PATH_METADATA_DEFAULTS}");
                mdj = JsonConvert.DeserializeObject<WorldMetaData>(json);
            }
            catch(Exception ex)
            {
                Debug.LogWarning($"Failed to load the metadata defaults: {ex.Message}");
                mdj = new();
            }

            return mdj;
        }

        public string Serialize() => JsonConvert.SerializeObject(this, Formatting.Indented);

        public static WorldMetaData Deserialize(string json) => JsonConvert.DeserializeObject<WorldMetaData>(json);
    }

    /// <summary>
    /// Suitable for locks/unlocks to properly deallocate resources when the control flow
    /// leaves the scope, be it regular or by an exception.
    /// 
    /// Credits go for C++ :)
    /// Best use with
    ///             using(Guard guard = new(allocate, release)) { ... }
    /// </summary>
    public class Guard : IDisposable
    {
        private readonly Action disengage;

        private bool _disposedValue;

        public Guard(Action engage, Action disengage)
        {
            this.disengage = disengage;
            engage();
        }

        ~Guard() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(_disposedValue) return;

            //if(disposing)
            //{
            //    // Needed? Dispose managed state (managed objects).
            //}

            disengage();
            _disposedValue = true;
        }
    }
}
