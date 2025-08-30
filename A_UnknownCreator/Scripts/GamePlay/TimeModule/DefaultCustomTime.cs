using System;
using UnityEngine;
namespace UnknownCreator.Modules
{
    public sealed class DefaultCustomTime : ICustomTime
    {
        public Action OnPause { get; set; }
        public Action OnResume { get; set; }

        public float LocalTimeScale
        {
            get => localTimeScale;
            set => localTimeScale = GetTimeScaleValue(value);
        }

        public bool IsPause => pauseCount > 0;

        private float localTimeScale = 1f;
        private float beforeTimeScale = 1f;
        private int pauseCount;

        public float DeltaTime(bool isApplyTimeScale = true)
        => isApplyTimeScale ? Time.deltaTime * LocalTimeScale : Time.unscaledDeltaTime;

        public void PauseGame()
        {
            if (pauseCount++ == 0)
            {
                beforeTimeScale = Time.timeScale;
                Time.timeScale = 0;
                OnPause?.Invoke();
            }
        }

        public void ResumeGame(bool isClear)
        {
            pauseCount = isClear ? 0 : Math.Max(0, pauseCount - 1);
            if (pauseCount == 0)
            {
                Time.timeScale = beforeTimeScale;
                OnResume?.Invoke();
            }
        }

        public void SetTimeScale(float value)
        {
            float newTimeScale = GetTimeScaleValue(value);

            if (IsPause)
                beforeTimeScale = newTimeScale;
            else
                Time.timeScale = newTimeScale;
        }

        private float GetTimeScaleValue(float value) => value <= Mathf.Epsilon ? 0 : value;
    }
}