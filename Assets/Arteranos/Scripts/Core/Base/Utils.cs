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
using System.Text;
using Arteranos.Avatar;
using Arteranos.Social;
using Arteranos.XR;

namespace Arteranos.Core
{
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

        /// <summary>
        /// Converts an 'url-like' string to an URI, with zero assumption of the parts likw
        /// host and port.
        /// </summary>
        /// <param name="urilike">The (incomplete) URI</param>
        /// <param name="scheme"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="path"></param>
        /// <param name="query"></param>
        /// <param name="fragment"></param>
        /// <returns>A filled-out URI</returns>
        /// <exception cref="ArgumentNullException">Port is unknown nor implied.</exception>
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

        /// <summary>
        /// Get the user ID fingerprint's encoding according to visibility's User ID settings
        /// </summary>
        /// <param name="fpmodeSet">The setting in question (your, or the nameplate user's setting)</param>
        /// <returns>The CryptoHelper's ToString() parameter</returns>
        public static string GetUIDFPEncoding(UIDRepresentation fpmodeSet)
        {
            return fpmodeSet switch
            {
                UIDRepresentation.base64_8 => CryptoHelpers.FP_Base64_8,
                UIDRepresentation.base64_15 => CryptoHelpers.FP_Base64_15,
                UIDRepresentation.Dice_4 => CryptoHelpers.FP_Dice_4,
                UIDRepresentation.Dice_5 => CryptoHelpers.FP_Dice_5,
                _ => CryptoHelpers.FP_Base64
            };
        }

        /// <summary>
        /// Determine if you are able to do the action in the question (to the targeted user)
        /// </summary>
        /// <param name="cap">The action you want to do</param>
        /// <param name="target">The targeted user you want to do for (or, against)</param>
        /// <returns>Self explanatory.</returns>
        public static bool IsAbleTo(UserCapabilities cap, IAvatarBrain target)
        {
            // You are offline, it has to be your own computer.
            if(XRControl.Me == null) return true;

            return XRControl.Me.IsAbleTo(cap, target);
        }

        public struct Paginated<T>
        {
            public int page;
            public int maxPage;
            public T[] payload;
        }

        /// <summary>
        /// Chops an array of T into smaller chunks
        /// </summary>
        /// <typeparam name="T">The payload data type</typeparam>
        /// <param name="whole">the data to cut into pages</param>
        /// <param name="page">Current page, from 1 up to including maxPage, or 0 to only get the number of pages</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>the data</returns>
        public static Paginated<T> Paginate<T>(T[] whole, int page, int pageSize = 25)
        {
            int maxPage = (whole.Length + pageSize - 1) / pageSize;
            int ceil = page * pageSize;
            if(whole.Length < ceil) ceil = whole.Length;

            return new Paginated<T>()
            {
                page = page,
                maxPage = maxPage,
                payload = page <= maxPage && page > 0 
                ? whole[((page - 1) * pageSize)..(ceil)] 
                : null
            };  
        }
    }
}
