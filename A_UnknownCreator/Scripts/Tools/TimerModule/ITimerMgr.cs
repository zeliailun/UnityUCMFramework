namespace UnknownCreator.Modules
{
    public interface ITimerMgr : IDearMgr
    {
        int GetTimerCount { get; }
        bool HasTimer(long id);
        bool HasTimer(ITimer timer);
        void RemoveTimer(long id);
        void RemoveTimer(ITimer timer);
        void ClearAllTimer();
        ITimer GetTimer(long id);
        ITimer CreateTimer(ITimer timer);
    }
}