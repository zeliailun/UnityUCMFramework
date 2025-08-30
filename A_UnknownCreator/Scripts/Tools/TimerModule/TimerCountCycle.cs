using System;

namespace UnknownCreator.Modules
{
    public class TimerCountCycle : TimerBase
    {
        public int playCount { set; get; }

        public float delay { set; get; }

        public bool isRemove { set; get; }

        public Action<TimerCountCycle> onTrigger { get; set; }

        public Action<TimerCountCycle> onCompleted { get; set; }


        public bool isCompleted => currentPlayCount >= playCount;

        public int currentPlayCount { private set; get; }

        protected override void OnInitTimer()
        {
            currentPlayCount = 0;
        }

        protected override void OnUpdateTimer()
        {
            if (time >= delay)
            {
                time = 0;
                ++currentPlayCount;
                onTrigger?.Invoke(this);
                if (currentPlayCount >= playCount)
                {
                    isStart = false;
                    onCompleted?.Invoke(this);

                    if (isRemove)
                        Mgr.Timer.RemoveTimer(this);
                }
            }
        }

        protected override void OnClearTimer()
        {
            onTrigger = null;
            onCompleted = null;
        }

        protected override void OnResetTimer()
        {
            currentPlayCount = 0;
        }
    }
}