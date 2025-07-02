using System.Collections.Generic;
namespace UnknownCreator.Modules
{
    public sealed class TimerMgr : ITimerMgr
    {
        internal List<ITimer> timerList = new();

        internal Dictionary<long, ITimer> dict = new();

        public int GetTimerCount => timerList.Count;

        //private TimerMgr() { }

        void IDearMgr.WorkWork()
        {
            timerList ??= new();
            dict ??= new();
        }

        void IDearMgr.DoNothing()
        {
            ClearAllTimer();
            timerList = null;
            dict = null;
        }

        void IDearMgr.UpdateMGR()
        {
            for (int i = 0; i < timerList.Count; i++)
            {
                timerList[i]?.Update();
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
            if (timer != null && dict.Remove(timer.id))
            {
                timerList.Remove(timer);
                Mgr.RPool.Release(timer);
            }
        }

        public void ClearAllTimer()
        {
            dict.Clear();
            ITimer timer;
            for (int i = timerList.Count - 1; i >= 0; i--)
            {
                timer = timerList[i];
                if (timer is null) continue;
                timerList.RemoveAt(i);
                Mgr.RPool.Release(timer);
            }
        }

        public ITimer GetTimer(long id)
        => dict.TryGetValue(id, out var value) ? value : null;

        public ITimer CreateTimer(ITimer timer)
        {
            if (timer != null)
            {
                if (!dict.TryGetValue(timer.id, out _))
                {
                    timer.Init();
                    dict.Add(timer.id, timer);
                    timerList.Add(timer);
                    timer.Update();
                    return timer;
                }
                else
                {
                    UCMDebug.LogError("尝试创建重复计时器");
                }
            }
            return null;
        }
    }

}
