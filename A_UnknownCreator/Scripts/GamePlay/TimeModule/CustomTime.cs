using System;
namespace UnknownCreator.Modules
{

    public static class CustomTime
    {
        public static event Action OnPause
        {
            add => customTime.OnPause += value;
            remove => customTime.OnPause -= value;
        }

        public static event Action OnResume
        {
            add => customTime.OnResume += value;
            remove => customTime.OnResume -= value;
        }

        public static float LocalTimeScale { set => customTime.LocalTimeScale = value; get => customTime.LocalTimeScale; }

        public static bool IsPause => customTime.IsPause;

        private static ICustomTime customTime = new DefaultCustomTime();

        public static void SetCustomTime(ICustomTime value)
        {
            customTime = value ?? new DefaultCustomTime();
        }

        public static float DeltaTime(bool isApplyTimeScale = true)
        => customTime.DeltaTime(isApplyTimeScale);

        public static void PauseGame()
        {
            customTime.PauseGame();
        }

        public static void ResumeGame(bool isClear)
        {
            customTime.ResumeGame(isClear);
        }

        public static void SetTimeScale(float value)
        {
            customTime.SetTimeScale(value);
        }
        public static void ClearPauseEvents()
        {
            customTime.ClearPauseEvents();
        }

        public static void ClearResumeEvents()
        {
            customTime.ClearResumeEvents();
        }

        public static void ClearAllEvents()
        {
            customTime.ClearAllEvents();
        }
    }
}