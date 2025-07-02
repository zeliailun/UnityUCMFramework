using UnityEngine;
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
            currentFrameCount = frameCount;
        }

        protected override void OnUpdateTimer()
        {
            frameCount--;
            if (frameCount <= 0)
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
            currentFrameCount = frameCount;
        }
    }
}
