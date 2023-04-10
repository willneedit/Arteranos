/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using Arteranos.XR;
using Arteranos.Core;

using Codice.Utils;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arteranos.Editor
{
    public class SceneBuilder : MonoBehaviour
    {
        public static List<string> gatheredAssets = new();

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

        public static (string, string) BuildTemplateScene()
        {
            Scene sc = SceneManager.GetActiveScene();
            string origPath = sc.path;

            // Save first
            if(sc.isDirty)
                EditorSceneManager.SaveScene(sc);

            string tmpSceneName = Path.Combine("Assets", "_" + Path.GetRandomFileName() + ".unity");

            // Save in a temporary scene, using a different file name
            EditorSceneManager.SaveScene(sc, tmpSceneName);

            ScrubEssentials();

            ReparentObjects(sc);

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

        private static void ScrubEssentials()
        {
            foreach(Camera camera in FindObjectsOfType<Camera>())
                DestroyImmediate(camera.gameObject);

            XRControl xrc;
            while(xrc = FindObjectOfType<XRControl>())
                DestroyImmediate(xrc.gameObject);

            Persistence p;
            while(p = FindObjectOfType<Persistence>())
                DestroyImmediate(p.gameObject);
        }

        private static void SaveRenderSettingsAssets()
        {
            RenderSettingsJSON j = new();
            string json = j.BackupRS();

            TextAsset ta = new(json);
            GatherAsset(ta, "Assets/RenderSettings.json.asset");

            if(RenderSettings.customReflection != null)
                GatherAsset(RenderSettings.customReflection, "Assets/SkyReflectTexture.png");

            if(RenderSettings.skybox != null)
            {
                Material mat = new(RenderSettings.skybox);
                GatherAsset(mat, "Assets/Skybox.mat");
            }
        }

        [MenuItem("Arteranos/Build scene as world", false, 10)]
        public static void BuildSceneAsWorld()
        {
            gatheredAssets.Clear();

            SaveRenderSettingsAssets();

            string tmpScenePath;
            string itemPath;
            (tmpScenePath, itemPath) = BuildTemplateScene();

            Debug.Log(tmpScenePath);
            Debug.Log(itemPath);

            GatherPrefab(GameObject.Find("Environment"), "Assets/Environment.prefab");

            List<BuildTarget> targets = new()
            {
                BuildTarget.StandaloneWindows64
            };


            Common.BuildAssetBundle(gatheredAssets.ToArray(), targets, itemPath);

            EditorSceneManager.OpenScene(itemPath);

            AssetDatabase.DeleteAsset(tmpScenePath);

            foreach(string path in gatheredAssets)
                AssetDatabase.DeleteAsset(path);
        }


        [MenuItem("Arteranos/Test world...", false, 20)]
        public static void TestWorld()
        {

            string ABName = Common.OpenFileDialog("", false, false, ".unity");
            if(string.IsNullOrEmpty(ABName)) return;

            GameObject go = new("_SceneLoader");
            go.AddComponent<Persistence>();
            SceneLoader sl = go.AddComponent<SceneLoader>();
            sl.Name = ABName;

            if(!EditorApplication.isPlaying)
                EditorApplication.EnterPlaymode();

        }

        // Wipe off the scene loader in the saved scene in Edit mode on reentering it.
        [InitializeOnLoad]
        public static class OnReenterEditMode
        {
            static OnReenterEditMode()
            {
                EditorApplication.playModeStateChanged += LogPlayModeState;
            }

            private static void LogPlayModeState(PlayModeStateChange state)
            {
                // Debug.Log(state);
                if(state == PlayModeStateChange.EnteredEditMode)
                {
                    SceneLoader sl = FindObjectOfType<SceneLoader>();
                    if(sl != null) DestroyImmediate(sl.gameObject);
                }
            }
        }
#if false
        static IEnumerator InstantiateObject()
        {
            string url = "file:///" + Common.OpenFileDialog("", false, false, ".unity");
            UnityEngine.Networking.UnityWebRequest request
                = UnityEngine.Networking.UnityWebRequestAssetBundle.GetAssetBundle(url, 0);
            yield return request.Send();
            AssetBundle bundle = UnityEngine.Networking.DownloadHandlerAssetBundle.GetContent(request);
            GameObject cube = bundle.LoadAsset<GameObject>("Sphere");
            Instantiate(cube);
        }
#endif
    }
}