using System;
namespace UnknownCreator.Modules
{
    public static class TimerGlobals
    {
        public static bool IsVaild(this ITimer timer)
        {
            return timer != null && Mgr.Timer.HasTimer(timer);
        }

        public static bool IsAlive(this ITimer timer)
        {
            return Mgr.Timer.HasTimer(timer);
        }

        public static void DestroySelf(this ITimer timer)
        {
            Mgr.Timer.RemoveTimer(timer);
        }


        //指定次数的帧循环 （注意：当isRemove为true时，对象会被回收对象池，外部引用需要赋NULL）
        public static ITimer CycleFrame(this ITimerMgr mgr, int frameCount, bool isRemove, Action<TimerFrameCycle> onCompleted = null, bool isApplyTimeScale = true)
        {
            TimerFrameCycle timer = Mgr.RPool.Load<TimerFrameCycle>();
            timer.frameCount = frameCount;
            timer.isRemove = isRemove;
            timer.isApplyTimeScale = isApplyTimeScale;
            timer.onCompleted = onCompleted;

            if (isRemove)
                UCMDebug.LogWarning("注意自动销毁时，外部引用要赋为NULL,否则会引发对象池错误");

            return mgr.CreateTimer(timer);
        }

        //固定次数间隔循环 （注意：当isRemove为true时，对象会被回收对象池，外部引用需要赋NULL）
        public static ITimer CycleCount(this ITimerMgr mgr, int loopNum, float delay, bool isRemove, Action<TimerCountCycle> onTrigger, Action<TimerCountCycle> onCompleted = null, bool isApplyTimeScale = true)
        {
            TimerCountCycle timer = Mgr.RPool.Load<TimerCountCycle>();
            timer.playCount = loopNum;
            timer.delay = delay;
            timer.isRemove = isRemove;
            timer.isApplyTimeScale = isApplyTimeScale;
            timer.onTrigger = onTrigger;
            timer.onCompleted = onCompleted;

            if (isRemove)
                UCMDebug.LogWarning("注意自动销毁时，外部引用要赋为NULL,否则会引发对象池错误");

            return mgr.CreateTimer(timer);
        }

        //循环延迟计时
        public static ITimer CycleDelay(this ITimerMgr mgr, float delay, Action<TimerDelayCycle> onTrigger, bool isApplyTimeScale = true)
        {
            TimerDelayCycle timer = Mgr.RPool.Load<TimerDelayCycle>();
            timer.delay = delay;
            timer.isApplyTimeScale = isApplyTimeScale;
            timer.onTrigger = onTrigger;
            return mgr.CreateTimer(timer);
        }


        //2段循环计时器,会在到达第一次延迟时调用一次onTrigger，后面根据第二段延迟时间循环调用
        public static ITimer CycleTwoStage(this ITimerMgr mgr, float delay1, float delay2, Action<TimerTwoStageCycle> onTrigger, bool isApplyTimeScale = true)
        {
            TimerTwoStageCycle timer = Mgr.RPool.Load<TimerTwoStageCycle>();
            timer.firstDelay = delay1;
            timer.secondDelay = delay2;
            timer.isApplyTimeScale = isApplyTimeScale;
            timer.onTrigger = onTrigger;
            return mgr.CreateTimer(timer);
        }

        //补间计算（注意：当isRemove为true时，对象会被回收对象池，外部引用需要赋NULL）
        public static ITimer Custom(this ITimerMgr mgr,
            float start, float end, float duration, int playCount, bool isRemove,
            Action<float> onValueChanged, Action<TimerTween> onCompleted = null,
            EaseTypes type = EaseTypes.Linear, bool isApplyTimeScale = true)
        {
            TimerTween timer = Mgr.RPool.Load<TimerTween>();
            timer.onCompleted = onCompleted;
            return CreateCustomTimer(mgr, timer, start, end, duration, playCount, isRemove, onValueChanged, onCompleted, type, isApplyTimeScale);
        }

        public static ITimer Custom<T>(this ITimerMgr mgr,
            T t, float start, float end, float duration, int playCount, bool isRemove,
            Action<T, float, TimerTween<T>> onValueChanged, Action<T, TimerTween<T>> onCompleted = null,
            EaseTypes type = EaseTypes.Linear, bool isApplyTimeScale = true)
        {
            TimerTween<T> timer = Mgr.RPool.Load<TimerTween<T>>();
            timer.t = t;
            timer.onCompleted = onCompleted;
            return CreateCustomTimer(mgr, timer, start, end, duration, playCount, isRemove, onValueChanged, onCompleted, type, isApplyTimeScale);
        }

        private static ITimer CreateCustomTimer(
            ITimerMgr mgr, TimerTween timer, float start, float end, float duration, int playCount, bool isRemove,
            Delegate onValueChanged, Delegate onCompleted,
            EaseTypes type, bool isApplyTimeScale)
        {
            timer.startValue = start;
            timer.endValue = end;
            timer.duration = duration;
            timer.type = type;
            timer.isRemove = isRemove;
            timer.playCount = Math.Max(playCount, 1);
            timer.isApplyTimeScale = isApplyTimeScale;
            timer.onValueChanged = onValueChanged;

            if (isRemove)
                UCMDebug.LogWarning("注意自动销毁时，外部引用要赋为NULL,否则会引发对象池错误");

            return mgr.CreateTimer(timer);
        }

    }
}