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
using Arteranos.Core;

namespace Arteranos.Editor
{

    public class BuildPlayers
    {
        private const string KUBO_VERSION = "v0.32.0";

        private static readonly string KUBO_EXECUTABLE_ROOT = $"https://github.com/ipfs/kubo/releases/download/{KUBO_VERSION}/kubo_{KUBO_VERSION}";
        private const string KUBO_ARCH_WIN64 = "windows-amd64";
        private const string KUBO_ARCH_LINUX64 = "linux-amd64";

        // public static string appName = Application.productName;
        public static readonly string appName = "Arteranos";

        public static Core.Version version { get; private set; } = null;

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


            string[] parts = data.Split('-');

            version = Core.Version.Parse(parts[0][1..]);

            version.Hash = parts[^1][1..^1];     // Remove the 'g' before the commit hash and the LF
            version.B = parts[^2];
            version.MMP = $"{version.Major}.{version.Minor}.{version.Patch}";
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

<?define version = """+ version.MMP + @""" ?>
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

        public static void BumpForceReloadFile()
        {
            // Put a timestamp in the RFC3339 datetime format.
            // Contents doesn't matter, only when it's CHANGING after a platform switch!

            File.WriteAllText("Assets/Generated/dummy.cs", @"
// Automatically generated file -- EDITS WILL BE OVERWRITTEN

#pragma warning disable IDE1006
public static class _dummy
{
    public static string creationTime = """ + DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK") + @""";
}
");
            AssetDatabase.Refresh();
        }

        [MenuItem("Arteranos/Build/Retrieve Kubo IPFS daemon (Windows + Linux)", false, 60)]
        public static void RetrieveIPFSDaemon()
            => RetrieveIPFSDaemon(false);

        public static void RetrieveIPFSDaemon(bool silent)
        {
            IEnumerator AquireIPFSExe(bool silent)
            {
                yield return AcquireIPFSWinExeCoroutine(silent);

                yield return AcquireIPFSLinuxExeCoroutine(silent);
            }

            EditorCoroutineUtility.StartCoroutineOwnerless(AquireIPFSExe(silent));
        }

        [MenuItem("Arteranos/Build/Update version and platform", false, 120)]
        public static void SetVersion()
        {
            GetProjectGitVersion();

            UpdateLicenseFiles();

            BumpForceReloadFile();

            Core.Version v = Core.Version.Load();
            Debug.Log($"Version detected: Full={v.Full}, M.M.P={v.MMP}");
        }

        [MenuItem("Arteranos/Build/Build Windows64", false, 140)]
        public static void BuildWin64()
        {
            static IEnumerator SingleTask()
            {
                GetProjectGitVersion();
                yield return BuildWin64Coroutine();
            }

            EditorCoroutineUtility.StartCoroutineOwnerless(SingleTask());
        }

        [MenuItem("Arteranos/Build/Build Windows Dedicated Server", false, 150)]
        public static void BuildWin64DedServ()
        {
            static IEnumerator SingleTask()
            {
                GetProjectGitVersion();
                yield return BuildWin64DSCoroutine();
            }

            EditorCoroutineUtility.StartCoroutineOwnerless(SingleTask());
        }

        [MenuItem("Arteranos/Build/Build Linux64 Dedicated Server", false, 160)]
        public static void BuildLinux64DedServ()
        {
            static IEnumerator SingleTask()
            {
                GetProjectGitVersion();
                yield return BuildLinux64DSCoroutine();
            }

            EditorCoroutineUtility.StartCoroutineOwnerless(SingleTask());
        }

        [MenuItem("Arteranos/Build/Build Installation Package (Windows)", false, 80)]
        public static void BuildWinInstallationPackage()
        {
            static IEnumerator SingleTask()
            {
                // Build Package wipes the build/ directory, and builds
                // the version files itself.
                yield return AcquireIPFSWinExeCoroutine(true);
                yield return BuildWinInstallationPackageCoroutine();
            }

            EditorCoroutineUtility.StartCoroutineOwnerless(SingleTask());
        }

        [MenuItem("Arteranos/Build/Build Installation Package (Linux)", false, 81)]
        public static void BuildLinuxInstallationPackage()
        {
            static IEnumerator SingleTask()
            {
                yield return AcquireIPFSLinuxExeCoroutine(true);
                yield return BuildDebianPackageCoroutine();
            }

            EditorCoroutineUtility.StartCoroutineOwnerless(SingleTask());
        }

        [MenuItem("Arteranos/Build/Build deployment directory", false, 100)]
        public static void BuildDeploymentDirectory()
        {
            static IEnumerator SingleTask()
            {
                // For convenience, reset build settings to force domain reload
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
                EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;

                GetProjectGitVersion();

                if (Directory.Exists("Deployment")) Directory.Delete("Deployment", true);
                string vstr = $"{version.Major}.{version.Minor}.{version.Patch}";

                string nameWin64 = $"arteranos-{vstr}-Win64.exe";
                string nameLinux64 = $"arteranos-server-{vstr}-Linux.deb";

                Directory.CreateDirectory("Deployment");

                File.Copy($"build/{nameWin64}", $"Deployment/{nameWin64}");
                File.Copy($"build/{nameLinux64}", $"Deployment/{nameLinux64}");

                yield return Execute("wsl", $"sha256sum >sha256sums {nameWin64} {nameLinux64} && echo \"SHA256 sums file created\"", "Deployment");

                yield return Execute("cmd", "/c start .", "Deployment");

                yield return Execute("cmd", "/c start notepad.exe CHANGELOG.md", ".");
            }

            EditorCoroutineUtility.StartCoroutineOwnerless(SingleTask());
        }

        private static IEnumerator AcquireIPFSWinExeCoroutine(bool silent)
        {
            string IPFSExe = "ipfs.exe";
            string desiredFile = $"/kubo/{IPFSExe}";
            string archiveFormat = "zip";
            string source = $"{KUBO_EXECUTABLE_ROOT}_{KUBO_ARCH_WIN64}.{archiveFormat}";

            yield return DownloadExecutable(silent, IPFSExe, desiredFile, archiveFormat, source);
        }

        private static IEnumerator AcquireIPFSLinuxExeCoroutine(bool silent)
        {
            string IPFSExe = "ipfs";
            string desiredFile = $"/{IPFSExe}";
            string archiveFormat = "tar.gz";

            string source = $"{KUBO_EXECUTABLE_ROOT}_{KUBO_ARCH_LINUX64}.{archiveFormat}";

            yield return DownloadExecutable(silent, IPFSExe, desiredFile, archiveFormat, source);
        }

        private static IEnumerator DownloadExecutable(bool silent, string IPFSExe, string desiredFile, string archiveFormat, string source)
        {
            // TODO sha512
            string target = $"{Application.temporaryCachePath}/downloaded-kubo-ipfs.{archiveFormat}";
            string targetDir = $"{target}.dir";
            string desired = $"{targetDir}{desiredFile}";

            // Earlier sessions may have it.
            if (File.Exists(IPFSExe))
            {
                if (!silent)
                {
                    Debug.Log($"ipfs.exe is already there in the project codebase, maybe you want to manually delete it?");
                    Debug.Log(Directory.GetCurrentDirectory());
                }

                IPFSExe = desired;
                yield break;
            }

            Debug.Log($"Downloading {source}...");

            Task taskDownload = DownloadFile(source, target);

            yield return new WaitUntil(() => taskDownload.IsCompleted);

            Debug.Log($"Unzipping {targetDir}...");

            if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);

            if (archiveFormat == "zip")
            {
                Task taskUnzip = Task.Run(() =>
                {
                    try
                    {
                        ZipFile.ExtractToDirectory(target, targetDir);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                });

                yield return new WaitUntil(() => taskUnzip.IsCompleted);
            }
            else if (archiveFormat == "tar.gz")
            {
                using FileStream compressedFileStream = File.Open(target, FileMode.Open);
                using MemoryStream outputFileStream = new();
                using var decompressor = new GZipStream(compressedFileStream, CompressionMode.Decompress);
                decompressor.CopyTo(outputFileStream);

                outputFileStream.Position = 0;
                Task taskUnzip = Utils.UnTarToDirectoryAsync(outputFileStream, targetDir);

                yield return new WaitUntil(() => taskUnzip.IsCompleted);
            }

            if (File.Exists(desired))
                File.Copy(desired, IPFSExe);
            else
                Debug.LogError($"{desired} not found.");

            Debug.Log("Done.");
        }

        private static Task DownloadFile(string source, string target)
        {
            return Task.Run(async () =>
            {
                long totalBytes = 0;
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
        }

        public static string[] GetSceneNames()
        {
            return new[] {
                "Assets/Arteranos/Modules/Core/Other/_Startup.unity",
                "Assets/Arteranos/Modules/OfflineScene/OfflineScene.unity",
                "Assets/Arteranos/Modules/Core/Other/Transition.unity"
            };
        }

        private static IEnumerator BuildWin64Coroutine()
        {
            BuildPlayerOptions bpo = new()
            {
                scenes = GetSceneNames(),
                locationPathName = $"build/Win64/{appName}.exe",
                target = BuildTarget.StandaloneWindows64,
                subtarget = (int)StandaloneBuildSubtarget.Player,
            };

            yield return null;

            CommenceBuild(bpo);
        }

        private static IEnumerator BuildWin64DSCoroutine()
        {
            BuildPlayerOptions bpo = new()
            {
                scenes = GetSceneNames(),
                locationPathName = $"build/Win64-Server/{appName}-Server.exe",
                target = BuildTarget.StandaloneWindows64,
                subtarget = (int)StandaloneBuildSubtarget.Server,
            };

            yield return null;

            CommenceBuild(bpo);
        }

        private static IEnumerator BuildLinux64DSCoroutine()
        {
            const string buildTargetRoot = "build/Linux64-Server";

            if(!Directory.Exists(buildTargetRoot)) Directory.CreateDirectory(buildTargetRoot);
            if (!File.Exists($"{buildTargetRoot}/ipfs")) File.Copy("ipfs", $"{buildTargetRoot}/ipfs");

            BuildPlayerOptions bpo = new()
            {
                scenes = GetSceneNames(),
                locationPathName = $"{buildTargetRoot}/{appName}-Server",
                target = BuildTarget.StandaloneLinux64,
                subtarget = (int)StandaloneBuildSubtarget.Server,
            };

            yield return null;

            CommenceBuild(bpo);
        }

        private static IEnumerator Execute(string command, string argline, string cwd = "build")
        {
            ProcessStartInfo psi = new()
            {
                FileName = command,
                Arguments = argline,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WorkingDirectory = cwd,
                CreateNoWindow = true,
            };

            using Process process = Process.Start(psi);
            using StreamReader reader = process.StandardOutput;

            Task t = Task.Run(() => process.WaitForExit());

            yield return new WaitUntil(() => t.IsCompleted);

            string data = reader.ReadToEnd();
            Debug.Log(data);

            yield return null;
        }

        public static IEnumerator BuildDebianPackageCoroutine()
        {
            int progressId = Progress.Start("Building...");

            try
            {
                GetProjectGitVersion();

                if (true)
                {
                    Progress.Report(progressId, 0.40f, "Build Linux Dedicated Server");

                    yield return BuildLinux64DSCoroutine();

                }

                Progress.Report(progressId, 0.40f, "Creating Debian Package");

                string systemroot = Environment.GetEnvironmentVariable("SystemRoot");

                yield return Execute($"{systemroot}\\system32\\cmd.exe", "/c build.bat" + $" {version.Major} {version.Minor} {version.Patch}", "Setup-Linux");

                Debug.Log("Finished.");

            }
            finally
            {
                Progress.Remove(progressId);
            }

        }

        public static IEnumerator BuildWinInstallationPackageCoroutine()
        {
            GetProjectGitVersion();

            string SetupExeName = $"arteranos-{version.Major}.{version.Minor}.{version.Patch}-Win64.exe";

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
                yield return Execute($"{wixroot}bin\\light", $"-ext WixNetFxExtension -ext WixBalExtension -ext WixUtilExtension MainBurn.wixobj -o {SetupExeName}");
            }

            int progressId = Progress.Start("Building...");

            try
            {
                // Debug convenience when false - for just building the installer package,
                // without building the actual executables
                if(true)
                {
                    Progress.Report(progressId, 0.40f, "Build Win64 Dedicated Server");

                    yield return BuildWin64DSCoroutine();

                    Progress.Report(progressId, 0.60f, "Build Win64 Desktop");

                    yield return BuildWin64Coroutine();

                    Directory.Delete("build/Win64/Arteranos_BurstDebugInformation_DoNotShip", true);
                    Directory.Delete("build/Win64-Server/Arteranos-Server_BurstDebugInformation_DoNotShip", true);

                    File.Copy("ipfs.exe", "build/ipfs.exe", true);

                }

                File.Move("build/Win64/Arteranos.exe", "build/Arteranos.exe");
                File.Move("build/Win64-Server/Arteranos-Server.exe", "build/Arteranos-Server.exe");
 
                Progress.Report(progressId, 0.80f, "Build setup wizard");

                if (File.Exists("build/ArteranosSetup.msi")) File.Delete("build/ArteranosSetup.msi");
                if (File.Exists($"build/{SetupExeName}")) File.Delete($"build/{SetupExeName}");

                yield return BuildSetup();

                File.Move("build/Arteranos.exe", "build/Win64/Arteranos.exe");
                File.Move("build/Arteranos-Server.exe", "build/Win64-Server/Arteranos-Server.exe");

                if (!File.Exists("build/ArteranosSetup.msi"))
                {
                    Debug.LogError("Installation wizard build failed - see logs");
                    yield break;
                }

                yield return BuildSetupExe();

                if(!File.Exists($"build/{SetupExeName}"))
                {
                    Debug.LogError("Installation executable build failed - see logs");
                    yield break;
                }

                Debug.Log("Build task finished.");
            }
            finally
            {
                Progress.Remove(progressId);
            }
        }
        private static void CommenceBuild(BuildPlayerOptions bpo)
        {
            bpo.options = BuildOptions.CleanBuildCache;
            bpo.extraScriptingDefines =
                bpo.subtarget == (int)StandaloneBuildSubtarget.Server
                ? new[] { "UNITY_SERVER" }
                : new string[0];

            string buildLocation = Path.GetDirectoryName(bpo.locationPathName);
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, bpo.target);
            EditorUserBuildSettings.standaloneBuildSubtarget = (StandaloneBuildSubtarget)bpo.subtarget;
            EditorUserBuildSettings.SetBuildLocation(BuildTarget.StandaloneWindows64, $"{buildLocation}/");

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
