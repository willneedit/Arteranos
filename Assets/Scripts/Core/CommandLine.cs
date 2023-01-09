using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR;
using System;

namespace Core
{
    public class CommandLine : ScriptableObject
    {
        public Dictionary<string, string> m_Commands = new Dictionary<string, string>();

        private NetworkManager netManager;

        private bool ParseBoolArg(string boolParam)
        {
            return boolParam == "on" || boolParam == "yes" || boolParam == "1";
        }

        private Dictionary<string, string> GetCommandlineArgs()
        {

            var args = System.Environment.GetCommandLineArgs();

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

        public void ParseCommandLine()
        {

            if (Application.isEditor) return;

            var args = GetCommandlineArgs();
        }
    }    
}
