/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

using Debug = UnityEngine.Debug;
using Newtonsoft.Json;
using System;
using Unity.EditorCoroutines.Editor;
using System.Collections;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO.Compression;

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
                CreateNoWindow = true,
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

        [MenuItem("Arteranos/Build/Retrieve Kubo IPFS daemon", false, 90)]
        public static void RetrieveIPFSDaemon()
            => RetrieveIPFSDaemon(false);

        public static void RetrieveIPFSDaemon(bool silent)
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(AcquireIPFSExecutableCoroutine(silent));
        }


        [MenuItem("Arteranos/Build/Update Project Version", false, 101)]
        public static void SetVersion()
        {
            GetProjectGitVersion();

            Core.Version v = Core.Version.Load();
            Debug.Log($"Version detected: Full={v.Full}, M.M.P={v.MMP}");

            UpdateLicenseFiles();
        }

        [MenuItem("Arteranos/Build/Build Windows64", false, 110)]
        public static void BuildWin64()
        {
            static IEnumerator SingleTask()
            {
                GetProjectGitVersion();
                yield return BuildWin64Coroutine();
            }

            EditorCoroutineUtility.StartCoroutineOwnerless(SingleTask());
        }

        [MenuItem("Arteranos/Build/Build Windows Dedicated Server", false, 120)]
        public static void BuildWin64DedServ()
        {
            static IEnumerator SingleTask()
            {
                GetProjectGitVersion();
                yield return BuildWin64DSCoroutine();
            }

            EditorCoroutineUtility.StartCoroutineOwnerless(SingleTask());
        }

        [MenuItem("Arteranos/Build Installation Package", false, 80)]
        public static void BuildInstallationPackage()
        {
            static IEnumerator SingleTask()
            {
                // Build Package wipes the build/ directory, and builds
                // the version files itself.
                yield return AcquireIPFSExecutableCoroutine(true);
                yield return BuildInstallationPackageCoroutine();
            }

            EditorCoroutineUtility.StartCoroutineOwnerless(SingleTask());
        }

        private static IEnumerator AcquireIPFSExecutableCoroutine(bool silent)
        {
            string IPFSExe = "ipfs.exe";

            string source = "https://github.com/ipfs/kubo/releases/download/v0.28.0/kubo_v0.28.0_windows-amd64.zip";
            // TODO sha512
            string target = $"{Application.temporaryCachePath}/downloaded-kubo-ipfs.zip";
            string targetDir = $"{target}.dir";
            string desired = $"{targetDir}/kubo/ipfs.exe";
            long totalBytes = 0;

            Debug.Log(Directory.GetCurrentDirectory());

            // Earlier sessions may have it.
            if (File.Exists(IPFSExe))
            {
                if (!silent) Debug.Log($"ipfs.exe is already there, maybe you want to manually delete it?");
                IPFSExe = desired;
                yield break;
            }

            Debug.Log($"Downloading {source}...");

            Task taskDownload = Task.Run(async () =>
            {
                if (File.Exists(target)) File.Delete(target);

                using HttpClient client = new();
                client.Timeout = TimeSpan.FromSeconds(60);
                using HttpResponseMessage response = await client.GetAsync(source).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                totalBytes = response.Content.Headers.ContentLength ?? -1;
                byte[] binary = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                using Stream s = File.Create(target);
                s.Write(binary, 0, binary.Length);
                s.Flush();
                s.Close();
            });

            yield return new WaitUntil(() => taskDownload.IsCompleted);

            Debug.Log($"Unzipping {targetDir}...");

            Task taskUnzip = Task.Run(() =>
            {
                if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);

                try
                {
                    ZipFile.ExtractToDirectory(target, targetDir);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            });

            yield return new WaitUntil(() => taskDownload.IsCompleted);

            if (File.Exists(desired)) File.Copy(desired, IPFSExe);

            Debug.Log("Done.");
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

        private static IEnumerator BuildWin64Coroutine()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;
            EditorUserBuildSettings.SetBuildLocation(BuildTarget.StandaloneWindows64, "build/Win64/");

            BuildPlayerOptions bpo = new()
            {
                scenes = GetSceneNames(),
                locationPathName = $"build/Win64/{appName}.exe",
                target = BuildTarget.StandaloneWindows64,
                subtarget = (int)StandaloneBuildSubtarget.Player,

                extraScriptingDefines = new string[0],
                options = BuildOptions.CleanBuildCache
            };

            yield return null;

            CommenceBuild(bpo);
        }

        private static IEnumerator BuildWin64DSCoroutine()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Server;
            EditorUserBuildSettings.SetBuildLocation(BuildTarget.StandaloneWindows64, "build/Win64-Server/");

            BuildPlayerOptions bpo = new()
            {
                scenes = GetSceneNames(),
                locationPathName = $"build/Win64-Server/{appName}-Server.exe",
                target = BuildTarget.StandaloneWindows64,
                subtarget = (int)StandaloneBuildSubtarget.Server,

                extraScriptingDefines = new[] { "UNITY_SERVER" },
                options = BuildOptions.CleanBuildCache
            };

            yield return null;

            CommenceBuild(bpo);
        }

        public static IEnumerator BuildInstallationPackageCoroutine()
        {
            IEnumerator Execute(string command, string argline)
            {
                ProcessStartInfo psi = new()
                {
                    FileName = command,
                    Arguments = argline,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    WorkingDirectory = "build",
                    CreateNoWindow = true,
                };

                using Process process = Process.Start(psi);
                using StreamReader reader = process.StandardOutput;

                process.WaitForExit();
                string data = reader.ReadToEnd();
                Debug.Log(data);

                yield return null;
            }

            IEnumerator BuildSetup()
            {
                string wixroot = Environment.GetEnvironmentVariable("wix");

                yield return Execute($"{wixroot}bin\\heat", "dir Win64 -out Win64.wxi -scom -sfrag -sreg -svb6 -ag -dr AppDir -cg Pack_Win64 -srd -var var.BinDir");
                yield return Execute($"{wixroot}bin\\heat", "dir Win64-Server -out Win64-Server.wxi -scom -sfrag -sreg -svb6 -ag -dr ServerDir -cg Pack_Win64_Server -srd -var var.SrvBinDir");
                yield return Execute($"{wixroot}bin\\candle", "..\\Setup\\Main.wxs Win64-Server.wxi Win64.wxi -ext WixFirewallExtension -dBinDir=Win64 -dSrvBinDir=Win64-Server -arch x64");
                yield return Execute($"{wixroot}bin\\light", "Main.wixobj Win64.wixobj Win64-Server.wixobj -ext WixUIExtension -ext WixFirewallExtension -o ArteranosSetup");
            }

            IEnumerator BuildSetupExe()
            {
                string wixroot = Environment.GetEnvironmentVariable("wix");

                yield return Execute($"{wixroot}bin\\candle", "-ext WixNetFxExtension -ext WixBalExtension -ext WixUtilExtension ..\\Setup\\MainBurn.wxs");
                yield return Execute($"{wixroot}bin\\light", "-ext WixNetFxExtension -ext WixBalExtension -ext WixUtilExtension MainBurn.wixobj -o ArteranosSetup");
            }

            IEnumerator BuildArchive()
            {
                yield return Execute("7z", "a -t7z -m0=lzma -mx=9 -mfb=64 -md=32m -ms=on ArteranosSetup.7z ArteranosSetup.exe");
            }

            int progressId = Progress.Start("Building...");

            try
            {
                if (Directory.Exists("build")) Directory.Delete("build", true);

                GetProjectGitVersion();

                Progress.Report(progressId, 0.40f, "Build Win64 Dedicated Server");

                yield return BuildWin64DSCoroutine();

                Progress.Report(progressId, 0.60f, "Build Win64 Desktop");

                yield return BuildWin64Coroutine();

                Directory.Move("build/Win64/Arteranos_BurstDebugInformation_DoNotShip", "build/Arteranos_BurstDebugInformation");
                Directory.Move("build/Win64-Server/Arteranos-Server_BurstDebugInformation_DoNotShip", "build/Arteranos-Server_BurstDebugInformation");

                File.Move("build/Win64/Arteranos.exe", "build/Arteranos.exe");
                File.Move("build/Win64-Server/Arteranos-Server.exe", "build/Arteranos-Server.exe");
                File.Copy("ipfs.exe", "build/ipfs.exe");

                Progress.Report(progressId, 0.80f, "Build setup wizard");

                yield return BuildSetup();

                File.Move("build/Arteranos.exe", "build/Win64/Arteranos.exe");
                File.Move("build/Arteranos-Server.exe", "build/Win64-Server/Arteranos-Server.exe");

                yield return BuildSetupExe();

                if(!File.Exists("build/ArteranosSetup.exe"))
                {
                    Debug.LogError("Installation wizard build failed - see logs");
                    yield break;
                }

                yield return BuildArchive();

                Debug.Log("Build task finished.");
            }
            finally
            {
                Progress.Remove(progressId);
            }
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
        }
    }
}
