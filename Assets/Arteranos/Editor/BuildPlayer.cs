/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Arteranos.Core;

using Debug = UnityEngine.Debug;
using Newtonsoft.Json;

namespace Arteranos
{

    public class BuildPlayers : MonoBehaviour
    {
        // public static string appName = Application.productName;
        public static readonly string appName = "Arteranos";

        public static void GetProjectGitVersion()
        {
            ProcessStartInfo psi = new()
            {
                FileName = "git",
                Arguments = "describe --tags --long",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using Process process = Process.Start(psi);
            using StreamReader reader = process.StandardOutput;

            string data = reader.ReadToEnd();

            Debug.Log($"Git says: {data}");

            Core.Version version = new();

            string[] parts = data.Split('-');

            version.Hash = parts[^1][1..^1];     // Remove the 'g' before the commit hash and the LF
            version.B = parts[^2];
            version.MMP = parts[0][1..];          // Remove the 'v' before the version
            version.MMPB = $"{version.MMP}.{version.B}";

            if(parts.Length > 3)
                version.Tag = "-"+string.Join("-", parts[1..^2]);

            version.Full = $"{version.MMP}.{version.B}{version.Tag}-{version.Hash}";

            TextAsset textAsset = new(JsonConvert.SerializeObject(version, Formatting.Indented));

            if(!Directory.Exists("Assets/Generated"))
                AssetDatabase.CreateFolder("Assets", "Generated");

            if(!Directory.Exists("Assets/Generated/Resources"))
                AssetDatabase.CreateFolder("Assets/Generated", "Resources");

            AssetDatabase.CreateAsset(textAsset, "Assets/Generated/Resources/Version.asset");

            textAsset = new();

            string WiXFileText =
@"<?xml version='1.0' encoding='utf-8' ?>

<?define version = """+ version.MMPB + @""" ?>
<?define fullversion = """+ version.Full + @""" ?>

<Include xmlns='http://schemas.microsoft.com/wix/2006/wi'>
  
</Include>
";
            if(!Directory.Exists("build"))
                Directory.CreateDirectory("build");

            File.WriteAllText("build/WiXVersion.wxi",WiXFileText);
        }

        public static void UpdateLicenseFiles()
        {
            File.Copy("LICENSE.md", "Assets\\Generated\\Resources\\LICENSE.md", true);
            File.Copy("Third Party Notices.md", "Assets\\Generated\\Resources\\Third Party Notices.md", true);

            AssetDatabase.Refresh();
        }

        [MenuItem("Arteranos/Update Project Version", false, 101)]
        public static void SetVersion()
        {
            GetProjectGitVersion();

            Core.Version v = Core.Version.Load();
            Debug.Log($"Version detected: Full={v.Full}, M.M.P={v.MMP}");

            UpdateLicenseFiles();
        }

        public static string[] GetSceneNames()
        {
            return new[] { 
//                "Assets/Arteranos/Scenes/SampleScene.unity"
                "Assets/Arteranos/Scenes/_Startup.unity",
                "Assets/Arteranos/Scenes/OfflineScene.unity",
                "Assets/Arteranos/Scenes/Transition.unity"
            };
        }

        [MenuItem("Arteranos/Build Windows64", false, 110)]
        public static void BuildWin64()
        {
            GetProjectGitVersion();

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;
            EditorUserBuildSettings.SetBuildLocation(BuildTarget.StandaloneWindows64, "build/Win64/");

            BuildPlayerOptions bpo = new()
            {
                scenes = GetSceneNames(),
                locationPathName = $"build/Win64/{appName}.exe",
                target = BuildTarget.StandaloneWindows64,
                subtarget = (int) StandaloneBuildSubtarget.Player,

                extraScriptingDefines = new string[0],
                options = BuildOptions.CleanBuildCache
            };

            CommenceBuild(bpo);
        }

        [MenuItem("Arteranos/Build Windows Dedicated Server", false, 120)]
        public static void BuildWin64DedServ()
        {
            GetProjectGitVersion();

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;
            EditorUserBuildSettings.SetBuildLocation(BuildTarget.StandaloneWindows64, "build/Win64-Server/");

            BuildPlayerOptions bpo = new()
            {
                scenes = GetSceneNames(),
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
