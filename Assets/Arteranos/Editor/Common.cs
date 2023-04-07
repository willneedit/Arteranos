/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Arteranos.Editor
{
    public static class Extensions
    {
        public static string Capitalize(this string str) => str[..1].ToUpper() + str[1..];

        public static string ToShortDTString(this DateTime dt) => dt.ToShortDateString() + ", " + dt.ToShortTimeString();
    }

    public class Common
    {
        public static readonly int currentUnityVersion = 20203;
        public static readonly int minimumUnityVersion = 20203;

        public static readonly string relaxedUnityVersion = "2020.3";
        public static readonly string strictUnityVersion = "2020.3.18f1";

        private static int _usingUnityVersion = 0;

        private static string _resourceDirectory = null;

        public static int UsingUnityVersion
        {
            get
            {
                if (_usingUnityVersion == 0)
                {
                    string[] parts = Application.unityVersion.Split('.');
                    int.TryParse(parts[0] + parts[1], out _usingUnityVersion);
                }

                return _usingUnityVersion;
            }
        }

        public static string ResourceDirectory
        {
            get
            {
                if(string.IsNullOrEmpty(_resourceDirectory))
                {
                    UnityEditor.PackageManager.PackageInfo p = UnityEditor.PackageManager.PackageInfo.FindForAssembly(Assembly.GetExecutingAssembly());
                    if (p != null)
                        _resourceDirectory = Path.Combine("Packages", p.name, "Resources");
                }

                if(String.IsNullOrEmpty(_resourceDirectory))
                {
                    // Find ourselves first, then go from there to the Resources folder.
                    string[] g = AssetDatabase.FindAssets("t:Script UrpInstaller");
                    if(g.Length > 0)
                    {
                        _resourceDirectory = AssetDatabase.GUIDToAssetPath(g[0]);
                        _resourceDirectory = Path.GetDirectoryName(_resourceDirectory);
                        _resourceDirectory = Path.GetDirectoryName(_resourceDirectory);
                        _resourceDirectory = Path.Combine(_resourceDirectory, "Resources");
                    }
                }

                return _resourceDirectory;
            }
        }
        public static void DisplayStatus(string caption, string defaultText, string activeText, string goodText = null)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(caption, GUILayout.Width(150.0f));

            GUIStyle style = new() { fontStyle = FontStyle.Bold };

            if (activeText == null)
            {
                style.normal.textColor = new Color(0.62f, 0, 0);
                EditorGUILayout.LabelField(defaultText, style);
            }
            else
            {
                if (goodText == null || activeText == goodText)
                    style.normal.textColor = new Color(0, 0.62f, 0);
                else
                    style.normal.textColor = new Color(0.2f, 0.2f, 0);

                EditorGUILayout.LabelField(activeText, style);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

        }

        /// <summary>
        /// Parses a string like "2021-01-06T08:52:02.432-08:00" to a UTC DateTime timestamp
        /// </summary>
        /// <param name="str">The string to parse</param>
        /// <returns>the timestamp in UTC</returns>
        public static DateTime ParseTimeString(string str)
        {
            int years = int.Parse(str[..4]);
            int months = int.Parse(str.Substring(5, 2));
            int days = int.Parse(str.Substring(8, 2));

            int hours = int.Parse(str.Substring(11, 2));
            int minutes = int.Parse(str.Substring(14, 2));
            int seconds = int.Parse(str.Substring(17, 2));

            int len = str.Length;

            int offs_hours = int.Parse(str.Substring(len - 6, 3));
            int offs_minutes = int.Parse(str.Substring(len - 2, 2));

            if (offs_hours < 0) offs_minutes = -offs_minutes;

            DateTime t = new(years, months, days, hours, minutes, seconds);
            // *remove* the timezone offset
            t = t.AddHours(-offs_hours);
            t = t.AddMinutes(-offs_minutes);

            return t;
        }

        private class VersionInfo
        {
            public int version = 0;
            public DateTime created = new();

            public string Datestring => created.ToShortDateString() + ", " + created.ToShortTimeString();

            public string Versionstring
            {
                get
                {
                    if (version == 0)
                        return null;
                    else if (version != Common.currentUnityVersion)
                        return "outdated (version " + version + ")";
                    else
                        return "current (version " + Common.currentUnityVersion + ")";
                }
            }

            public bool Present => version != 0;
        }

        /// <summary>
        /// Offers a combined text entry/file selection dialog for the editor GUI
        /// </summary>
        /// <param name="label">Label of item</param>
        /// <param name="folder">Select a folder, not a file</param>
        /// <param name="save">Select to save, rather than to open</param>
        /// <param name="path">Suggested path (absolute or relative to project root)</param>
        /// <param name="extension">when saving a file, the file extension</param>
        /// <returns>The path to the item. Unchanged if the dialog has been canceled.</returns>
        public static string FileSelectionField(GUIContent label, bool folder, bool save, string path, string extension = null)
        {
            EditorGUILayout.BeginHorizontal();

            path = EditorGUILayout.TextField(label, path);

            if (GUILayout.Button("...", GUILayout.Width(20)))
            {
                string newPath = OpenFileDialog(path, folder, save, extension);
                if(!string.IsNullOrEmpty(newPath))
                    path = newPath;
            }

            EditorGUILayout.EndHorizontal();
            return path;
        }

        // The file and directory dialog need some serious boilerplate to actually be useful.
        public static string OpenFileDialog(string path, bool folder, bool save, string extension)
        {
            string newPath = folder
                ? save
                    ? EditorUtility.SaveFolderPanel("Select directory to save to", path, "")
                    : EditorUtility.OpenFolderPanel("Select directory to open", path, "")
                : save
                    ? EditorUtility.SaveFilePanel("Select file to save to", path, "", extension)
                    : EditorUtility.OpenFilePanel("Select file to open", path, "");
            if(newPath.StartsWith(Application.dataPath))
                newPath = "Assets" + newPath[Application.dataPath.Length..];

            return newPath;
        }

        /// <summary>
        /// Replace interpunction characters in a filename with '_'
        /// </summary>
        /// <param name="filename">Filename or path element name</param>
        /// <returns>Sanitized path element name</returns>
        public static string SanitizeFileName(string filename)
        {
            char[] chars = new char[filename.Length];

            for (int i = 0; i < filename.Length; i++)
            {
                char c = filename[i];
                chars[i] = (!Char.IsLetterOrDigit(c) && c != '_' && c != '.') ? '_' : c;
            }

            return new string(chars);
        }

        /// <summary>
        /// Create a temporary directory with a unique name
        /// </summary>
        /// <returns>Directory path</returns>
        public static string CreateTempDirectory()
        {
            string tmpDirName;
            do
            {
                tmpDirName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            } while (Directory.Exists(tmpDirName));

            Directory.CreateDirectory(tmpDirName);
            return tmpDirName;
        }

        /// <summary>
        /// Build a AltspaceVR compliant asset Bundle zip out of the given data
        /// </summary>
        /// <param name="assetFiles">Input files (Kit Prefabs or file with preformatted scene)</param>
        /// <param name="screenshotFiles">Kits: Screenshots to kit items</param>
        /// <param name="architectures">Architectures to build for</param>
        /// <param name="tgtRootName">Target root name (must match upload file name of zip file)</param>
        /// <param name="targetFileName">File name to locally save to (incl. .zip) or null to open dialog</param>
        /// <returns>The chosen filename</returns>
        public static string BuildAssetBundle(string[] assetFiles, List<BuildTarget> architectures, string tgtRootName, string[] screenshotFiles = null, string targetFileName = null)
        {
            string tmpSaveLocation = CreateTempDirectory();

            if(screenshotFiles != null)
            {
                string screenshotsSave = Path.Combine(tmpSaveLocation, "Screenshots");

                // Gather screenshots
                if(screenshotFiles.Length > 0)
                {
                    if(!Directory.Exists(screenshotsSave))
                        Directory.CreateDirectory(screenshotsSave);

                    foreach(string srcFile in screenshotFiles)
                    {
                        if(Path.GetExtension(srcFile) != ".png")
                            continue;

                        string srcFileName = Path.GetFileName(srcFile);
                        File.Copy(srcFile, Path.Combine(screenshotsSave, srcFileName));
                    }
                }
            }

            tgtRootName = SanitizeFileName(tgtRootName).ToLower();

            targetFileName ??= OpenFileDialog(Path.Combine(Application.dataPath, tgtRootName + ".zip"), false, true, "zip");

            if (string.IsNullOrEmpty(targetFileName))
            {
                Debug.Log("Build has been canceled.");
                return null;
            }

            AssetBundleBuild[] abb =
            {
                new AssetBundleBuild()
                {
                    assetBundleName = tgtRootName,
                    assetNames = assetFiles
                }
            };

            foreach (BuildTarget architecture in architectures)
            {
                string assetBundlesSave = Path.Combine(tmpSaveLocation, "AssetBundles");
                if (architecture == BuildTarget.Android)
                    assetBundlesSave = Path.Combine(assetBundlesSave, "Android");
                else if (architecture == BuildTarget.StandaloneOSX)
                    assetBundlesSave = Path.Combine(assetBundlesSave, "Mac");

                if (!Directory.Exists(assetBundlesSave))
                    Directory.CreateDirectory(assetBundlesSave);
                    BuildPipeline.BuildAssetBundles(
                        assetBundlesSave,
                        abb,
                        BuildAssetBundleOptions.StrictMode,
                        architecture);
            }

            File.Delete(targetFileName);
            CreateZip(tmpSaveLocation, targetFileName);

            Directory.Delete(tmpSaveLocation, true);
            return targetFileName;
        }

        public static Dictionary<BuildTarget, bool> supported_cache = new();

        /// <summary>
        /// Checks if build support for the given platform is loaded
        /// </summary>
        /// <param name="target">The target platform</param>
        /// <returns>true if build support is present</returns>
        public static bool IsBuildTargetSupported(BuildTarget target)
        {
            if(!supported_cache.TryGetValue(target, out bool res))
            {
                Type moduleManager = Type.GetType("UnityEditor.Modules.ModuleManager,UnityEditor.dll");

                MethodInfo getTargetStringFromBuildTarget = moduleManager.GetMethod(
                    "GetTargetStringFromBuildTarget",
                    BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo isPlatformSupportLoaded = moduleManager.GetMethod(
                    "IsPlatformSupportLoaded",
                    BindingFlags.Static | BindingFlags.NonPublic);

                string targetString = (string) getTargetStringFromBuildTarget.Invoke(null, new object[] { target });
                res = (bool) isPlatformSupportLoaded.Invoke(null, new object[] { targetString });

                supported_cache[target] = res;

                if(!res)
                    Debug.LogWarning("Build Support '" + targetString + "' is not installed, building for this platform will be disabled.");
            }

            return res;
        }

        /// <summary>
        /// Install a named resource from a template file within the package
        /// </summary>
        /// <param name="resname">Name of the resource</param>
        /// <param name="tgtpath">(optional) subdirectory within the assets folder</param>
        /// <returns></returns>
        public static bool InstallResource(string resName, string tgtPath = null)
        {
            string destPath;

            if(!string.IsNullOrEmpty(tgtPath))
            {
                Directory.CreateDirectory(Path.Combine("Assets", tgtPath));
                destPath = Path.Combine("Assets", tgtPath, resName);
            }
            else
            {
                destPath = Path.Combine("Assets", resName);
            }

            if(File.Exists(destPath))
            {
                Debug.Log("Skipped copying: " + destPath);
                return false;
            }

            string srcPath = Path.Combine(ResourceDirectory, resName);

            File.Copy(srcPath + ".in", destPath);
            if(File.Exists(srcPath + ".meta.in"))
                File.Copy(srcPath + ".meta.in", destPath + ".meta");

            Debug.Log("Copied " + resName + " to " + destPath);
            return true;
        }

        private static void CreateZip(string sourceDirectory, string outputFile) => ZipFile.CreateFromDirectory(sourceDirectory, outputFile);
    }
}

#endif // UNITY_EDITOR
