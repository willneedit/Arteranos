/*
 * Copyright (c) 2023, willneedit
 * 
 * Licensed by the Mozilla Public License 2.0,
 * residing in the LICENSE.md file in the project's root directory.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arteranos.Core
{
    public class SceneLoader : MonoBehaviour
    {
#if false
        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
        }
#endif

        public void InitiateLoad(string name) => StartCoroutine(LoadScene(name));


        public AsyncOperation loaderOp = null;

        public IEnumerator LoadScene(string name)
        {
            yield return null;

            AssetBundle myLoadedAssetBundle = AssetBundle.LoadFromFile(name);
            if(myLoadedAssetBundle == null)
            {
                Debug.Log("Failed to load AssetBundle!");
                yield break;
            }

            Debug.Log("Done loading AssetBundle.");

            Debug.Log($"Streamed Assed Bundle? {myLoadedAssetBundle.isStreamedSceneAssetBundle}");

            foreach(string assName in myLoadedAssetBundle.GetAllAssetNames())
                Debug.Log($"Asset: {assName}");

            foreach(string scenePath in myLoadedAssetBundle.GetAllScenePaths())
                Debug.Log($"Scene: {scenePath}");

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


        }

        private void OnCompleted(AsyncOperation obj)
        {
            Debug.Log($"Completed, Objects={SceneManager.GetActiveScene().GetRootGameObjects().Length}");
        }
    }
}
