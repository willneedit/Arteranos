#if UNITY_EDITOR

/*
 * UGLY HACK: Guiding You to the necessary Asset S(t)ore items with Your default browser,
 * download with Unity and importing into the project.
 * 
 * This is intended to keep Your (or, my) project's repository - public, private or corporate - 
 * clean of the required Asset Store items to avoid the hassles with Unity Store's
 * EULA.
 * 
 * In a nutshell, You MAY process Your asset store item in Your project, along with the
 * item's license (or the Unity Store EULA, if applicable), but, by the Unity Store EULA,
 * You MUST NOT distribute the Assert Store item, regardless as-is (the original .unitypackage
 * as downloaded from the Asset Store) or extracted (like imported and repackaged, then saved
 * in a cloud storage) unless the item's license supersedes the Unity Store EULA's limitations.
 * 
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bootstrap
{
    internal class AssetStoreLoader
    {
        static AssetStoreItem[] current = null;

        // --------------------------------------------------------------------
        #region ASSET STORE SHOPPING LIST
        public static AssetStoreItem[] GetShoppingList()
        {
            current ??= new AssetStoreItem[]
                {
                    new("Sci-fi GUI skin",
                        "https://assetstore.unity.com/packages/2d/gui/sci-fi-gui-skin-15606",
                        "3drina/Textures MaterialsGUI Skins/Sci-fi GUI skin",
                        "Sci-Fi UI"
                    )
                };

            return current;
        }

        #endregion
        // --------------------------------------------------------------------
        #region CONTROLLER

        public static void ImportUnityPackage(string path)
        {
            string openPath = Path.Combine(GetAssetStoreDirectory(), path);
            AssetDatabase.ImportPackage(openPath, false);
        }

        public static string GetAssetStoreDirectory()
        {
            string path = "";
            if(SystemInfo.operatingSystem.Contains("Windows"))
                path = InternalEditorUtility.unityPreferencesFolder + Path.DirectorySeparatorChar + "../../Asset Store-5.x";
            else if(SystemInfo.operatingSystem.Contains("Mac"))
                path = InternalEditorUtility.unityPreferencesFolder + Path.DirectorySeparatorChar + "../../../Unity/Asset Store-5.x";

            return path;
        }

        static void BackupScene()
        {
            Scene sc = SceneManager.GetActiveScene();

            // Save first
            if(sc.isDirty)
                EditorSceneManager.SaveScene(sc);

            SessionState.SetString("ASLoader_SavedScene", sc.path);



            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            EditorSceneManager.CloseScene(sc,true);
        }

        public static void RestoreScene()
        {
            Scene sc = SceneManager.GetActiveScene();

            string savedScene = SessionState.GetString("ASLoader_SavedScene", null);

            EditorSceneManager.OpenScene(savedScene);
            EditorSceneManager.CloseScene(sc, true);

            SessionState.EraseString("ASLoader_SavedScene");
        }


        [MenuItem("Tools/Reload Store Assets")]
        static void OnMenuSelected()
        {
            SessionState.SetBool("ASLoader_Startup", false);
            EditorApplication.update += RunAssetStoreLoader;
        }

        [InitializeOnLoadMethod]
        static void OnInitializeOnLoad() => EditorApplication.update += RunAssetStoreLoader;

        static void RunAssetStoreLoader()
        {
            EditorApplication.update -= RunAssetStoreLoader;

            GameObject go = null;
            current = null;
            AssetStoreWorker aw = null;

            string savedScene = SessionState.GetString("ASLoader_SavedScene", null);
            if(!string.IsNullOrEmpty(savedScene))
            {
                Debug.Log("Resuming operation...");
                go = GameObject.Find("AssetLoaderWorker");
                if(go) aw = go.GetComponent<AssetStoreWorker>();

                // The RestoreScene() is scheduled right before exit of the Worker.
                // RestoreScene();
            }
            else
            {
                if(SessionState.GetBool("ASLoader_Startup", false)) return;
                SessionState.GetBool("ASLoader_Startup", true);

                // First to check if we're something to do at all.
                bool okay = true;
                AssetStoreItem[] array = GetShoppingList();
                for(int i = 0; i < array.Length; i++)
                {
                    AssetStoreItem item = array[i];
                    _ = item.Analyze();
                    okay &= item.IsOk();
                }

                if(okay) return;

                BackupScene();

                Debug.Log($"Asset Store Loader starting - Asset Store cache directory: {GetAssetStoreDirectory()}");
                go = new("AssetLoaderWorker");
                aw = go.AddComponent<AssetStoreWorker>();
            }

            if(aw != null) EditorApplication.update += aw.Invoker;
        }
    }

    #endregion


    public class AssetStoreWorker : MonoBehaviour
    {
        // --------------------------------------------------------------------
        #region WORKER

        IEnumerator WorkThroughShoppingList()
        {
            bool issues = true;

            while(issues)
            {
                yield return null;

                issues = false;
                AssetStoreItem[] items = AssetStoreLoader.GetShoppingList();
                for(int i = 0,  c = items.Count(); i < c; i++)
                {
                    if(items[i].IsOk()) continue;

                    issues = true;
                    AssetStoreState state = items[i].Analyze();

                    if(state == AssetStoreState.NotShown)
                    {
                        items[i].State = RequestStoreDownload(items[i]);
                        break;
                    }
                    if(state == AssetStoreState.NotExtracted)
                    {
                        AssetStoreLoader.ImportUnityPackage(items[i].packageName + ".unitypackage");
                        break;
                    }
                }
            }

            EditorApplication.update += Cleanup;
        }

        private void Cleanup()
        {
            EditorApplication.update -= Cleanup;

            AssetStoreLoader.RestoreScene();
            // DestroyImmediate(this.gameObject);
        }
        private AssetStoreState RequestStoreDownload(AssetStoreItem item)
        {
            bool result = EditorUtility.DisplayDialog("Request",
                $"This project needs the asset store item\n" +
                $"'{item.name}'.\n" +
                $"We'd open your browser, and you need to perform\n" +
                $"the motions to acquire the requested store asset.\n\n" +
                $"You can do that later with the menu 'Tools/Reload Store Assets'\n",
                $"Go ahead", "Later, thanks");

            if(result)
            {
                Application.OpenURL(item.itemURL);
                return AssetStoreState.NotDownloaded;
            }
            else
            {
                return AssetStoreState.Skipped;
            }

        }
        
        public void Invoker()
        {
            EditorApplication.update -= Invoker;
            StartCoroutine(WorkThroughShoppingList());
        }

        #endregion
    }

    class AssetStoreItem
    {
        public string name;         // The Store Asset to aim for
        public string itemURL;      // The Store Asset URL, pointing to this item
        public string packageName;  // The .unitypackage residing in Your Unity preferences
        public string targetPath;   // The root path for the extracted asset
        public long size;
        public AssetStoreState State { get; set; } // Current state

        public AssetStoreItem(string name, string itemURL, string packageName, string targetPath)
        {
            this.name = name;
            this.itemURL = itemURL;
            this.packageName = packageName;
            this.targetPath = targetPath;
            this.State = AssetStoreState.NotVisited;
            this.size = 0;
        }

        public bool IsOk() => (State == AssetStoreState.OK || State == AssetStoreState.Skipped);

        public AssetStoreState Analyze()
        {
            // Already seen where we are, we're home free
            if(State == AssetStoreState.OK || State == AssetStoreState.Skipped) return State;

            // Download request pending
            if(State == AssetStoreState.NotDownloaded)
            {
                string str3 = Path.Combine(AssetStoreLoader.GetAssetStoreDirectory(), packageName + ".unitypackage");

                long length = 0;
                try
                {
                    length = new FileInfo(str3).Length;
                }
                catch(Exception)
                {

                }

                if(length != size || size == 0)
                {
                    size = length;
                    return State;
                }

                return State = AssetStoreState.NotExtracted;
            }

            string str = Path.Combine("Assets", targetPath);
            if(Directory.Exists(str))
                return State = AssetStoreState.OK;

            string str2 = Path.Combine(AssetStoreLoader.GetAssetStoreDirectory(), packageName + ".unitypackage");
            if(File.Exists(str2))
                return State = AssetStoreState.NotExtracted;

            return State = AssetStoreState.NotShown;

        }
    }

    enum AssetStoreState
    {
        NotVisited = 0,     // Haven't seen there yet
        Skipped,            // That's what he said
        OK,                 // All present and accounted for
        NotExtracted,       // Item needs extraction
        NotDownloaded,      // Item hasn't downloaded from the Store yet
        NotShown            // The user needs to visit the Store and perform the transaction
    }

}

#endif
