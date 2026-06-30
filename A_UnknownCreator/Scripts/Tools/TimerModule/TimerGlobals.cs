using System;

namespace UnknownCreator.Modules
{
    public static class TimerGlobals
    {
        public const long InvalidTimerID = -1;

        // =========================================================
        // ID 扩展
        // =========================================================

        public static bool IsValid(this long timerID)
        {
            return timerID != InvalidTimerID && Mgr.Timer.HasTimer(timerID);
        }

        public static bool IsAlive(this long timerID)
        {
            return timerID != InvalidTimerID && Mgr.Timer.HasTimer(timerID);
        }

        public static void DestroySelf(this ref long timerID)
        {
            if (timerID != InvalidTimerID)
                Mgr.Timer.RemoveTimer(timerID);

            timerID = InvalidTimerID;
        }

        // =========================================================
        // Handle 扩展
        // =========================================================

        public static TimerHandle<T> ToHandle<T>(this ITimer timer) where T : class, ITimer
        {
            return new TimerHandle<T>(timer as T);
        }


        // =========================================================
        // 内部工具
        // =========================================================

        private static long ToTimerID<T>(TimerHandle<T> handle) where T : class, ITimer
        {
            return handle.idValue;
        }


        // =========================================================
        // 帧计时器 - 返回 Handle
        // =========================================================

        public static TimerHandle<TimerFrameCycle> CycleFrameHandle(
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

            return mgr.CreateTimer(timer).ToHandle<TimerFrameCycle>();
        }

        // 帧计时器 - 返回 ID
        public static long CycleFrameID(
            this ITimerMgr mgr,
            int frameCount,
            bool isRemove,
            Action<TimerFrameCycle> onCompleted = null,
            bool isApplyTimeScale = true)
        {
            return ToTimerID(mgr.CycleFrameHandle(
                frameCount,
                isRemove,
                onCompleted,
                isApplyTimeScale));
        }

        // =========================================================
        // 固定次数间隔循环 - 返回 Handle
        // =========================================================

        public static TimerHandle<TimerCountCycle> CycleCountHandle(
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

            return mgr.CreateTimer(timer).ToHandle<TimerCountCycle>();
        }

        // 固定次数间隔循环 - 返回 ID
        public static long CycleCountID(
            this ITimerMgr mgr,
            int loopNum,
            float delay,
            bool isRemove,
            Action<TimerCountCycle> onTrigger,
            Action<TimerCountCycle> onCompleted = null,
            bool isApplyTimeScale = true)
        {
            return ToTimerID(mgr.CycleCountHandle(
                loopNum,
                delay,
                isRemove,
                onTrigger,
                onCompleted,
                isApplyTimeScale));
        }

        // =========================================================
        // 无限延迟循环 - 返回 Handle
        // =========================================================

        public static TimerHandle<TimerDelayCycle> CycleDelayHandle(
            this ITimerMgr mgr,
            float delay,
            Action<TimerDelayCycle> onTrigger,
            bool isApplyTimeScale = true)
        {
            TimerDelayCycle timer = Mgr.RPool.Load<TimerDelayCycle>();

            timer.delay = delay;
            timer.isApplyTimeScale = isApplyTimeScale;
            timer.onTrigger = onTrigger;

            return mgr.CreateTimer(timer).ToHandle<TimerDelayCycle>();
        }

        // 无限延迟循环 - 返回 ID
        public static long CycleDelayID(
            this ITimerMgr mgr,
            float delay,
            Action<TimerDelayCycle> onTrigger,
            bool isApplyTimeScale = true)
        {
            return ToTimerID(mgr.CycleDelayHandle(
                delay,
                onTrigger,
                isApplyTimeScale));
        }

        // =========================================================
        // 二段循环计时器 - 返回 Handle
        // =========================================================

        public static TimerHandle<TimerTwoStageCycle> CycleTwoStageHandle(
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

            return mgr.CreateTimer(timer).ToHandle<TimerTwoStageCycle>();
        }

        // 二段循环计时器 - 返回 ID
        public static long CycleTwoStageID(
            this ITimerMgr mgr,
            float delay1,
            float delay2,
            Action<TimerTwoStageCycle> onTrigger,
            bool isApplyTimeScale = true)
        {
            return ToTimerID(mgr.CycleTwoStageHandle(
                delay1,
                delay2,
                onTrigger,
                isApplyTimeScale));
        }

        // =========================================================
        // 补间计时器 - 返回 Handle
        // =========================================================

        public static TimerHandle<TimerTween> CustomHandle(
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

        // 补间计时器 - 返回 ID
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
            return ToTimerID(mgr.CustomHandle(
                start,
                end,
                duration,
                playCount,
                isRemove,
                onValueChanged,
                onCompleted,
                type,
                isApplyTimeScale));
        }

        // =========================================================
        // 带泛型数据的补间计时器 - 返回 Handle
        // =========================================================

        public static TimerHandle<TimerTween<T>> CustomHandle<T>(
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

        // 带泛型数据的补间计时器 - 返回 ID
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
            return ToTimerID(mgr.CustomHandle(
                t,
                start,
                end,
                duration,
                playCount,
                isRemove,
                onValueChanged,
                onCompleted,
                type,
                isApplyTimeScale));
        }

        // =========================================================
        // Custom 公共创建逻辑
        // =========================================================

        private static TimerHandle<TTimer> CreateCustomTimer<TTimer>(
            ITimerMgr mgr,
            TTimer timer,
            float start,
            float end,
            float duration,
            int playCount,
            bool isRemove,
            Delegate onValueChanged,
            Delegate onCompleted,
            EaseTypes type,
            bool isApplyTimeScale)
            where TTimer : TimerTween
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

            return mgr.CreateTimer(timer).ToHandle<TTimer>();
        }
    }
}