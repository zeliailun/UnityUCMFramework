using System;
using System.Collections.Generic;

namespace UnknownCreator.Modules
{
    public sealed class TimerMgr : ITimerMgr
    {
        internal List<ITimer> timerList = new();

        internal Dictionary<long, ITimer> dict = new();

        public int GetTimerCount => timerList.Count;

        private bool isUpdating;
        private bool needCompact;
        private readonly List<ITimer> pendingReleaseList = new();



        //private TimerMgr() { }

        void IDearMgr.WorkWork()
        {
            timerList ??= new();
            dict ??= new();

            isUpdating = false;
            needCompact = false;

            pendingReleaseList.Clear();
        }

        void IDearMgr.DoNothing()
        {
            ClearAllTimer();
        }

        void IDearMgr.UpdateMGR()
        {
            isUpdating = true;

            try
            {
                for (int i = timerList.Count - 1; i >= 0; i--)
                {
                    ITimer timer = timerList[i];
                    if (timer == null) continue;

                    if (!HasTimer(timer)) continue;

                    if (timer is IInternalTimer t)
                        t.Update();
                }
            }
            finally
            {
                isUpdating = false;

                for (int i = 0; i < pendingReleaseList.Count; i++)
                {
                    Mgr.RPool.Release(pendingReleaseList[i]);
                }

                pendingReleaseList.Clear();

                if (needCompact)
                {
                    needCompact = false;
                    timerList.RemoveAll(t => t == null);
                }
            }
        }

        public bool HasTimer(long id)
        => dict.TryGetValue(id, out _);

        public bool HasTimer(ITimer timer)
        => timer != null && HasTimer(timer.id);

        public void RemoveTimer(long id)
        {
            var value = GetTimer(id);
            if (value != null)
            {
                dict.Remove(id);
                timerList.Remove(value);
                Mgr.RPool.Release(value);
            }
        }

        public void RemoveTimer(ITimer timer)
        {
            if (timer == null || dict == null) return;

            if (!dict.Remove(timer.id))
                return;

            if (isUpdating)
            {
                int index = timerList.IndexOf(timer);
                if (index >= 0)
                    timerList[index] = null;

                pendingReleaseList.Add(timer);
                needCompact = true;
                return;
            }

            timerList.Remove(timer);
            Mgr.RPool.Release(timer);
        }

        public void ClearAllTimer()
        {
            dict.Clear();

            for (int i = timerList.Count - 1; i >= 0; i--)
            {
                ITimer timer = timerList[i];
                if (timer == null) continue;

                timerList.RemoveAt(i);
                Mgr.RPool.Release(timer);
            }

            pendingReleaseList.Clear();
            needCompact = false;
            isUpdating = false;
        }

        public ITimer GetTimer(long id)
        => dict.TryGetValue(id, out var value) ? value : null;

        public ITimer CreateTimer(ITimer timer)
        {
            if (timer == null)
                return null;

            if (timer is not IInternalTimer internalTimer)
            {
                UCMDebug.LogError("创建 Timer 失败：timer 不是 IInternalTimer");
                return null;
            }

            // 已初始化的 Timer，只有一种情况是正常的：
            // 它已经在当前 TimerMgr 中，并且 dict 里的对象就是它自己
            if (timer.isInited)
            {
                if (dict.TryGetValue(timer.id, out ITimer existTimer) && ReferenceEquals(existTimer, timer))
                {
                    UCMDebug.LogWarning("尝试重复创建已经存在的 Timer");
                    return timer;
                }

                UCMDebug.LogError("创建 Timer 失败：Timer 已初始化，但不在当前 TimerMgr 中，状态异常");
                return null;
            }

            internalTimer.Init();

            if (dict.TryGetValue(timer.id,out _))
            {
                UCMDebug.LogError("尝试创建重复计时器");
                Mgr.RPool.Release(timer);
                return null;
            }

            dict.Add(timer.id, timer);
            timerList.Add(timer);

            return timer;
        }

        private void FlushPendingRelease()
        {
            for (int i = 0; i < pendingReleaseList.Count; i++)
            {
                ITimer timer = pendingReleaseList[i];
                if (timer == null) continue;

                try
                {
                    Mgr.RPool.Release(timer);
                }
                catch (Exception e)
                {
                    UCMDebug.LogError($"Timer Release 异常: {e}");
                }
            }

            pendingReleaseList.Clear();

            if (needCompact)
            {
                needCompact = false;
                timerList.RemoveAll(t => t == null);
            }
        }
    }

}
