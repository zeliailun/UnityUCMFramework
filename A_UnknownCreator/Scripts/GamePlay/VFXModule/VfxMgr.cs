using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public sealed class VfxMgr : IVfxMgr
    {
        private Dictionary<EntityId, IVfx> vfxDict = new();

        private List<IVfx> vfxList = new();

        //private VfxMgr() { }

        void IDearMgr.WorkWork()
        {
            vfxDict ??= new();
            vfxList ??= new();
        }

        void IDearMgr.DoNothing()
        {
            vfxDict = null;
            vfxList = null;
        }

        void IDearMgr.UpdateMGR()
        {
            for (int i = 0; i < vfxList.Count; i++)
                vfxList[i]?.UpdateVfx();
        }

        public T CreateVfx<T>(string vfxName, IEntity owner)
        where T : class, IVfx
        {
            var obj = Mgr.GPool.Load(vfxName, true, false);
            var vfx = Mgr.RPool.Load<T>();
            vfx.InitVfx(vfxName, obj, owner);
            vfxDict.Add(vfx.id, vfx);
            vfxList.Add(vfx);
            return vfx;
        }

        public void DestroyVfx(EntityId id)
        {
            if (vfxDict.Remove(id, out var vfx))
            {
                vfxList.Remove(vfx);
                Mgr.RPool.Release(vfx);
            }

        }

        public IVfx GetVfx(EntityId id)
        => vfxDict.TryGetValue(id, out var result) ? result : null;

        public T GetVfx<T>(EntityId id)
        where T : class, IVfx
        => GetVfx(id) as T;

        public bool HasVfx(EntityId id)
        => GetVfx(id) != null;

        public void ReleaseAllVfx()
        {
            vfxDict.Clear();
            IVfx vfx;
            for (int i = vfxList.Count - 1; i >= 0; i--)
            {
                vfx = vfxList[i];
                vfxList.RemoveAt(i);
                Mgr.RPool.Release(vfx);
            }
        }
    }
}