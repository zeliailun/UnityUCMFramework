using System;

namespace UnknownCreator.Modules
{
    public abstract class TimerBase : IInternalTimer, IReference
    {
        public long id { get; private set; }

        public bool isStart { get; protected set; }

        public bool isApplyTimeScale { get; set; }

        public float time { get; protected set; }

        public Action onUpdate { get; set; }
        public Action onRelease { get; set; }

        public bool isInited { get; private set; } = false;

        // 初始化 Timer
        void IInternalTimer.Init()
        {
            if (isInited) return;
            isInited = true;

            time = 0;
            id = GlobalID.GetUniqueID();
            OnInitTimer();
            isStart = true;
        }

        // ITimer 更新方法
        void IInternalTimer.Update()
        {
            if (!isStart) return;

            time += CustomTime.DeltaTime(isApplyTimeScale);

            OnUpdateTimer();

            if (!isStart || !Mgr.Timer.HasTimer(this)) return;

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
            onRelease?.Invoke();
            OnClearTimer();
            onUpdate = null;
            onRelease = null;
            time = 0;
            id = -1;
            isInited = false;
        }

        protected virtual void OnInitTimer() { }

        protected virtual void OnUpdateTimer() { }

        protected virtual void OnClearTimer() { }

        protected virtual void OnResetTimer() { }

        protected virtual void OnPauseTimer(bool pause) { }
    }
}


