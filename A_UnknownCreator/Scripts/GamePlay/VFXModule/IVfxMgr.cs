namespace UnknownCreator.Modules
{
    public interface IVfxMgr : IDearMgr
    {
        public T CreateVfx<T>(string vfxName,IEntity owner = null)
        where T : class, IVfx;
        void DestroyVfx(int id);
        IVfx GetVfx(int id);
        T GetVfx<T>(int id)
        where T : class, IVfx;
        bool HasVfx(int id);
        void ReleaseAllVfx();
    }
}