/*
 * Copyright (c) 2024, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.XR;
using Arteranos.Core;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using Arteranos.UI;
using System;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;
using System.Threading.Tasks;
using Ipfs;
using Arteranos.Core.Operations;
using Arteranos.Services;
using Newtonsoft.Json;
using ProtoBuf;

namespace Arteranos.Editor
{
    public class KitMetaData
    {
        public string KitName = "Unnamed Kit";
        public string KitDescription = string.Empty;
        public UserID AuthorID = null;
        public DateTime Created = DateTime.MinValue;

        public string Serialize() => JsonConvert.SerializeObject(this, Formatting.Indented);

        public static KitMetaData Deserialize(string json) => JsonConvert.DeserializeObject<KitMetaData>(json);
    }

    [ProtoContract]
    public struct KitEntryItem
    {
        [ProtoMember(1)]
        public string Name;

        [ProtoMember(2)]
        public Guid GUID;

        public KitEntryItem(string name, Guid guid)
        {
            Name = name;
            GUID = guid;
        }
    }

    public class KitBuilderGUI : EditorWindow
    {
        public static KitMetaData metadata = null;
        public static Client client = null;

        private static bool inProgress = false;

        private GameObject[] gameObjects = null;
        private bool objectListFoldout = false;
        private string targetFile = null;

        public static void ShowGUI(GameObject[] gameObjects)
        {
            client ??= Client.Load();
            metadata ??= new() { AuthorID = client.MeUserID };

            KitBuilderGUI gui = GetWindow<KitBuilderGUI>();
            gui.gameObjects = gameObjects;
            gui.Show();
        }

        public static void FinishBuild()
        {
            inProgress = false;
            GetWindow<KitBuilderGUI>().Repaint();
        }

        public void OnGUI()
        {
            if (inProgress)
            {

                GUIStyle style = new()
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 24,
                    alignment = TextAnchor.MiddleCenter,
                };

                style.normal.textColor = new Color(0.80f, 0, 0);
                EditorGUILayout.LabelField("\nBuild in progress...", style);
                return;
            }


            if (metadata?.AuthorID == null)
            {
                Close();
                return;
            }

            EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(10, 10, 10, 10) });

            EditorGUILayout.LabelField("Author Name", metadata.AuthorID);

            metadata.KitName = EditorGUILayout.TextField("Kit Name", metadata.KitName);

            EditorGUILayout.LabelField("Kit description:");
            metadata.KitDescription = EditorGUILayout.TextArea(metadata.KitDescription);

            if (objectListFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
                objectListFoldout, "Kit object list"))
            {
                int i = 1;
                foreach(GameObject gameObject in gameObjects)
                {
                    EditorGUILayout.LabelField(i.ToString(), gameObject.name);
                    i++;
                }
            }

            targetFile = Common.FileSelectionField(
                new GUIContent("Target Zip File"),
                false,
                true,
                targetFile,
                "zip");

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Build Kit Zip File", new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold }))
            {
                inProgress = true;
                EditorCoroutineUtility.StartCoroutineOwnerless(
                    KitBuilder.CommitBuild(gameObjects, metadata));
            }


            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.EndVertical();
        }
    }

    public static class KitBuilder
    {
        [MenuItem("Assets/Create Kit from here...", true)]
        private static bool CreateKitFromHereValidation()
        {
            if (!TryGetExportNameAndGameObjects(out _, out GameObject[] objs)) return false;

            foreach (var obj in objs)
                if (AssetDatabase.GetAssetPath(obj) == null) return false;

            return true;
        }

        [MenuItem("Assets/Create Kit from here...", false, 21)]
        private static void CreateKitFromHere()
        {
            TryGetExportNameAndGameObjects(out _, out GameObject[] objs);

            KitBuilderGUI.ShowGUI(objs);
        }

        private static bool TryGetExportNameAndGameObjects(out string name, out GameObject[] gameObjects)
        {
            var transforms = Selection.GetTransforms(SelectionMode.Assets);
            if (transforms.Length > 0)
            {
                name = transforms.Length > 1
                    ? SceneManager.GetActiveScene().name
                    : Selection.activeObject.name;

                gameObjects = transforms.Select(x => x.gameObject).ToArray();
                return true;
            }

            name = null;
            gameObjects = null;
            return false;
        }


        public static IEnumerator CommitBuild(GameObject[] objs, KitMetaData metaData)
        {

            string tmpKitDirectory = $"_Kit{Path.GetRandomFileName()}.dir";
            Directory.CreateDirectory(tmpKitDirectory);

            List<(KitEntryItem, GameObject)> objGuids = new();

            IEnumerator AssembleKitItemDirectories()
            {
                string itemDirectory = $"{tmpKitDirectory}/KitItems";
                Directory.CreateDirectory(itemDirectory);

                string screenshotDirectory = $"{tmpKitDirectory}/KitScreenshots";
                Directory.CreateDirectory(screenshotDirectory);

                foreach (GameObject obj in objs)
                {

                    KitEntryItem item = new(obj.name, Guid.NewGuid());

                    using Stream ms = File.Create($"{screenshotDirectory}/{item.GUID}.png");
                    yield return EditorUtilities.CreateAssetPreviewStream(obj, ms);
                    ms.Flush();

                    using Stream stream = File.Create($"{itemDirectory}/{item.GUID}.map");
                    Serializer.Serialize(stream, item);
                    objGuids.Add((item, obj));
                }
            }

            IEnumerator AssembleMetaData()
            {
                string metadataFile = $"{tmpKitDirectory}/Metadata.json";

                metaData.Created = DateTime.UtcNow;

                string json = JsonConvert.SerializeObject(metaData, Formatting.Indented);
                File.WriteAllText(metadataFile, json);

                yield return null;
            }

            IEnumerator AssembleKitScreenshot()
            {
                string screenshotPNGFile = $"{tmpKitDirectory}/Screenshot.png";

                using Stream stream = File.Create(screenshotPNGFile);

                EditorUtilities.TakeSceneViewPhotoStream(stream);

                yield return null;
            }

            IEnumerator AssembleKitAssetBundle()
            {
                string gatheredAssetsDirectory = $"Assets/_Kit{Path.GetRandomFileName()}.dir";

                Directory.CreateDirectory(gatheredAssetsDirectory);

                List<string> gatheredAssets = new();

                foreach ((KitEntryItem, GameObject) a in objGuids)
                {
                    string gatheredAssetPath = $"{gatheredAssetsDirectory}/{a.Item1.GUID}.prefab";

                    AssetDatabase.CopyAsset(
                        AssetDatabase.GetAssetPath(a.Item2),
                        gatheredAssetPath);

                    gatheredAssets.Add(gatheredAssetPath);

                    yield return null;
                }

                const BuildTarget architecture = BuildTarget.StandaloneWindows64;

                string assetBundleDirectory = $"{tmpKitDirectory}/{Common.GetArchitectureDirName(architecture)}";
                Directory.CreateDirectory(assetBundleDirectory);

                AssetBundleBuild[] abb =
                {
                    new()
                    {
                        assetBundleName = $"{Guid.NewGuid()}.kit",
                        assetNames = gatheredAssets.ToArray()
                    }
                };

                BuildPipeline.BuildAssetBundles(
                    assetBundleDirectory, 
                    abb, 
                    BuildAssetBundleOptions.StrictMode, 
                    architecture);

                yield return null;
            }

            yield return AssembleKitItemDirectories();
            yield return AssembleMetaData();
            yield return AssembleKitScreenshot();
            yield return AssembleKitAssetBundle();

            KitBuilderGUI.FinishBuild();
            yield return null;
        }
    }
}
