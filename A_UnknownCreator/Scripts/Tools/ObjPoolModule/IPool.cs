
namespace UnknownCreator.Modules
{
    public interface IPool
    {
        string poolName { get; }
        int maxNum { get; }
        int remainingNum { get; }
        
        int storageCount { get; }
        float clearInterval { get; }
        void UpdatePool();
        void ClearPool();
        void DestroyPool();
        bool HasObject(object obj);
    }
}