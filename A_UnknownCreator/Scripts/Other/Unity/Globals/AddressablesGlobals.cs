using System;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace UnknownCreator.Modules
{
    public static partial class UnityGlobals
    {
        //同步
        public static SceneInstance LoadSceneSync(string key, LoadSceneMode mode, bool activateOnLoad = true)
        {
            return Addressables.LoadSceneAsync(key, mode, activateOnLoad).WaitForCompletion();
        }

        public static T LoadSync<T>(string key) where T : class
        {
            return Addressables.LoadAssetAsync<T>(key).WaitForCompletion();
        }

        public static void LoadSyncAutoRelease<T>(string key, Action<T> callBack) where T : class
        {
            var asset = Addressables.LoadAssetAsync<T>(key).WaitForCompletion();
            callBack.Invoke(asset);
            Release(asset);
        }

        public static bool HasAssetSync(string key)
        => Addressables.LoadResourceLocationsAsync(key).WaitForCompletion().Count > 0;

        public static T HasAndLoadSync<T>(string key) where T : class
        => HasAssetSync(key) ? LoadSync<T>(key) : null;

        public static void HasAndLoadSyncAutoRelease<T>(string key, Action<T> callBack) where T : class
        {
            if (HasAssetSync(key)) LoadSyncAutoRelease<T>(key, callBack);
        }


        //====================================================================================================================================


        //异步
        public static AsyncOperationHandle<SceneInstance> LoadSceneAsync(string key, LoadSceneMode mode, bool activateOnLoad = true)
        => Addressables.LoadSceneAsync(key, mode, activateOnLoad);

        public static async Task<T> LoadAsync<T>(string key) where T : class
        {
            var handle = Addressables.LoadAssetAsync<T>(key);
            await handle.Task;
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                return handle.Result;
            }
            return null;
        }


        //====================================================================================================================================

        //释放
        public static void Release<T>(T obj) where T : class
        {
            if (obj != null)
                Addressables.Release(obj);
        }

        public static AsyncOperationHandle<SceneInstance> ReleaseSceneAsync(SceneInstance scene, UnloadSceneOptions unloadOptions, bool autoReleaseHandle = true)
        {
            return Addressables.UnloadSceneAsync(scene, unloadOptions, autoReleaseHandle);
        }

        public static SceneInstance ReleaseSceneSync(SceneInstance scene, UnloadSceneOptions unloadOptions, bool autoReleaseHandle = true)
        {
            return Addressables.UnloadSceneAsync(scene, unloadOptions, autoReleaseHandle).WaitForCompletion();
        }

    }
}