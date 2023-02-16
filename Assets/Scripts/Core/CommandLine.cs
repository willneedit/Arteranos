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
        public Dictionary<string, string> m_Commands = new Dictionary<string, string>();

        public Dictionary<string, string> GetCommandlineArgs()
        {
#if UNITY_EDITOR
            // DEBUG: Commandline mocking in Editor
            string[] args = {  };
#else
            var args = System.Environment.GetCommandLineArgs();
#endif

            for (int i = 0; i < args.Length; ++i)
            {
                var arg = args[i].ToLower();
                if (arg.StartsWith("-"))
                {
                    var value = i < args.Length - 1 ? args[i + 1].ToLower() : null;
                    value = (value?.StartsWith("-") ?? false) ? null : value;

                    m_Commands.Add(arg, value);
                }
            }
            return m_Commands;
        }
    }    
}
