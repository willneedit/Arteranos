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

namespace Arteranos.Core
{
    /// <summary>
    /// Cut-down instance of BitArray, limited to 64 bits for optimization
    /// </summary>
    public class Bit64field
    {
        public virtual ulong Bits { get; set; }

        public Bit64field(ulong bits = 0) 
        {
            this.Bits = bits; 
        }

        public static implicit operator ulong(Bit64field field) => field.Bits;

        public override string ToString() => Bits.ToString();

        public void Set(ulong bits, bool desired)
        {
            if(desired)
            {
                this.Bits |= bits;
            }
            else
            {
                this.Bits &= ~bits;
            }
        }

        public bool IsAll(ulong bits) => (this.Bits & bits) == bits;

        public bool IsAny(ulong bits) => (this.Bits & bits) != 0;
    }

    public class UserState : Bit64field
    {
        // Just an ordinary everyday user.
        public const ulong Normal = 0;

        // ---------------------------------------------------------------
        // 0  ... 15: User related

        // Trusted user, whatever it shall be... ¯\_(ツ)_/¯
        public const ulong Trusted = (1 << 0);

        // ---------------------------------------------------------------
        // 16 ... 31: World related

        // Derived from the world administrator, can elevate or demote assistants with the same powers
        public const ulong World_admin_asstnt = (1 << 30);

        // World administrators
        public const ulong World_admin = ((ulong) 1 << 31);

        // ---------------------------------------------------------------
        // 32 ... 47: Server related

        // Derived from the world administrator, can elevate or demote assistants with the same powers
        public const ulong Srv_admin_asstnt = (1 << 46);

        // Server administrators
        public const ulong Srv_admin = (1 << 47);


        // ---------------------------------------------------------------
        // 48 ... 63: Repercussions, especially with the regative result (= banned bit set)

        // Below the user's legal or recommended age, e.g. below 13 b/c COPPA.
        public const ulong Underage = (1 << 48);

        // Disruptive behavior
        public const ulong Trolling = (1 << 49);

        // Discrimination/-phobic/hatemongering (see below)
        public const ulong Hating = (1 << 50);

        // Incessantly loud (e.g. noisy mic) (Maybe automatically force-muted on login?)
        public const ulong Loud = (1 << 51);

        // Mobbing/Bullying/Harassment
        public const ulong Bullying = (1 << 52);

        // Undesired sexual advances or harassment
        public const ulong SxHarassment = (1 << 53);

        // Exploit/security leak usage (maybe tripped in the network stack)
        public const ulong Exploiting = (1 << 60);

        // Impersonation (to gain privileges by social engineering)
        public const ulong Impersonation = (1 << 61);

        // Attempted ban evasion
        public const ulong BanEvading = (1 << 62);

        // User has been banned
        public const ulong Banned = ((ulong) 1 << 63);

        public override string ToString() => base.ToString();

        public override ulong Bits { get => base.Bits; set => base.Bits = value; }

        [JsonIgnore]
        public bool IsBanned => IsAny(Banned);

        [JsonIgnore]
        public bool IsWAdmin => IsAny(World_admin | World_admin_asstnt);

        [JsonIgnore]
        public bool IsSAdmin => IsAny(Srv_admin | Srv_admin_asstnt);

        public UserState(ulong init) : base(init) { }

    }

    public struct ServerUserState
    {
        public UserID userID;
        public UserState state;
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
        public IEnumerable<ServerUserState> FindUsers(ServerUserState query)
        {
            return from entry in Base
                   where Match(entry, query)
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
                state = new UserState(UserState.Srv_admin),
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
