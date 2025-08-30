using System;
using System.Collections.Generic;
namespace UnknownCreator.Modules
{
    public abstract class ObjPoolBase<T> : IPool where T : class, new()
    {
        private HashSet<T> findPool = new();

        private Stack<T> pool = new();

        public virtual string poolName => string.Empty;

        private int count;
        public int storageCount
        {
            get => count;
            private set => count = Math.Clamp(value, 0, maxNum);
        }

        public int maxNum { get; set; }

        private int remNum;
        public int remainingNum
        {
            get => remNum;
            set => remNum = Math.Max(1, value);
        }

        private float interval;
        public float clearInterval
        {
            get => interval;
            set => interval = (float)Math.Max(.1, value);
        }

        public float maxDestroyTime { get; set; } = 60;

        public bool isEnableClear { get; set; }

        private float time;

        public ObjPoolBase()
        {

        }

        public ObjPoolBase(ObjPoolInfo info)
        {
            this.maxNum = info.maxNum;
            this.remainingNum = info.remainingNum;
            this.clearInterval = info.clearInterval;
            this.isEnableClear = info.isAutoClear;
        }

        public (bool isNew, T t) Love()
        {
            T obj;
            if (storageCount > 0)
            {
                storageCount--;
                obj = pool.Pop();
                findPool.Remove(obj);
                OnPop(obj);
            }
            else
            {
                obj = OnCreate();
            }
            return (storageCount <= 0, obj);
        }

        public void Hate(T obj)
        {
            if (HasObject(obj))
            {
                UCMDebug.LogWarning("尝试存放空对象或者重复的对象>>" + obj);
                return;
            }

            if (storageCount < maxNum)
            {

                OnRelease(obj);
                pool.Push(obj);
                findPool.Add(obj);
                storageCount++;
                return;
            }
            OnRelease(obj);
            OnClear(obj);
        }

        public void Preload(T obj)
        {
            if (HasObject(obj))
            {
                UCMDebug.LogWarning("尝试存放空对象或者重复的对象>>" + obj);
                return;
            }

            if (storageCount > maxNum)
            {
                UCMDebug.LogWarning("存放的对象数量超出限制");
                return;
            }

            OnPreStored(obj);
            pool.Push(obj);
            findPool.Add(obj);
            storageCount++;
        }

        public void ClearPool()
        {
            isEnableClear = false;
            foreach (T item in pool) OnClear(item);
            pool.Clear();
            findPool.Clear();
            storageCount = 0;
            isEnableClear = true;
        }

        void IPool.DestroyPool()
        {
            ClearPool();
            OnPoolDestroy();
        }

        void IPool.UpdatePool()
        {
            if (!isEnableClear ||
                pool.Count < 1 ||
                storageCount < 1) return;

            time += CustomTime.DeltaTime(false);

            if (time < clearInterval) return;

            time = 0;

            if (storageCount > remainingNum)
            {


                var obj = pool.Pop();
                findPool.Remove(obj);
                OnClear(obj);
                storageCount--;
            }
        }

        public bool HasObject(object obj)
        => obj != null && storageCount > 0 && findPool.Contains((T)obj);

        protected virtual T OnCreate() => default;

        protected virtual void OnPop(T obj) { }

        protected virtual void OnPreStored(T obj) { }

        protected virtual void OnRelease(T obj) { }

        protected virtual void OnClear(T obj) { }

        protected virtual void OnPoolDestroy() { }

    }
}