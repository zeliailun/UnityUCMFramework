using System;

namespace UnknownCreator.Modules
{
    public class TimerFrameCycle : TimerBase
    {
        public int frameCount { set; get; }

        public bool isRemove { set; get; }

        public Action<TimerFrameCycle> onCompleted { get; set; }

        public int currentFrameCount { private set; get; }

        protected override void OnInitTimer()
        {
            currentFrameCount = Math.Max(frameCount, 1);
        }

        protected override void OnUpdateTimer()
        {
            currentFrameCount--;

            if (currentFrameCount <= 0)
            {
                isStart = false;
                onCompleted?.Invoke(this);

                if (isRemove)
                    Mgr.Timer.RemoveTimer(this);
            }
        }

        protected override void OnClearTimer()
        {
            onCompleted = null;
        }

        protected override void OnResetTimer()
        {
            currentFrameCount = Math.Max(frameCount, 1);
        }
    }
}
