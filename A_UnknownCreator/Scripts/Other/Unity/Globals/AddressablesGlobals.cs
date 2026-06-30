using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace UnknownCreator.Modules
{
    public static partial class UnityGlobals
    {
        /// <summary>
        /// 记录通过 UnityGlobals 加载出来的 Addressables 资源句柄。
        /// Key = 加载出来的资源对象
        /// Value = 该对象对应的加载句柄栈
        /// 
        /// 同一个资源可能被 LoadSync 多次，所以用 Stack 保存多个 handle。
        /// Release 一次就释放一个 handle。
        /// </summary>
        private static readonly Dictionary<object, Stack<AsyncOperationHandle>> addressableHandleCache = new();

        //====================================================================================================================================
        // 同步
        //====================================================================================================================================

        public static SceneInstance LoadSceneSync(string key, LoadSceneMode mode, bool activateOnLoad = true)
        {
            return Addressables.LoadSceneAsync(key, mode, activateOnLoad).WaitForCompletion();
        }

        public static T LoadSync<T>(string key) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            var handle = Addressables.LoadAssetAsync<T>(key);
            T asset = handle.WaitForCompletion();

            if (handle.Status != AsyncOperationStatus.Succeeded || asset == null)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);

                return null;
            }

            RegisterHandle(asset, handle);
            return asset;
        }

        public static void LoadSyncAutoRelease<T>(string key, Action<T> callBack) where T : class
        {
            T asset = LoadSync<T>(key);

            if (asset == null)
                return;

            callBack?.Invoke(asset);
            Release(asset);
        }

        public static bool HasAssetSync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            var handle = Addressables.LoadResourceLocationsAsync(key);
            var locations = handle.WaitForCompletion();

            bool hasAsset = locations != null && locations.Count > 0;

            if (handle.IsValid())
                Addressables.Release(handle);

            return hasAsset;
        }

        public static T HasAndLoadSync<T>(string key) where T : class
        {
            return HasAssetSync(key) ? LoadSync<T>(key) : null;
        }

        public static void HasAndLoadSyncAutoRelease<T>(string key, Action<T> callBack) where T : class
        {
            if (HasAssetSync(key))
                LoadSyncAutoRelease(key, callBack);
        }

        //====================================================================================================================================
        // 异步
        //====================================================================================================================================

        public static AsyncOperationHandle<SceneInstance> LoadSceneAsync(string key, LoadSceneMode mode, bool activateOnLoad = true)
        {
            return Addressables.LoadSceneAsync(key, mode, activateOnLoad);
        }

        public static async Task<T> LoadAsync<T>(string key) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            var handle = Addressables.LoadAssetAsync<T>(key);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);

                return null;
            }

            RegisterHandle(handle.Result, handle);
            return handle.Result;
        }

        //====================================================================================================================================
        // 释放
        //====================================================================================================================================

        /// <summary>
        /// 安全释放资源。
        /// 只有通过 UnityGlobals.LoadSync / LoadAsync 记录过的对象，才会真正释放。
        /// 如果对象不是 Addressables 加载出来的，或者已经释放过，会直接跳过，不会报 Addressables 不认识该对象。
        /// </summary>
        public static void Release<T>(T obj) where T : class
        {
            if (obj == null)
                return;

            if (!addressableHandleCache.TryGetValue(obj, out var stack))
                return;

            if (stack == null || stack.Count <= 0)
            {
                addressableHandleCache.Remove(obj);
                return;
            }

            var handle = stack.Pop();

            if (stack.Count <= 0)
                addressableHandleCache.Remove(obj);

            if (handle.IsValid())
                Addressables.Release(handle);
        }

        public static AsyncOperationHandle<SceneInstance> ReleaseSceneAsync(SceneInstance scene, UnloadSceneOptions unloadOptions, bool autoReleaseHandle = true)
        {
            return Addressables.UnloadSceneAsync(scene, unloadOptions, autoReleaseHandle);
        }

        public static SceneInstance ReleaseSceneSync(SceneInstance scene, UnloadSceneOptions unloadOptions, bool autoReleaseHandle = true)
        {
            return Addressables.UnloadSceneAsync(scene, unloadOptions, autoReleaseHandle).WaitForCompletion();
        }

        /// <summary>
        /// 判断这个对象是不是通过 UnityGlobals 记录过的 Addressables 资源。
        /// </summary>
        public static bool IsAddressableTracked(object obj)
        {
            return obj != null && addressableHandleCache.ContainsKey(obj);
        }

        /// <summary>
        /// 释放所有还没释放的 Addressables 资源。
        /// 一般在退出 Play / ResetStaticState 最后兜底调用。
        /// </summary>
        public static void ReleaseAllTrackedAddressables()
        {
            foreach (var kvp in addressableHandleCache)
            {
                var stack = kvp.Value;

                if (stack == null)
                    continue;

                while (stack.Count > 0)
                {
                    var handle = stack.Pop();

                    if (handle.IsValid())
                        Addressables.Release(handle);
                }
            }

            addressableHandleCache.Clear();
        }

        /// <summary>
        /// 只清记录，不释放资源。
        /// 一般不推荐直接用，除非你确定 Addressables 已经被 Unity 自己清掉了。
        /// </summary>
        public static void ClearAddressableHandleCache()
        {
            addressableHandleCache.Clear();
        }

        private static void RegisterHandle<T>(T asset, AsyncOperationHandle<T> handle) where T : class
        {
            if (asset == null)
                return;

            if (!addressableHandleCache.TryGetValue(asset, out var stack))
            {
                stack = new Stack<AsyncOperationHandle>();
                addressableHandleCache.Add(asset, stack);
            }

            stack.Push(handle);
        }
    }
}