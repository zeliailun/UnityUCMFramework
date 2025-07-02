using System;

namespace UnknownCreator.Modules
{
    public class TimerTween : TimerBase
    {
        public float startValue;
        public float endValue;
        public float duration;
        public EaseTypes type;
        public Delegate onValueChanged;
        public Delegate onCompleted;
        public int playCount;
        public bool isRemove;
        public bool isCompleted => currentPlayCount >= playCount;
        public int currentPlayCount { private set; get; }

        protected override void OnInitTimer()
        {
            currentPlayCount = 0;
        }


        protected override void OnUpdateTimer()
        {
            if (currentPlayCount >= playCount) return;

            if (time >= duration)
            {
                ValueChanged(endValue);
                time = 0f;
                if (++currentPlayCount >= playCount)
                {
                    isStart = false;
                    OnCompleted();
                    if (isRemove) Mgr.Timer.RemoveTimer(this);
                }
            }
            else
            {
                ValueChanged(EaseCalcGlobals.Calc(type, startValue, endValue, Math.Min(time / duration, 1F)));
            }
        }

        protected override void OnResetTimer()
        {
            currentPlayCount = 0;
        }

        protected override void OnClearTimer()
        {
            onValueChanged = null;
            onCompleted = null;
        }


        protected virtual void ValueChanged(float value)
        {
            (onValueChanged as Action<float>)?.Invoke(value);
        }


        protected virtual void OnCompleted()
        {
            (onCompleted as Action<TimerTween>)?.Invoke(this);
        }
    }

    public class TimerTween<T> : TimerTween
    {
        public T t;

        protected override void ValueChanged(float value)
        {
            (onValueChanged as Action<T, float, TimerTween<T>>)?.Invoke(t, value, this);
        }


        protected override void OnCompleted()
        {
            (onCompleted as Action<T, TimerTween<T>>)?.Invoke(t, this);
        }

        protected override void OnClearTimer()
        {

            base.OnClearTimer();
            t = default;
        }
    }


}

