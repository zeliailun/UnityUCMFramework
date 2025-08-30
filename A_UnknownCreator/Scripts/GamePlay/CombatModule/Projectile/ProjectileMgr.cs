using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public sealed class ProjectileMgr : IProjectileMgr
    {
        private Dictionary<int, Projectile> projDict = new();

        private List<Projectile> projList = new();

        private int maxAttempts = 3;

        //private ProjectileMgr() { }

        void IDearMgr.WorkWork()
        {
            projDict ??= new();
            projList ??= new();
        }

        void IDearMgr.DoNothing()
        {
            ReleaseAllProjectile();
            projDict = null;
            projList = null;
        }

        [JsonIgnore] public Func<Projectile, GameObject, (bool, Unit)> FilterProjectileHit { set; get; }

        public void ReleaseProjectile(int id)
        {
            if (projDict.Remove(id, out var value))
            {
                projList.Remove(value);
                Mgr.RPool.Release(value);
            }
        }

        public void ReleaseProjectile(Projectile proj)
        {
            if (IsValidProjectile(proj) && projDict.Remove(proj.id, out Projectile value))
            {
                projList.Remove(value);
                Mgr.RPool.Release(value);
            }
        }


        public void ReleaseAllProjectile()
        {
            int attemptCount = 0;
            Projectile value;
            while (projList.Count > 0)
            {
                projDict.Clear();

                attemptCount++;

                for (int i = projList.Count - 1; i >= 0; i--)
                {
                    value = projList[i];
                    if (projList.Remove(value))
                        Mgr.RPool.Release(value);
                }

                if (attemptCount > maxAttempts)
                {
                    UCMDebug.LogWarning("投射物生成可能触发了死循环");
                    break;
                }
            }

        }

        public Projectile GetProjectile(int id)
        => projDict.TryGetValue(id, out var value) ? value : null;

        public bool IsValidProjectile(Projectile pb)
        => pb != null &&
           projDict.TryGetValue(pb.id, out _) &&
           !pb.isRelease;

        public bool IsValidProjectile(int id)
        => projDict.TryGetValue(id, out var pb) &&
        !pb.isRelease;

        public Projectile LaunchProjectile<IMvt, ICheck, Data>(ProjectileInfo<IMvt, ICheck, Data> info)
        where IMvt : class, IProjMvt
        where ICheck : class, IProjCheck
        where Data : ProjectileData, new()
        {
            var obj = Mgr.GPool.Load(info.data.projName, true, false);
            var proj = Mgr.RPool.Load<Projectile>();
            proj.InitProjectile(obj, info.data, info.mvt, info.check, info.kv);
            projDict.Add(proj.id, proj);
            projList.Add(proj);
            proj.UpdateProjectile();
            return proj;
        }

        public ProjectileInfo<IMvt, ICheck, Data> CreateProjectileData<IMvt, ICheck, Data>()
        where IMvt : class, IProjMvt
        where ICheck : class, IProjCheck
        where Data : ProjectileData, new()
        {
            var mvt = Mgr.RPool.Load<IMvt>();
            var check = Mgr.RPool.Load<ICheck>();
            var vb = Mgr.RPool.Load<VariableMgr>();
            var data = Mgr.RPool.Load<Data>();
            return new(mvt, check, vb, data);
        }

        void IDearMgr.UpdateMGR()
        {
            for (int i = 0; i < projList.Count; i++)
                projList[i]?.UpdateProjectile();
        }
    }

    public struct ProjectileInfo<IMvt, ICheck, Data>
    where IMvt : class, IProjMvt
    where ICheck : class, IProjCheck
    where Data : ProjectileData, new()
    {
        public IMvt mvt;
        public ICheck check;
        public IVariableMgr kv;
        public Data data;

        public ProjectileInfo(IMvt mvt, ICheck check, IVariableMgr kv, Data data)
        {
            this.mvt = mvt;
            this.check = check;
            this.kv = kv;
            this.data = data;
        }

        public ProjectileInfo<IProjMvt, IProjCheck, ProjectileData> As()
        {
            return new ProjectileInfo<IProjMvt, IProjCheck, ProjectileData>(
                mvt,
                check,
                kv,
                data
            );
        }

        public bool isVaild => mvt != null && check != null && kv != null && data != null;
    }
}