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
            string[] args = { "arteranos://localhost/https://github.com/willneedit/willneedit.github.io/raw/master/Abbey.zip" };
#else
            var args = System.Environment.GetCommandLineArgs();
#endif

            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i].ToLower();
                if (arg.StartsWith("-"))
                {
                    string value = i < args.Length - 1 ? args[i + 1].ToLower() : null;
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
