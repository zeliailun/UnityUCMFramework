using System;

namespace UnknownCreator.Modules
{
    public class TimerDelayCycle : TimerBase
    {
        public float delay { set; get; }

        public Action<TimerDelayCycle> onTrigger { get; set; }

        protected override void OnUpdateTimer()
        {
            if (time >= delay)
            {
                time = 0;
                onTrigger?.Invoke(this);
            }                   
        }

        protected override void OnClearTimer()
        {
            onTrigger = null;
        }
    }
}