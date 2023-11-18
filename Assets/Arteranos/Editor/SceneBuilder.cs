/*
 * Copyright (c) 2023, willneedit
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
using Arteranos.Web;

namespace Arteranos.Editor
{
    public class SceneBuilderGUI : EditorWindow
    {
        public WorldMetaData metadata = null;
        public string targetFile = string.Empty;
        public string screenshotFile = string.Empty;

        public bool inProgress = false;


        [MenuItem("Arteranos/Build scene as world", false, 10)]
        public static void ShowScenebuilderGUI()
        {
            SceneBuilderGUI window = GetWindow<SceneBuilderGUI>("World building");
            window.Show();
        }

        public void OnGUI()
        {
            if(inProgress)
            {

                GUIStyle style = new() { 
                    fontStyle = FontStyle.Bold, 
                    fontSize = 24,
                    alignment= TextAnchor.MiddleCenter,
                };

                style.normal.textColor = new Color(0.80f, 0, 0);
                EditorGUILayout.LabelField("\nBuild in progress...", style);
                return;
            }

            metadata ??= WorldMetaData.LoadDefaults();

            EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(10, 10, 10, 10) });

            metadata.WorldName = EditorGUILayout.TextField("World Name", metadata.WorldName);

            metadata.Author = EditorGUILayout.TextField("Author Name", metadata.Author);

            screenshotFile = Common.FileSelectionField(
                new GUIContent("Screenshot"),
                false,
                false,
                screenshotFile);

            targetFile = Common.FileSelectionField(
                new GUIContent("Target Zip File"),
                false,
                true,
                targetFile,
                "zip");

            EditorGUILayout.Space(10);

            if(GUILayout.Button("Build World Zip File", new GUIStyle(GUI.skin.button)
                {
                    fontStyle = FontStyle.Bold
                }))
            {
                CommitBuild(metadata, targetFile, screenshotFile);
            }

            EditorGUILayout.Space(10);

            if(GUILayout.Button("Reload defaults"))
            {
                metadata = WorldMetaData.LoadDefaults();
                Repaint();
            }

            if(GUILayout.Button("Save defaults"))
                metadata.SaveDefaults();

            EditorGUILayout.EndVertical();
        }

        private void CommitBuild(WorldMetaData metadata, string targetFile, string screenshotFile)
        {
            void CompletedBuild()
            {
                inProgress = false;
                // Close();
                SceneBuilder.OnCompletedBuild -= CompletedBuild;
            }

            inProgress = true;

            SceneBuilder.metadata = metadata;
            SceneBuilder.targetFile = targetFile;
            SceneBuilder.screenshotFile = screenshotFile;

            SceneBuilder.OnCompletedBuild += CompletedBuild;

            SceneBuilder.BuildSceneAsWorld();
        }
    }

    public class SceneBuilder : ScriptableObject
    {
        public static readonly string ROOT_PATH = "Assets/Root/";

        public static event System.Action OnCompletedBuild;

        public static WorldMetaData metadata;
        public static string screenshotFile;
        public static string targetFile;

        private static readonly List<string> gatheredAssets = new();

        public static void GatherAsset(Object asset, string path)
        {
            AssetDatabase.CreateAsset(asset, path);
            gatheredAssets.Add(path);
        }

        public static void GatherPrefab(GameObject instanceRoot, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(instanceRoot, path);
            gatheredAssets.Add(path);
        }

        public static void GatherCopiedAsset(Object asset, string path)
        {
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(asset), path);
            gatheredAssets.Add(path);
        }

        public static (string, string) BuildTemplateScene()
        {
            Scene sc = SceneManager.GetActiveScene();
            string origPath = sc.path;

            // Save first
            if(sc.isDirty)
                EditorSceneManager.SaveScene(sc);

            string tmpSceneName = ROOT_PATH + "_" + Path.GetRandomFileName() + ".unity";

            // Save in a temporary scene, using a different file name
            EditorSceneManager.SaveScene(sc, tmpSceneName);

            ScrubEssentials();

            ReparentObjects(sc);

            RelayerTree();

            EditorSceneManager.SaveScene(sc);

            return (tmpSceneName, origPath);
        }

        private static void ReparentObjects(Scene sc)
        {
            GameObject env = GameObject.Find("Environment");

            if(env == null)
                env = new GameObject("Environment");

            List<GameObject> looseObjects = sc.GetRootGameObjects().ToList().FindAll(x =>
                x.name != "Environment"
            );

            // Reparent all the loose objects below "Environment"
            foreach(GameObject l in looseObjects) l.transform.SetParent(env.transform);
        }

        private static void RelayerTree() => RelayerTree(GameObject.Find("Environment"));

        private static void RelayerTree(GameObject go)
        {
            go.layer = 0;
            for(int i = 0; i < go.transform.childCount; ++i)
                RelayerTree(go.transform.GetChild(i).gameObject);
        }

        private static void ScrubEssentials()
        {
            foreach(Camera camera in FindObjectsOfType<Camera>())
                DestroyImmediate(camera.gameObject);

            IXRControl xrc;
            if((xrc = XRControl.Instance) != null)
                DestroyImmediate(xrc.gameObject);

            Persistence p;
            while(p = FindObjectOfType<Persistence>())
                DestroyImmediate(p.gameObject);
        }

        private static void SaveLightData()
        {
            SetupLLDMenuItem.BootstrapLLD(out LevelLightmapData lld, out LightingScenarioData lsd);

            GatherAsset(lsd, ROOT_PATH + "LightingScenarioData.asset");
            GatherPrefab(lld.gameObject, ROOT_PATH + "LevelLightmapData.prefab");

            if(Lightmapping.lightingSettings != null)
                GatherCopiedAsset(Lightmapping.lightingSettings, ROOT_PATH + "LightingSettings.lighting");
        }

        public static void BuildSceneAsWorld()
        {
            static IEnumerator Cleanup(string itemPath)
            {
                EditorSceneManager.OpenScene(itemPath);

                yield return null;

                AssetDatabase.DeleteAsset(ROOT_PATH[0..^1]);
                OnCompletedBuild?.Invoke();
            }

            AssetDatabase.CreateFolder("Assets", "Root");

            gatheredAssets.Clear();

            string tmpScenePath;
            string itemPath;
            (tmpScenePath, itemPath) = BuildTemplateScene();

            SaveLightData();

            GatherPrefab(GameObject.Find("Environment"), ROOT_PATH + "Environment.prefab");

            LightingScenarioDataFactory f = ObjectFactory.CreateInstance<LightingScenarioDataFactory>();
            f.OnBakingDone += () =>
            {
                CompileWorldData(itemPath);
                EditorCoroutineUtility.StartCoroutineOwnerless(Cleanup(itemPath));
                DestroyImmediate(f);
            };

            LevelLightmapData lld = FindObjectOfType<LevelLightmapData>();
            lld.lightingScenariosData[0].storeRendererInfos = true;
            lld.lightingScenariosData[0].geometrySceneName = itemPath;

            f.GenerateLightingScenarioData(lld.lightingScenariosData[0], false);
        }

        private static void CompileWorldData(string itemPath)
        {

            Debug.Log("Baking done, compiling Asset Bundle");

            List<BuildTarget> targets = new()
            {
                BuildTarget.StandaloneWindows64
            };

            if(string.IsNullOrEmpty(targetFile)) targetFile = null;

            metadata.Created = System.DateTime.Now;

            Common.BuildAssetBundle(
                gatheredAssets.ToArray(),
                targets,
                itemPath,
                metadata.Serialize(),
                screenshotFile,
                targetFile);
        }

        [MenuItem("Arteranos/Test world...", false, 20)]
        public static void TestWorld()
        {
            if(EditorApplication.isPlaying)
            {
                Debug.LogError("You already are in the play mode - this menu item is supposed to test the world while editing.");
                return;
            }

            string worldZipName = Common.OpenFileDialog("", false, false, ".zip");
            if(string.IsNullOrEmpty(worldZipName)) return;

            SessionState.SetString("ENTER_TEST_WORLD", $"file://{worldZipName}");

            if(!EditorApplication.isPlaying)
                EditorApplication.EnterPlaymode();

        }

        // Wipe off the scene loader in the saved scene in Edit mode on reentering it.
        [InitializeOnLoad]
        public static class OnEditModeChanged
        {
            static OnEditModeChanged()
            {
                EditorApplication.playModeStateChanged += LogPlayModeState;
            }

            private static void LogPlayModeState(PlayModeStateChange state)
            {
                if(state == PlayModeStateChange.EnteredEditMode)
                {
                    SceneLoader sl = FindObjectOfType<SceneLoader>();
                    if(sl != null) DestroyImmediate(sl.gameObject);
                }

                if(state == PlayModeStateChange.EnteredPlayMode)
                {
                    string testWorldZip = SessionState.GetString("ENTER_TEST_WORLD", string.Empty);
                    SessionState.EraseString("ENTER_TEST_WORLD");

                    if(!string.IsNullOrEmpty(testWorldZip))
                    {
                        IProgressUI pui = ProgressUIFactory.New();

                        pui.SetupAsyncOperations(() => WorldDownloader.PrepareDownloadWorld(testWorldZip, true));

                        pui.Completed += (context) => OnLoadWorldComplete(context);
                        pui.Faulted += OnLoadWorldFaulted;
                    }
                }
            }

            private static void OnLoadWorldFaulted(System.Exception ex, Context _context) => Debug.LogError($"Error in loading world: {ex.Message}");
            private static void OnLoadWorldComplete(Context _context)
            {
                Debug.Log("World data file loading and unpacking succeeded.");
                WorldDownloader.EnterDownloadedWorld(_context);
            }
        }
    }
}