using System;

namespace UnknownCreator.Modules
{
    public static class TimerGlobals
    {
        public const long InvalidTimerID = -1;

        public static bool IsValid(this ITimer timer)
        {
            return timer != null && Mgr.Timer.HasTimer(timer);
        }

        public static bool IsAlive(this ITimer timer)
        {
            return timer != null && Mgr.Timer.HasTimer(timer);
        }

        public static bool IsValid(this long timerID)
        {
            return timerID != InvalidTimerID && Mgr.Timer.HasTimer(timerID);
        }

        public static bool IsAlive(this long timerID)
        {
            return timerID != InvalidTimerID && Mgr.Timer.HasTimer(timerID);
        }

        public static long GetTimerID(this ITimer timer)
        {
            return timer?.id ?? InvalidTimerID;
        }

        public static void DestroySelf(this ITimer timer)
        {
            Mgr.Timer.RemoveTimer(timer);
        }

        public static void DestroySelf(this ref long timerID)
        {
            Mgr.Timer.RemoveTimer(timerID);
            timerID = InvalidTimerID;
        }

        private static long ToTimerID(ITimer timer)
        {
            return timer?.id ?? InvalidTimerID;
        }

        private static void LogAutoRemoveWarning(bool isRemove)
        {
            if (isRemove)
                UCMDebug.LogWarning("注意自动销毁时，外部引用要赋为NULL,否则会引发对象池错误");
        }

        public static TimerHandle<T> ToHandle<T>(this ITimer timer) where T : class, ITimer
        {
            return new TimerHandle<T>(timer as T);
        }

        // =========================================================
        // 帧计时器 - 返回 ITimer
        // =========================================================

        public static ITimer CycleFrame(
            this ITimerMgr mgr,
            int frameCount,
            bool isRemove,
            Action<TimerFrameCycle> onCompleted = null,
            bool isApplyTimeScale = true)
        {
            TimerFrameCycle timer = Mgr.RPool.Load<TimerFrameCycle>();

            timer.frameCount = Math.Max(frameCount, 1);
            timer.isRemove = isRemove;
            timer.isApplyTimeScale = isApplyTimeScale;
            timer.onCompleted = onCompleted;

            LogAutoRemoveWarning(isRemove);

            return mgr.CreateTimer(timer);
        }

        // 帧计时器 - 返回 long id
        public static long CycleFrameID(
            this ITimerMgr mgr,
            int frameCount,
            bool isRemove,
            Action<TimerFrameCycle> onCompleted = null,
            bool isApplyTimeScale = true)
        {
            ITimer timer = mgr.CycleFrame(
                frameCount,
                isRemove,
                onCompleted,
                isApplyTimeScale);

            return ToTimerID(timer);
        }

        // =========================================================
        // 固定次数间隔循环 - 返回 ITimer
        // =========================================================

        public static ITimer CycleCount(
            this ITimerMgr mgr,
            int loopNum,
            float delay,
            bool isRemove,
            Action<TimerCountCycle> onTrigger,
            Action<TimerCountCycle> onCompleted = null,
            bool isApplyTimeScale = true)
        {
            TimerCountCycle timer = Mgr.RPool.Load<TimerCountCycle>();

            timer.playCount = Math.Max(loopNum, 1);
            timer.delay = delay;
            timer.isRemove = isRemove;
            timer.isApplyTimeScale = isApplyTimeScale;
            timer.onTrigger = onTrigger;
            timer.onCompleted = onCompleted;

            LogAutoRemoveWarning(isRemove);

            return mgr.CreateTimer(timer);
        }

        // 固定次数间隔循环 - 返回 long id
        public static long CycleCountID(
            this ITimerMgr mgr,
            int loopNum,
            float delay,
            bool isRemove,
            Action<TimerCountCycle> onTrigger,
            Action<TimerCountCycle> onCompleted = null,
            bool isApplyTimeScale = true)
        {
            ITimer timer = mgr.CycleCount(
                loopNum,
                delay,
                isRemove,
                onTrigger,
                onCompleted,
                isApplyTimeScale);

            return ToTimerID(timer);
        }

        // =========================================================
        // 无限延迟循环 - 返回 ITimer
        // =========================================================

        public static ITimer CycleDelay(
            this ITimerMgr mgr,
            float delay,
            Action<TimerDelayCycle> onTrigger,
            bool isApplyTimeScale = true)
        {
            TimerDelayCycle timer = Mgr.RPool.Load<TimerDelayCycle>();

            timer.delay = delay;
            timer.isApplyTimeScale = isApplyTimeScale;
            timer.onTrigger = onTrigger;

            return mgr.CreateTimer(timer);
        }

        // 无限延迟循环 - 返回 long id
        public static long CycleDelayID(
            this ITimerMgr mgr,
            float delay,
            Action<TimerDelayCycle> onTrigger,
            bool isApplyTimeScale = true)
        {
            ITimer timer = mgr.CycleDelay(
                delay,
                onTrigger,
                isApplyTimeScale);

            return ToTimerID(timer);
        }

        // =========================================================
        // 二段循环计时器 - 返回 ITimer
        // =========================================================

        public static ITimer CycleTwoStage(
            this ITimerMgr mgr,
            float delay1,
            float delay2,
            Action<TimerTwoStageCycle> onTrigger,
            bool isApplyTimeScale = true)
        {
            TimerTwoStageCycle timer = Mgr.RPool.Load<TimerTwoStageCycle>();

            timer.firstDelay = delay1;
            timer.secondDelay = delay2;
            timer.isApplyTimeScale = isApplyTimeScale;
            timer.onTrigger = onTrigger;

            return mgr.CreateTimer(timer);
        }

        // 二段循环计时器 - 返回 long id
        public static long CycleTwoStageID(
            this ITimerMgr mgr,
            float delay1,
            float delay2,
            Action<TimerTwoStageCycle> onTrigger,
            bool isApplyTimeScale = true)
        {
            ITimer timer = mgr.CycleTwoStage(
                delay1,
                delay2,
                onTrigger,
                isApplyTimeScale);

            return ToTimerID(timer);
        }

        // =========================================================
        // 补间计时器 - 返回 ITimer
        // =========================================================

        public static ITimer Custom(
            this ITimerMgr mgr,
            float start,
            float end,
            float duration,
            int playCount,
            bool isRemove,
            Action<float> onValueChanged,
            Action<TimerTween> onCompleted = null,
            EaseTypes type = EaseTypes.Linear,
            bool isApplyTimeScale = true)
        {
            TimerTween timer = Mgr.RPool.Load<TimerTween>();

            return CreateCustomTimer(
                mgr,
                timer,
                start,
                end,
                duration,
                playCount,
                isRemove,
                onValueChanged,
                onCompleted,
                type,
                isApplyTimeScale);
        }

        // 补间计时器 - 返回 long id
        public static long CustomID(
            this ITimerMgr mgr,
            float start,
            float end,
            float duration,
            int playCount,
            bool isRemove,
            Action<float> onValueChanged,
            Action<TimerTween> onCompleted = null,
            EaseTypes type = EaseTypes.Linear,
            bool isApplyTimeScale = true)
        {
            ITimer timer = mgr.Custom(
                start,
                end,
                duration,
                playCount,
                isRemove,
                onValueChanged,
                onCompleted,
                type,
                isApplyTimeScale);

            return ToTimerID(timer);
        }

        // =========================================================
        // 带泛型数据的补间计时器 - 返回 ITimer
        // =========================================================

        public static ITimer Custom<T>(
            this ITimerMgr mgr,
            T t,
            float start,
            float end,
            float duration,
            int playCount,
            bool isRemove,
            Action<T, float, TimerTween<T>> onValueChanged,
            Action<T, TimerTween<T>> onCompleted = null,
            EaseTypes type = EaseTypes.Linear,
            bool isApplyTimeScale = true)
        {
            TimerTween<T> timer = Mgr.RPool.Load<TimerTween<T>>();
            timer.t = t;

            return CreateCustomTimer(
                mgr,
                timer,
                start,
                end,
                duration,
                playCount,
                isRemove,
                onValueChanged,
                onCompleted,
                type,
                isApplyTimeScale);
        }

        // 带泛型数据的补间计时器 - 返回 long id
        public static long CustomID<T>(
            this ITimerMgr mgr,
            T t,
            float start,
            float end,
            float duration,
            int playCount,
            bool isRemove,
            Action<T, float, TimerTween<T>> onValueChanged,
            Action<T, TimerTween<T>> onCompleted = null,
            EaseTypes type = EaseTypes.Linear,
            bool isApplyTimeScale = true)
        {
            ITimer timer = mgr.Custom(
                t,
                start,
                end,
                duration,
                playCount,
                isRemove,
                onValueChanged,
                onCompleted,
                type,
                isApplyTimeScale);

            return ToTimerID(timer);
        }

        private static ITimer CreateCustomTimer(
            ITimerMgr mgr,
            TimerTween timer,
            float start,
            float end,
            float duration,
            int playCount,
            bool isRemove,
            Delegate onValueChanged,
            Delegate onCompleted,
            EaseTypes type,
            bool isApplyTimeScale)
        {
            timer.startValue = start;
            timer.endValue = end;
            timer.duration = duration;
            timer.type = type;
            timer.isRemove = isRemove;
            timer.playCount = Math.Max(playCount, 1);
            timer.isApplyTimeScale = isApplyTimeScale;
            timer.onValueChanged = onValueChanged;
            timer.onCompleted = onCompleted;

            LogAutoRemoveWarning(isRemove);

            return mgr.CreateTimer(timer);
        }
    }
}