using System;
namespace UnknownCreator.Modules
{
    public interface ICustomTime
    {
        Action OnPause { set; get; }
        Action OnResume { set; get; }

        float DeltaTimeScale { get; set; }

        float LocalTimeScale { set; get; }

        bool IsPause { get; }

        float DeltaTime(bool isApplyTimeScale = true);

        void PauseGame();

        void ResumeGame(bool isClear);
    }
}