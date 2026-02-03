using UnityEngine;

namespace UnknownCreator.Modules
{
    public interface IVfxMgr : IDearMgr
    {
        public T CreateVfx<T>(string vfxName,IEntity owner = null)
        where T : class, IVfx;
        void DestroyVfx(EntityId id);
        IVfx GetVfx(EntityId id);
        T GetVfx<T>(EntityId id)
        where T : class, IVfx;
        bool HasVfx(EntityId id);
        void ReleaseAllVfx();
    }
}