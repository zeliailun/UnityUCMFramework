using UnityEngine;
namespace UnknownCreator.Modules
{
    public interface IVfx
    {
        EntityId id { get; }
        IEntity owner { get; }
        GameObject rootObj { get; }
        Transform rootT { get; }
        string vfxName { get; }
        bool isPlaying { get; }
        bool isRelease { get; }
        void InitVfx(string vfxName, GameObject obj, IEntity owner);
        void PlayVfx();
        void StopVfx();
        void RestartVfx();
        void PauseVfx(bool isPause);
        void UpdateVfx();
        void DestroyVfx(float delay);
        void DestroyImmediateVfx();
        void SetFollow(int bodyID, Vector3 offset);
        void SetParent(bool worldPositionStays);
        void ClearFollow();
        void ClearParent();
    }


}