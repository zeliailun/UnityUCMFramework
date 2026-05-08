using System;
namespace UnknownCreator.Modules
{
    public interface ICustomTime
    {
        event Action OnPause;

        event Action OnResume;

        float LocalTimeScale { set; get; }

        bool IsPause { get; }

        float DeltaTime(bool isApplyTimeScale = true);

        void PauseGame();

        void ResumeGame(bool isClear);
        void SetTimeScale(float value);


        void ClearPauseEvents();

        void ClearResumeEvents();

        void ClearAllEvents();
    }
}