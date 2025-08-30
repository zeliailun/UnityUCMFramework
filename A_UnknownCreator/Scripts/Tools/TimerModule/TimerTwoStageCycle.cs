using System;

namespace UnknownCreator.Modules
{
    public class TimerTwoStageCycle : TimerBase
    {
        public float delayCache { private set; get; }

        public float firstDelay { set; get; }

        public float secondDelay { set; get; }

        public Action<TimerTwoStageCycle> onTrigger { get; set; }

        protected override void OnUpdateTimer()
        {
            if (time >= delayCache)
            {
                time = 0;
                delayCache = secondDelay;
                onTrigger?.Invoke(this);
            }
        }

        protected override void OnInitTimer()
        {
            delayCache = firstDelay;
        }

        protected override void OnResetTimer()
        {
            delayCache = firstDelay;
        }

        protected override void OnClearTimer()
        {
            onTrigger = null;
        }
    }
}