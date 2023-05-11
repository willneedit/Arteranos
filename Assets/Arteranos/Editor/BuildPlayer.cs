/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Arteranos
{
    public class BuildPlayers : MonoBehaviour
    {
        // public static string appName = Application.productName;
        public static string appName = "Arteranos";

        [MenuItem("Arteranos/Build Windows64", false, 101)]
        public static void BuildWin64()
        {
            BuildPlayerOptions bpo = new()
            {
                scenes = new[] { "Assets/Arteranos/Scenes/SampleScene.unity" },
                locationPathName = $"build/Win64/{appName}.exe",
                target = BuildTarget.StandaloneWindows64,
                subtarget = (int) StandaloneBuildSubtarget.Player,

                extraScriptingDefines = new string[0],
                options = BuildOptions.CleanBuildCache
            };

            CommenceBuild(bpo);
        }

        [MenuItem("Arteranos/Build Windows Dedicated Server", false, 101)]
        public static void BuildWin64DedServ()
        {
            BuildPlayerOptions bpo = new()
            {
                scenes = new[] { "Assets/Arteranos/Scenes/SampleScene.unity" },
                locationPathName = $"build/Win64-Server/{appName}-Server.exe",
                target = BuildTarget.StandaloneWindows64,
                subtarget = (int) StandaloneBuildSubtarget.Server,

                extraScriptingDefines = new[] { "UNITY_SERVER" },
                options = BuildOptions.CleanBuildCache
            };

            CommenceBuild(bpo);
        }

        private static void CommenceBuild(BuildPlayerOptions bpo)
        {
            BuildReport report = BuildPipeline.BuildPlayer(bpo);
            BuildSummary summary = report.summary;

            if(summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build succeeded: {summary.totalSize} bytes, {summary.totalTime} time.");
            }
            else
            {
                Debug.LogError($"Build unsuccesful: {summary.result}");
            }

            ForceRecompile();
        }

        private static void ForceRecompile()
        {
            // UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(UnityEditor.Compilation.RequestScriptCompilationOptions.CleanBuildCache);
            // EditorUtility.RequestScriptReload();
        }
    }
}
