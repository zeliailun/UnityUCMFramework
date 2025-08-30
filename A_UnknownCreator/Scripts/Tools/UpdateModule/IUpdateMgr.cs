namespace UnknownCreator.Modules
{
    public interface IUpdateMgr : IDearMgr
    {
        int GetUpdateCount { get; }
        int GetFixedUpdateCount { get; }
        int GetLateUpdateCount { get; }
        void AddUpdata<T>(T eup) where T : IOnUpdate;
        void AddFixedUpdata<T>(T eup) where T : IOnFixedUpdate;
        void AddLateUpdata<T>(T eup) where T : IOnLateUpdate;
        void RemoveUpdata<T>(T eup) where T : IOnUpdate;
        void RemoveFixedUpdata<T>(T eup) where T : IOnFixedUpdate;
        void RemoveLateUpdata<T>(T eup) where T : IOnLateUpdate;
    }
}