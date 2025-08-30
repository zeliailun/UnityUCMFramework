using System;
namespace UnknownCreator.Modules
{
    public static class CustomTime
    {
        public static Action OnPause { set => customTime.OnPause = value; get => customTime.OnPause; }
        public static Action OnResume { set => customTime.OnResume = value; get => customTime.OnResume; }

        public static float LocalTimeScale { set => customTime.LocalTimeScale = value; get => customTime.LocalTimeScale; }

        public static bool IsPause => customTime.IsPause;

        private static ICustomTime customTime = new DefaultCustomTime();

        public static void SetCustomTime(ICustomTime value)
        {
            customTime = value;
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
    }
}