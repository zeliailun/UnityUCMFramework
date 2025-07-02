using System.Collections.Generic;
using UnityEngine;
namespace UnknownCreator.Modules
{
    public sealed class GameObjPoolMgr : IGameObjPoolMgr
    {
        internal GameObject root;

        internal Dictionary<string, IPool> gameObjPool = new();

        internal Dictionary<string, Stack<GameObject>> newObjPool = new();

        internal List<IPool> poolList = new();

        [JsonIgnore]
        public int poolCount => gameObjPool.Count;

        private string rootObjName;
        [JsonIgnore]
        public string rootName
        {
            get => rootObjName;
            set
            {
                root.name = rootObjName = value;
            }
        }

        //private GameObjPoolMgr() { }

        void IDearMgr.WorkWork()
        {
            gameObjPool ??= new();
            poolList ??= new();
            newObjPool ??= new();
            root = new GameObject(nameof(GameObjPoolMgr));
            Object.DontDestroyOnLoad(root);
        }

        void IDearMgr.UpdateMGR()
        {
            for (int i = 0; i < poolList.Count; i++)
            {
                poolList[i]?.UpdatePool();
            }
        }

        void IDearMgr.DoNothing()
        {
            DestroyAll();
            Object.Destroy(root);
            root = null;
            gameObjPool = null;
            poolList = null;
        }

        public void SetRoot(GameObject obj,bool worldPositionStays)
        {
            obj.transform.SetParent(root.transform, worldPositionStays);
        }

        public void SetRoot(Transform obj, bool worldPositionStays)
        {
            obj.SetParent(root.transform, worldPositionStays);
        }

        public ObjPoolBase<GameObject> CreatePool(string prefabName, bool isSetRoot, bool isActive, ObjPoolInfo info)
        {
            if (!gameObjPool.TryGetValue(prefabName, out var pool))
            {
                var newPool = new GameObjPool(info, prefabName, isSetRoot, isActive);
                gameObjPool.Add(prefabName, newPool);
                poolList.Add(newPool);
                return newPool;
            }
            return (ObjPoolBase<GameObject>)pool;
        }


        public GameObject Load(string prefabName, bool isSetRoot, bool isActive)
        => CreatePool(prefabName, isSetRoot, isActive, ObjPoolInfo.defaultInfo).Love().t;

        public void Preload(string name, GameObject go, int count, ObjPoolInfo info)
        {
            var pool = CreatePool(name, true, false, info);
            for (int i = 0; i < count; i++)
            {
                pool.Preload(Object.Instantiate(go));
            }
        }

        public void Release(string prefabName, GameObject go)
        {
            if (go == null)
            {
                UCMDebug.LogWarning("无法释放null对象");
                return;
            }

            if (gameObjPool.TryGetValue(prefabName, out var pool))
                ((ObjPoolBase<GameObject>)pool).Hate(go);
            else
                CreatePool(prefabName, true, false, ObjPoolInfo.defaultInfo).Hate(go);
        }

        public (GameObject, bool) GetNewGameObject(string name)
        {
            GameObject go;
            bool isNew = false;
            if (newObjPool.TryGetValue(name, out var result) &&
                result.Count > 0)
            {
                go = result.Pop();
            }
            else
            {
                go = new GameObject(name);
                isNew = true;
            }
            SetRoot(go,true);
            go.SetActive(true);
            return (go, isNew);
        }

        public void ReleaseNewGameObject(string name, GameObject go)
        {
            if (go == null)
            {
                UCMDebug.LogWarning("无法释放null对象");
                return;
            }
            go.SetActive(false);
            if (newObjPool.TryGetValue(name, out var result))
            {
                result.Push(go);
            }
            else
            {
                var stack = new Stack<GameObject>();
                stack.Push(go);
                newObjPool.Add(name, stack);
            }
        }

        public bool HasObject(GameObject obj, string name)
        => gameObjPool.TryGetValue(name, out var result) && result.HasObject(obj);

        public void DestroyPool(string prefabName)
        {
            if (gameObjPool.TryGetValue(prefabName, out var pool))
            {
                pool.DestroyPool();
                gameObjPool.Remove(prefabName);
                poolList.Remove(pool);
            }
        }

        public void ClearAll()
        {
            for (int i = poolList.Count - 1; i >= 0; i--)
            {
                poolList[i]?.ClearPool();
            }
        }

        public void DestroyAll()
        {
            gameObjPool.Clear();
            for (int i = poolList.Count - 1; i >= 0; i--)
            {
                poolList[i].DestroyPool();
                poolList.RemoveAt(i);
            }

            foreach (var pool in newObjPool.Values)
            {
                for (int i = 0; i < pool.Count; i++)
                    Object.Destroy(pool.Pop());
            }
            newObjPool.Clear();
        }
    }
}
