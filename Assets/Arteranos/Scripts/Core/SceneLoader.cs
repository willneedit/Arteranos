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
using Newtonsoft.Json;


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
            string target = "Arteranos/Scenes/Blank-1";

            if(prev.name == "Blank-1")
                target = "Arteranos/Scenes/Blank-2";

            SceneManager.LoadSceneAsync(target);

            AssetBundleRequest abrGO = loadedAB.LoadAssetAsync<GameObject>("Assets/Environment.prefab");
            AssetBundleRequest abrSB = loadedAB.LoadAssetAsync<Material>("Assets/Skybox.mat");
            AssetBundleRequest abrRS = loadedAB.LoadAssetAsync<TextAsset>("Assets/RenderSettings.json.asset");

            while(!abrRS.isDone)
                yield return null;

            if(abrRS.asset as TextAsset != null)
            {
                RenderSettingsJSON j = new();
                j.RestoreRS((abrRS.asset as TextAsset).text);
            }

            while(!abrSB.isDone)
                yield return null;

            if(abrSB.asset as Material != null)
                RenderSettings.skybox = abrSB.asset as Material;

            while(!abrGO.isDone)
                yield return null;

            GameObject environment = abrGO.asset as GameObject;
            if(environment == null)
            {
                Debug.LogError("Cannot load the Environment asset");
                yield break;
            }

            environment.SetActive(false);

            Debug.Log("Populating scene...");

            Scene sc = SceneManager.GetActiveScene();

            GameObject[] gobs = sc.GetRootGameObjects();

            foreach(GameObject gob in gobs)
                Destroy(gob);

            GameObject go = Instantiate(environment);
            StripScripts(go.transform);


            Debug.Log("Populating scene done, setting active...");
            go.SetActive(true);

            Debug.Log("Scene is live.");

            Debug.Log("Loader finished, cleaning up.");

            loadedAB.Unload(false);

            Destroy(gameObject);

        }

    }

    public class RenderSettingsJSON
    {
        public Color ambientSkyColor;
        public Color ambientEquatorColor;
        public Color ambientGroundColor;
        public float ambientIntensity;
        public Color ambientLight;
        public UnityEngine.Rendering.AmbientMode ambientMode;
        // public Rendering.SphericalHarmonicsL2 ambientProbe;
        public UnityEngine.Rendering.DefaultReflectionMode defaultReflectionMode;
        public int defaultReflectionResolution;
        public float flareFadeSpeed;
        public float flareStrength;
        public bool fog;
        public Color fogColor;
        public float fogDensity;
        public float fogEndDistance;
        public float fogStartDistance;
        public FogMode fogMode;
        public float haloStrength;
        public int reflectionBounces;
        public float reflectionIntensity;
        public Color subtractiveShadowColor;
        public Light sun;

        private Texture customReflection;
        private Material skybox;

        public string BackupRS()
        {
            ambientSkyColor = RenderSettings.ambientSkyColor;
            ambientEquatorColor = RenderSettings.ambientEquatorColor;
            ambientGroundColor = RenderSettings.ambientGroundColor;
            ambientIntensity = RenderSettings.ambientIntensity;
            ambientLight = RenderSettings.ambientLight;
            ambientMode = RenderSettings.ambientMode;
            //ambientProbe = RenderSettings.ambientProbe;
            defaultReflectionMode = RenderSettings.defaultReflectionMode;
            defaultReflectionResolution = RenderSettings.defaultReflectionResolution;
            flareFadeSpeed = RenderSettings.flareFadeSpeed;
            flareStrength = RenderSettings.flareStrength;
            fog = RenderSettings.fog;
            fogColor = RenderSettings.fogColor;
            fogDensity = RenderSettings.fogDensity;
            fogEndDistance = RenderSettings.fogEndDistance;
            fogStartDistance = RenderSettings.fogStartDistance;
            fogMode = RenderSettings.fogMode;
            haloStrength = RenderSettings.haloStrength;
            reflectionBounces = RenderSettings.reflectionBounces;
            reflectionIntensity = RenderSettings.reflectionIntensity;
            subtractiveShadowColor = RenderSettings.subtractiveShadowColor;
            sun = RenderSettings.sun;

            customReflection = RenderSettings.customReflection;
            skybox = RenderSettings.skybox;

            return JsonUtility.ToJson(this);
        }

        public void RestoreRS(string json)
        {
            JsonUtility.FromJsonOverwrite(json, this);

            RenderSettings.ambientMode = ambientMode;
            switch(ambientMode)
            {
                case UnityEngine.Rendering.AmbientMode.Skybox:
                    RenderSettings.ambientIntensity = ambientIntensity;
                    break;
                case UnityEngine.Rendering.AmbientMode.Trilight:
                    RenderSettings.ambientSkyColor = ambientSkyColor;
                    RenderSettings.ambientEquatorColor = ambientEquatorColor;
                    RenderSettings.ambientGroundColor = ambientGroundColor;
                    break;
                case UnityEngine.Rendering.AmbientMode.Flat:
                    RenderSettings.ambientSkyColor = ambientSkyColor;
                    break;
                case UnityEngine.Rendering.AmbientMode.Custom:
                    RenderSettings.ambientLight = ambientLight;
                    break;
            }

            //RenderSettings.ambientProbe = ambientProbe;
            RenderSettings.defaultReflectionMode = defaultReflectionMode;
            RenderSettings.defaultReflectionResolution = defaultReflectionResolution;
            RenderSettings.flareFadeSpeed = flareFadeSpeed;
            RenderSettings.flareStrength = flareStrength;
            RenderSettings.fog = fog;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogEndDistance = fogEndDistance;
            RenderSettings.fogStartDistance = fogStartDistance;
            RenderSettings.fogMode = fogMode;
            RenderSettings.haloStrength = haloStrength;
            RenderSettings.reflectionBounces = reflectionBounces;
            RenderSettings.reflectionIntensity = reflectionIntensity;
            RenderSettings.subtractiveShadowColor = subtractiveShadowColor;
            RenderSettings.sun = sun;

            // RenderSettings.customReflection = customReflection;
            // RenderSettings.skybox = skybox;
        }
    }
}
