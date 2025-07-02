using System;
using System.Collections.Generic;

namespace UnknownCreator.Modules
{
    public sealed class ReferencePoolMgr : IReferencePoolMgr
    {
        internal Dictionary<Type, IPool> referencePool = new();

        internal List<IPool> poolList = new();

        public int poolCount => referencePool.Count;

        //private ReferencePoolMgr() { }

        void IDearMgr.WorkWork()
        {
            referencePool ??= new();
            poolList ??= new();
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
            referencePool = null;
            poolList = null;
        }

        public ObjPoolBase<object> CreatePool(Type type, ObjPoolInfo info)
        {
            if (!referencePool.TryGetValue(type, out var pool))
            {
                ReferencePool newPool = new(info, type);
                referencePool.Add(type, newPool);
                poolList.Add(newPool);
                return newPool;
            }
            return (ObjPoolBase<object>)pool;
        }

        public object Load(Type type)
        => CreatePool(type, ObjPoolInfo.defaultInfo).Love().t;

        public T Load<T>() where T : class//, new()
        => (T)Load(typeof(T));

        public void Release(object obj)
        {
            if (obj is null)
            {
                UCMDebug.LogWarning("无法释放空对象！");
                return;
            }
            var typeCache = obj.GetType();

            if (referencePool.TryGetValue(typeCache, out var pool))
                ((ObjPoolBase<object>)pool).Hate(obj);
            else
                UCMDebug.LogWarning("该对象不是由对象池创建，无法释放！");
            //GetPool(typeCache, ObjPoolInfo.defaultInfo).Hate(obj);
        }

        public void Preload<T>(int count, ObjPoolInfo info) where T : class, new()
        {
            var pool = CreatePool(typeof(T), info);
            for (int i = 0; i < count; i++)
                pool.Preload(new T());
        }

        public void Preload(string className, int count, ObjPoolInfo info)
        {
            var type = Type.GetType(className);
            var pool = CreatePool(type, info);
            for (int i = 0; i < count; i++)
                pool.Preload(Activator.CreateInstance(type));
        }

        public bool HasObject(object obj)
        => obj is not null && HasObject(obj.GetType(), obj);

        public bool HasObject(Type type, object obj)
        => type != null && referencePool.TryGetValue(type, out var pool) && pool.HasObject(obj);

        public void DestroyPool(object obj)
        => DestroyPool(obj.GetType());

        public void DestroyPool(Type type)
        {
            if (referencePool.TryGetValue(type, out var pool))
            {
                pool.DestroyPool();
                referencePool.Remove(type);
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
            referencePool.Clear();
            for (int i = poolList.Count - 1; i >= 0; i--)
            {
                poolList[i].DestroyPool();
                poolList.RemoveAt(i);
            }
        }
    }
}
