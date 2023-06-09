/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.Avatar;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Arteranos.Core
{
    public class SettingsManager : MonoBehaviour
    {
        private CommandLine Command;

        protected static string TargetedServerPort = null;
        protected static string DesiredWorld = null;

        public static bool StartupTrigger { get; private set; } = false;

        public static string ResetStartupTrigger()
        {
            StartupTrigger = false;
            return DesiredWorld;
        }

        public static ServerSettingsJSON CurrentServer { get; set; } = null;

        public static Transform Purgatory { get; private set; }
        public static ClientSettings Client { get; internal set; }
        public static ServerSettings Server { get; internal set; }

        public static List<IAvatarBrain> Users { get; internal set; } = new();

        private void Awake()
        {
            SetupPurgatory();

            ParseSettingsAndCmdLine();
        }

        private void ParseSettingsAndCmdLine()
        {
            bool GetCmdArg(string key, out string val)
            {
                return Command.Commands.TryGetValue(key, out val);
            }

            bool GetBoolArg(string key, bool def = false)
            {
                if(GetCmdArg(key, out _))
                    return true;

                if(GetCmdArg("-no" + key, out _))
                    return false;

                return def;
            }

            Client = ClientSettings.LoadSettings();
            Server = ServerSettings.LoadSettings();
            Command = ScriptableObject.CreateInstance<CommandLine>();

            Command.GetCommandlineArgs();

            if(Command.PlainArgs.Count > 0)
            {

                Uri uri = Utils.ProcessUriString(
                    Command.PlainArgs[0],
           
                    scheme: "arteranos",
                    port: ServerSettingsJSON.DefaultMetadataPort
                );

                Command.PlainArgs.RemoveAt(0);

                if(!string.IsNullOrEmpty(uri.Host))
                    TargetedServerPort = $"{uri.Host}:{uri.Port}";

                if(uri.AbsolutePath != "/")
                    DesiredWorld = uri.AbsolutePath[1..];

                StartupTrigger = true;

            }

            Client.VRMode = GetBoolArg("-vr", Client.VRMode);
        }

        private void SetupPurgatory()
        {
            Purgatory = new GameObject("_Purgatory").transform;
            Purgatory.position = new Vector3(0, -9000, 0);
            DontDestroyOnLoad(Purgatory.gameObject);
        }

        public static void RegisterUser(IAvatarBrain brain) => Users.Add(brain);

        public static void UnregisterUser(IAvatarBrain brain)
        {
            if(Users.Contains(brain))
                Users.Remove(brain);
        }

        public static IAvatarBrain GetOnlineUser(UserID userID)
        {
            IEnumerable<IAvatarBrain> q0 = from entry in GameObject.FindGameObjectsWithTag("Player")
                    select entry.GetComponent<IAvatarBrain>();

            IEnumerable<IAvatarBrain> q = from entry in q0
                                             where entry.UserID == userID
                                             select entry;

            return q.Count() > 0 ? q.First() : null;
        }

        public static IEnumerable<IAvatarBrain> GetOnlineUsers()
        {
            return from entry in GameObject.FindGameObjectsWithTag("Player")
                   select entry.GetComponent<IAvatarBrain>();

        }
    }
}