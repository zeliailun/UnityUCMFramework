using System;

namespace UnknownCreator.Modules
{
    public abstract class TimerBase : ITimer, IReference
    {
        private long _id;
        private readonly object _idLock = new();
        public long id
        {
            get
            {
                if (_id == 0)
                {
                    lock (_idLock)
                    {
                        if (_id == 0)
                        {
                            _id = GlobalID.GetUniqueID();
                        }
                    }
                }
                return _id;
            }
        }

        public bool isStart { get; protected set; }

        public bool isApplyTimeScale { get; set; }

        public float time { get; protected set; }

        public Action onUpdate { get; set; }
        public Action onRelease { get; set; }

        // 初始化 Timer
        public void Init()
        {
            time = 0;
            OnInitTimer();
            isStart = true;
        }

        // ITimer 更新方法
        void ITimer.Update()
        {
            if (!isStart) return;

            time += CustomTime.DeltaTime(isApplyTimeScale);

            OnUpdateTimer();
            onUpdate?.Invoke();
        }

        // 重置 Timer
        public void Reset()
        {
            OnResetTimer();
            time = 0;
            isStart = true;
        }

        // 暂停或恢复 Timer
        public void Pause(bool pause)
        {
            isStart = !pause;
            OnPauseTimer(pause);
        }

        // 释放 Timer 资源
        void IReference.ObjRelease()
        {
            isStart = false;
            time = 0;
            onRelease?.Invoke();
            OnClearTimer();
            onUpdate = null;
            onRelease = null;
            lock (_idLock)
            {
                if (_id != 0)
                {
                    GlobalID.RecycleID(_id);
                    _id = 0;
                }
            }
        }

        protected virtual void OnInitTimer() { }

        protected virtual void OnUpdateTimer() { }

        protected virtual void OnClearTimer() { }

        protected virtual void OnResetTimer() { }

        protected virtual void OnPauseTimer(bool pause) { }
    }
}