﻿/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arteranos.Core
{
    public class SettingsManager : MonoBehaviour
    {

        protected enum ConnectionMode
        {
            Disconnected = 0,
            Client,
            Server,
            Host
        }

        private CommandLine Command;

        protected ConnectionMode m_ConnectionMode = ConnectionMode.Disconnected;

        protected static string TargetedServerPort = null;
        protected static string DesiredWorld = null;

        public static Web.IConnectionManager ConnectionManager { get; protected set; }
        public static Transform Purgatory { get; private set; }
        public static ClientSettings Client { get; internal set; }
        public static ServerSettings Server { get; internal set; }

        public static List<string> Users { get; internal set; } = new();

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

            }

            Client.VRMode = GetBoolArg("-vr", Client.VRMode);
        }

        private void SetupPurgatory()
        {
            Purgatory = new GameObject("_Purgatory").transform;
            Purgatory.position = new Vector3(0, -9000, 0);
            DontDestroyOnLoad(Purgatory.gameObject);
        }

        public static void RegisterUser(string userHash)
        {
            if(!Users.Contains(userHash))
                Users.Add(userHash);
        }

        public static void UnregisterUser(string userHash)
        {
            if(Users.Contains(userHash))
                Users.Remove(userHash);
        }
    }
}