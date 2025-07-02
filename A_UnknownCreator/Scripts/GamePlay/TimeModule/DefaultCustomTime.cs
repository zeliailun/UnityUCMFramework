using System;
using UnityEngine;
namespace UnknownCreator.Modules
{
    public class DefaultCustomTime : ICustomTime
    {
        public Action OnPause { get; set; }
        public Action OnResume { get; set; }
        public float DeltaTimeScale { get; set; } = 1;
        public float LocalTimeScale
        {
            set => Time.timeScale = value <= Mathf.Epsilon ? 0 : value;
            get => Time.timeScale;
        }

        public bool IsPause => Mathf.Approximately(LocalTimeScale, 0);

        private float beforeTimeScale;
        private int pauseCount;

        public float DeltaTime(bool isApplyTimeScale = true)
        => isApplyTimeScale ? Time.deltaTime * DeltaTimeScale : Time.unscaledDeltaTime;

        public void PauseGame()
        {
            ++pauseCount;
            if (pauseCount > 0 && !IsPause)
            {
                beforeTimeScale = LocalTimeScale;
                LocalTimeScale = 0;
                OnPause?.Invoke();
            }
        }

        public void ResumeGame(bool isClear)
        {
            pauseCount = isClear ? 0 : Math.Max(0, pauseCount - 1);
            if (pauseCount == 0 && IsPause)
            {
                LocalTimeScale = beforeTimeScale;
                OnResume?.Invoke();
            }
        }
    }
}