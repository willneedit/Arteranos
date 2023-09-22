/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using System.ComponentModel;

namespace Arteranos.Core
{
    public class UserState : Bit64field
    {
        // Just an ordinary everyday user.
        public const ulong Normal = 0;

        // ---------------------------------------------------------------
        // 0  ... 15: User related

        // Trusted user, whatever it shall be... ¯\_(ツ)_/¯
        public const ulong Trusted = (ulong) 1 << 0;

        // ---------------------------------------------------------------
        // 16 ... 31: World related

        // Derived from the world administrator, can elevate or demote assistants with the same powers
        public const ulong World_admin_asstnt = (ulong) 1 << 30;

        // World administrators
        public const ulong World_admin = (ulong) 1 << 31;

        // ---------------------------------------------------------------
        // 32 ... 47: Server related

        // Derived from the world administrator, can elevate or demote assistants with the same powers
        public const ulong Srv_admin_asstnt = (ulong) 1 << 46;

        // Server administrators
        public const ulong Srv_admin = (ulong) 1 << 47;


        // ---------------------------------------------------------------
        // 48 ... 63: Repercussions, especially with the regative result (= banned bit set)

        // Below the user's legal or recommended age, e.g. below 13 b/c COPPA.
        public const ulong Underage = (ulong) 1 << 48;

        // Disruptive behavior
        public const ulong Trolling = (ulong) 1 << 49;

        // Discrimination/-phobic/hatemongering (see below)
        public const ulong Hating = (ulong) 1 << 50;

        // Incessantly loud (e.g. noisy mic) (Maybe automatically force-muted on login?)
        public const ulong Loud = (ulong) 1 << 51;

        // Mobbing/Bullying/Harassment
        public const ulong Bullying = (ulong) 1 << 52;

        // Undesired sexual advances or harassment
        public const ulong SxHarassment = (ulong) 1 << 53;

        // Exploit/security leak usage (maybe tripped in the network stack)
        public const ulong Exploiting = (ulong) 1 << 60;

        // Impersonation (to gain privileges by social engineering)
        public const ulong Impersonation = (ulong) 1 << 61;

        // Attempted ban evasion
        public const ulong BanEvading = (ulong) 1 << 62;

        // User has been banned
        public const ulong Banned = (ulong) 1 << 63;

        // All of the 'good' bits.
        public const ulong GOOD_MASK = ((ulong)1 << 48) - 1;

        public static bool IsBanned(ulong field) => IsAny(field, Banned);

        public static bool IsWAdmin(ulong field) => IsAny(field, World_admin | World_admin_asstnt);

        public static bool IsSAdmin(ulong field) => IsAny(field, Srv_admin | Srv_admin_asstnt);
    }

    public class UserPrivacy
    {
        public UserVisibility UIDVisibility;
        public UIDRepresentation UIDRepresentation;
        public UserVisibility TextReception;
    }

    public enum UserVisibility
    {
        [Description("everyone")]
        everyone = 0,
        [Description("friends only")]
        friends = 1,
        [Description("no one")]
        none = 2
    }

    public enum UIDRepresentation
    {
        [Description("8 characters")]
        base64_8,
        [Description("15 characters")]
        base64_15,
        [Description("4 words")]
        Dice_4,
        [Description("5 words")]
        Dice_5
    }

    public class ServerUserState : UserState
    {
        public UserID userID;
        public ulong userState;
        public string address;
        public string deviceUID;
        public string remarks;
    }

    public class ServerUserBase
    {
        public List<ServerUserState> Base = new();

        public const string PATH_SERVER_USERBASE = "ServerUserBase.json";

        public static bool MatchElement<T>(T entry, T query) 
            => query == null || query.Equals(entry);

        public static bool Match(ServerUserState entry, ServerUserState query)
        {
            return
                MatchElement(entry.userID, query.userID) &&
                MatchElement(entry.address, query.address) &&
                MatchElement(entry.deviceUID, query.deviceUID);
        }

        public static bool MatchOR(ServerUserState entry, ServerUserState query)
        {
            return
                MatchElement(entry.userID, query.userID) ||
                MatchElement(entry.address, query.address) ||
                MatchElement(entry.deviceUID, query.deviceUID);
        }

        public IEnumerable<ServerUserState> FindUsers(ServerUserState query)
        {
            return from entry in Base
                   where Match(entry, query)
                   select entry;
        }

        public IEnumerable<ServerUserState> FindUsersOR(ServerUserState query)
        {
            return from entry in Base
                   where MatchOR(entry, query)
                   select entry;
        }

        public void RemoveUsers(ServerUserState removals)
        {
            IEnumerable<ServerUserState> newBase = from entry in Base
                          where !Match(entry, removals)
                          select entry;

            Base.Clear();
            foreach(ServerUserState entry in newBase)
                Base.Add(entry);
        }

        public void AddUser(ServerUserState user) => Base.Add(user);

        private void AddRootSA()
        {
            ClientSettings cs = SettingsManager.Client;
            if(cs == null)
            {
                Debug.LogWarning("Client Settings is not initialized - skipping");
                return;
            }

            AddUser(new ServerUserState()
            {
                userID = new(cs.UserPublicKey, cs.Me.Nickname),
                userState = UserState.Srv_admin,
                address = null,
                deviceUID = null,
                remarks = "Auto-generated root user"
            });

            Save();
        }

        public static ServerUserBase Load()
        {
            ServerUserBase sub;

            try
            {
                string json = File.ReadAllText($"{Application.persistentDataPath}/{PATH_SERVER_USERBASE}");
                sub = JsonConvert.DeserializeObject<ServerUserBase>(json);
            }
            catch(Exception e)
            {
                Debug.LogWarning($"Failed to load server user base - generating root server admin: {e.Message}");
                sub = new();

                sub.AddRootSA();
            }

            return sub;
        }

        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText($"{Application.persistentDataPath}/{PATH_SERVER_USERBASE}", json);
            }
            catch(Exception e)
            {
                Debug.LogWarning($"Failed to save server user base: {e.Message}");
            }
        }


    }
}
