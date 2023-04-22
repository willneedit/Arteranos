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
using System.IO;

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
                StartCoroutine(LoadScene(Name));
            }
        }

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
            Name = null;

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

            Scene newScene = SceneManager.CreateScene($"Scene_{Path.GetRandomFileName()}");

            yield return null;

            SceneManager.SetActiveScene(newScene);

            AssetBundleRequest abrGO = loadedAB.LoadAssetAsync<GameObject>("Assets/Root/Environment.prefab");
            AssetBundleRequest abrLL = loadedAB.LoadAssetAsync<GameObject>("Assets/Root/LevelLightmapData.prefab");
            // AssetBundleRequest abrLS = loadedAB.LoadAssetAsync<LightingSettings>("Assets/Root/LightingSettings.lighting");

            while(!abrGO.isDone) yield return null;

            GameObject environment = abrGO.asset as GameObject;
            if(environment == null)
            {
                // Must be horribly wrong. Or, someone tampered with the world data.
                Debug.LogError("Cannot load the Environment asset");
                SceneManager.UnloadSceneAsync(newScene);
                yield break;
            }

            environment.SetActive(false);

            Debug.Log("Populating scene...");

            //Do we really need it in a playback setting?
            //while(!abrLS.isDone) yield return null;

            //Lightmapping.lightingSettings = abrLS.asset as LightingSettings;

            GameObject go = Instantiate(environment);
            StripScripts(go.transform);

            Debug.Log("Adding lighting data...");

            while(!abrLL.isDone) yield return null;

            GameObject llGO = Instantiate(abrLL.asset as GameObject);

            LevelLightmapData lld = llGO.GetComponent<LevelLightmapData>();

            Debug.Log("Populating scene done, setting active...");
            go.SetActive(true);

            lld.allowLoadingLightingScenes = false;
            lld.LoadLightingScenarioData(0);

            Debug.Log("Scene is live.");

            Debug.Log("Loader finished, cleaning up.");

            loadedAB.Unload(false);

            SceneManager.UnloadSceneAsync(prev);

            Destroy(gameObject);

        }

    }
}
