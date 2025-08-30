using System;

namespace UnknownCreator.Modules
{
    public interface ITimer
    {
        long id { get; }

        bool isStart { get; }

        Action onUpdate { get; set; }

        Action onRelease { get; set; }

        void Init();

        void Update();

        void Reset();

        void Pause(bool pause);
    }
}