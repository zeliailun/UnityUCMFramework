using System;

namespace UnknownCreator.Modules
{
    public interface IReferencePoolMgr : IDearMgr
    {
        int poolCount { get; }
        public object Load(Type type);
        public T Load<T>() where T : class;//, new();
        void Release(object obj);
        bool HasObject(object obj);
        bool HasObject(Type type, object obj);
        void DestroyPool(object obj);
        void DestroyPool(Type type);
        void ClearAll();
        void DestroyAll();
        ObjPoolBase<object> CreatePool(Type type, ObjPoolInfo  info);
        void Preload<T>(int count, ObjPoolInfo info) where T : class, new();
        void Preload(string className, int count, ObjPoolInfo info);
    }
}