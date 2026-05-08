using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public sealed class VfxMgr : IVfxMgr
    {
        private Dictionary<EntityId, IVfx> vfxDict = new();
        private List<IVfx> vfxList = new();

        void IDearMgr.WorkWork()
        {
            vfxDict ??= new Dictionary<EntityId, IVfx>();
            vfxList ??= new List<IVfx>();
        }

        void IDearMgr.DoNothing()
        {
            ReleaseAllVfx();
            vfxDict = null;
            vfxList = null;
        }

        void IDearMgr.UpdateMGR()
        {
            if (vfxList == null) return;

            // 倒序遍历：UpdateVfx 里可能会销毁自己并从 vfxList 移除，正序会跳过元素。
            for (int i = vfxList.Count - 1; i >= 0; i--)
            {
                vfxList[i]?.UpdateVfx();
            }
        }

        public T CreateVfx<T>(string vfxName, IEntity owner = null)
            where T : class, IVfx
        {
            if (string.IsNullOrEmpty(vfxName)) return null;

            vfxDict ??= new Dictionary<EntityId, IVfx>();
            vfxList ??= new List<IVfx>();

            GameObject obj = Mgr.GPool.Load(vfxName, true, false);
            if (obj == null) return null;

            T vfx = Mgr.RPool.Load<T>();
            if (vfx == null)
            {
                Mgr.GPool.Release(vfxName, obj);
                return null;
            }

            vfx.InitVfx(vfxName, obj, owner);

            if (vfxDict.ContainsKey(vfx.id))
            {
                Mgr.RPool.Release(vfx);
                return null;
            }

            vfxDict.Add(vfx.id, vfx);
            vfxList.Add(vfx);
            return vfx;
        }

        public void DestroyVfx(EntityId id)
        {
            if (vfxDict == null || vfxList == null) return;

            if (!vfxDict.Remove(id, out IVfx vfx)) return;

            vfxList.Remove(vfx);

            if (vfx != null && !vfx.isRelease)
                Mgr.RPool.Release(vfx);
        }

        public IVfx GetVfx(EntityId id)
        {
            if (vfxDict == null) return null;

            return vfxDict.TryGetValue(id, out IVfx result) ? result : null;
        }

        public T GetVfx<T>(EntityId id)
            where T : class, IVfx
        {
            return GetVfx(id) as T;
        }

        public bool HasVfx(EntityId id)
        {
            return GetVfx(id) != null;
        }

        public void ReleaseAllVfx()
        {
            if (vfxList == null) return;

            for (int i = vfxList.Count - 1; i >= 0; i--)
            {
                IVfx vfx = vfxList[i];
                if (vfx != null && !vfx.isRelease)
                    Mgr.RPool.Release(vfx);
            }

            vfxList.Clear();
            vfxDict?.Clear();
        }
    }
}
