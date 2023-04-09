/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arteranos.Core
{
    public class SceneLoader : MonoBehaviour
    {
        public string Name = null;

        private readonly List<string> TNWhitelist = new()
        {
            "UnityEngine.",
            "TMPro.",
            "Arteranos.User"
        };

        private readonly List<string> AssWhiteList = new()
        {
            "UnityEngine.AnimationModule",
            "UnityEngine.AudioModule",
            "UnityEngine.ClothModule",
            "UnityEngine.CoreModule",
            "UnityEngine.DirectorModule",
            "UnityEngine.GIModule",
            "UnityEngine.ParticleSystemModule",
            "UnityEngine.PhysicsModule",
            "UnityEngine.SpriteMaskModule",
            "UnityEngine.SpriteShapeModule",
            "UnityEngine.TerrainModule",
            "UnityEngine.TerrainPhysicsModule",
            "UnityEngine.UIModule",

            "UnityEngine.UI",
            "Unity.XR.Interaction.Toolkit",
            "Unity.RenderPipelines.Universal.Runtime",
            "Unity.TextMeshPro",

            "Arteranos.User"

        };

        void Start()
        {
            Debug.Log($"Loader deployed, name={Name}");
            if(Name != null)
            {
                InitiateLoad(Name);
                Name = null;
            }
        }

        public void InitiateLoad(string name) => StartCoroutine(LoadScene(name));

        private bool MatchWith(string name, List<string> patterns)
        {
            foreach(string pattern in patterns)
                if(name.StartsWith(pattern)) return true;

            return false;
        }

        public bool CheckComponent(Component component)
        {
            System.Type type = component.GetType();
            if(type == null) return false;

            if(!MatchWith(type.FullName, TNWhitelist)) return false;

            Assembly asm = type.Assembly;

            if(!MatchWith(asm.GetName().Name, AssWhiteList)) return false;

            return true;
        }

        public void StripScripts(Transform transform)
        {
            Component[] components = transform.GetComponents<Component>();
            foreach(Component component in components)
            {
                if(component == null)
                {
                    // Can't grasp it because it has missing code.
                    // Debug.LogWarning($"Detected defunct component in {transform.name}");
                }
                else if(!CheckComponent(component))
                {
                    System.Type type = component.GetType();
                    Assembly asm = type.Assembly;

                    Debug.LogWarning($"Removing {component.GetType().FullName} ({asm.GetName().Name}) in {transform.name}");
                    if(component as Behaviour != null)
                        (component as Behaviour).enabled = false;

                    // Really have to use it. Unwanted scripts have to move away as quick as possible.
                    DestroyImmediate(component);
                }
            }

            for(int i = 0, c = transform.childCount; i < c; ++i)
                StripScripts(transform.GetChild(i));
        }

        public IEnumerator LoadScene(string name)
        {
            yield return null;

            AssetBundle loadedAB = AssetBundle.LoadFromFile(name);
            if(loadedAB == null)
            {
                Debug.Log("Failed to load AssetBundle!");
                yield break;
            }

            Debug.Log("Done loading AssetBundle.");

            Debug.Log($"Streamed Assed Bundle? {loadedAB.isStreamedSceneAssetBundle}");

            if(loadedAB.isStreamedSceneAssetBundle)
            {
                Debug.LogError("This is a streamed scene assetbundle, which we don't want to.");
                yield break;
            }

            foreach(string assName in loadedAB.GetAllAssetNames())
                Debug.Log($"Asset: {assName}");

            foreach(string scenePath in loadedAB.GetAllScenePaths())
                Debug.Log($"Scene: {scenePath}");

            Scene prev = SceneManager.GetActiveScene();
            Scene sc = SceneManager.CreateScene("NewScene");
            SceneManager.SetActiveScene(sc);

            AssetBundleRequest abr = loadedAB.LoadAssetAsync<GameObject>("Assets/Environment.prefab");

            while(!abr.isDone)
            {
                yield return null;

                Debug.Log($"Progress: {abr.progress}");
            }

            GameObject environment = abr.asset as GameObject;
            if(environment == null)
            {
                Debug.LogError("Cannot load the Environment asset");
                yield break;
            }

            environment.SetActive(false);

            Debug.Log("Populating scene...");
            GameObject go = Instantiate(environment);
            StripScripts(go.transform);

            Debug.Log("Populating scene done, setting active...");
            go.SetActive(true);
            Debug.Log("Scene is live.");

            _ = SceneManager.UnloadSceneAsync(prev);

            Debug.Log("Loader finished, cleaning up.");
            Destroy(gameObject);


#if false
            string first = myLoadedAssetBundle.GetAllScenePaths()[0];

            Scene prev = SceneManager.GetActiveScene();

            Debug.Log($"Loading first scene: {first}");

            loaderOp = SceneManager.LoadSceneAsync(first);
            loaderOp.allowSceneActivation = false;
            loaderOp.completed += OnCompleted;
            loaderOp.priority = 90000;

            Scene loaded = SceneManager.GetSceneByPath(first);

            Debug.Log($"Done loading the scene {loaded.name}");

            //foreach(GameObject go in sc.GetRootGameObjects())
            //{
            //    Debug.Log($"Deactivating {go.name}");
            //    go.SetActive(false);
            //}

            //Debug.Log("Deactivating all root objects done");


            //myLoadedAssetBundle.Unload(false);

            Scene boot = SceneManager.CreateScene("Bootstrap");

            if(SceneManager.SetActiveScene(boot))
                Debug.Log("Switching to bootstrap");

            _ = SceneManager.UnloadSceneAsync(prev);

            // SceneManager.UnloadScene(loaded);

            Debug.Log("Loader function finished.");

            while(loaderOp.progress < 0.9f)
                yield return null;

            loaderOp.allowSceneActivation = true;

            Debug.Log($"Allowed scene activation. Progress={loaderOp.progress}, Objects={loaded.GetRootGameObjects().Length}");

            yield return null;

            Debug.Log($"List of objects, Objects={loaded.GetRootGameObjects().Length}");

            yield return null;

            Debug.Log($"List of objects, Objects={loaded.GetRootGameObjects().Length}");


            // SceneManager.SetActiveScene(boot);

            Debug.Log("Scene loading completed.");

#endif
        }

    }
}
