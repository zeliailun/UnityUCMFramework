using System;

namespace UnknownCreator.Modules
{
    public interface ITimer
    {
        long id { get; }

        bool isStart { get; }

        bool isInited { get; }

        Action onUpdate { get; set; }

        Action onRelease { get; set; }

        void Reset();

        void Pause(bool pause);
    }

    internal interface IInternalTimer : ITimer
    {
        void Init();
        void Update();
    }
}