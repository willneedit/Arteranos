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

        private static string BuildEnvironsPrefab()
        {
            GameObject env = GameObject.Find("Environment");

            string tmpPrefabName = "Assets/Environment.prefab";

            PrefabUtility.SaveAsPrefabAsset(env, tmpPrefabName);

            return tmpPrefabName;
        }

        [MenuItem("Arteranos/Build scene as world", false, 10)]
        public static void BuildSceneAsWorld()
        {
            string tmpScenePath;
            string itemPath;
            (tmpScenePath, itemPath) = BuildTemplateScene();

            Debug.Log(tmpScenePath);
            Debug.Log(itemPath);

            string tmpPrefabName = BuildEnvironsPrefab();

            Debug.Log(tmpPrefabName);

            string txt = File.ReadAllText(tmpScenePath);
            TextAsset ta = new(txt);
            AssetDatabase.CreateAsset(ta, tmpScenePath + ".asset");

            List<BuildTarget> targets = new()
            {
                BuildTarget.StandaloneWindows64
            };

            string[] assetFiles = new string[]
            {
                tmpPrefabName,
                //tmpScenePath + ".asset"
            };

            Common.BuildAssetBundle(assetFiles, targets, itemPath);

            EditorSceneManager.OpenScene(itemPath);

            AssetDatabase.DeleteAsset(tmpScenePath + ".asset");
            AssetDatabase.DeleteAsset(tmpScenePath);

            AssetDatabase.DeleteAsset(tmpPrefabName);
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