/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections.Generic;
using UnityEngine;

namespace Arteranos.Core
{
    public class CommandLine : ScriptableObject
    {
        public Dictionary<string, string> Commands = new();
        public List<string> PlainArgs { get; internal set; } = new();

        public Dictionary<string, string> GetCommandlineArgs()
        {
#if UNITY_EDITOR
            // DEBUG: Commandline mocking in Editor

            // Default invocation
            string[] args = { "Arteranos.exe" };

            // Connect to server, loading the server's world
            // string[] args = { "Arteranos.exe", "arteranos://localhost/" };

            // Offline with the preloaded world
            // string[] args = { "Arteranos.exe", "arteranos:///https://github.com/willneedit/willneedit.github.io/raw/master/Abbey.zip" };

            // Search for server with this world.
            // string[] args = { "Arteranos.exe", "arteranos://anyhost/https://github.com/willneedit/willneedit.github.io/raw/master/Abbey.zip" };

            // Pick a server. Any server that suits your profile.
            // string[] args = { "Arteranos.exe", "arteranos://anyhost/" };
#else
            var args = System.Environment.GetCommandLineArgs();            
#endif

            Debug.Log("Invocation arguments:");

            foreach(string d_args in args)
                Debug.Log(d_args);

            // Skip the 0th argument, the program name itself
            for (int i = 1; i < args.Length; ++i)
            {
                string arg = args[i];
                if (arg.StartsWith("-"))
                {
                    string value = i < args.Length - 1 ? args[i + 1] : null;
                    value = (value?.StartsWith("-") ?? false) ? null : value;
                    if(value != null) ++i;

                    Commands.Add(arg, value);
                }
                else
                {
                    PlainArgs.Add(arg);
                }
            }
            return Commands;
        }
    }    
}
