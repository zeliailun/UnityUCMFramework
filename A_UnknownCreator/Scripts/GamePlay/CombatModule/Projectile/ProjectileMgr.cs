using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public sealed class ProjectileMgr : IProjectileMgr
    {
        private Dictionary<long, Projectile> projDict = new();

        private List<Projectile> projList = new();

        private int maxAttempts = 3;

        [JsonIgnore]
        public FilterSlot<(Projectile, GameObject), (bool, Unit)> projFilter { private set; get; } = new();

        //private ProjectileMgr() { }

        void IDearMgr.WorkWork()
        {
            projDict ??= new();
            projList ??= new();
            projFilter ??= new();
        }

        void IDearMgr.DoNothing()
        {
            projFilter.Clear();
            ReleaseAllProjectile();
            projDict = null;
            projList = null;
        }

        void IDearMgr.UpdateMGR()
        {
            Projectile proj;
            float deltaTime = CustomTime.DeltaTime();
            for (int i = projList.Count - 1; i >= 0; i--)
            {
                proj = projList[i];
                if (proj is null || proj.isRelease) continue;
                proj.UpdateProjectile(deltaTime);
            }
        }



        public void ReleaseProjectile(long id)
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

            while (projList.Count > 0)
            {
                attemptCount++;

                int count = projList.Count;

                Projectile value;
                for (int i = count - 1; i >= 0; i--)
                {
                    if (i >= projList.Count)
                        continue;

                    value = projList[i];

                    projList.RemoveAt(i);

                    if (value == null)
                        continue;

                    projDict.Remove(value.id);
                    Mgr.RPool.Release(value);
                }

                if (attemptCount > maxAttempts)
                {
                    UCMDebug.LogWarning("投射物释放可能触发了死循环");
                    break;
                }
            }

            projDict.Clear();
        }

        public Projectile GetProjectile(long id)
        => projDict.TryGetValue(id, out var value) ? value : null;

        public bool IsValidProjectile(Projectile pb)
        => pb != null &&
           projDict.TryGetValue(pb.id, out _) &&
           !pb.isRelease;

        public bool IsValidProjectile(long id)
        => projDict.TryGetValue(id, out var pb) &&
        !pb.isRelease;

        public Projectile LaunchProjectile<IMvt, ICheck, Data>(ProjectileInfo<IMvt, ICheck, Data> info)
        where IMvt : class, IProjMvt
        where ICheck : class, IProjCheck
        where Data : ProjectileData, new()
        {
            return LaunchProjectile(info.mvt, info.check, info.data, info.kv);
        }


        public Projectile LaunchProjectile(IProjMvt mvt, IProjCheck check, ProjectileData data, IVariableMgr kv)
        {
            var proj = Mgr.RPool.Load<Projectile>();
            proj.InitProjectile(data, mvt, check, kv);
            projDict.Add(proj.id, proj);
            projList.Add(proj);
            proj.UpdateProjectile(CustomTime.DeltaTime());
            return proj;
        }

        public Projectile LaunchProjectile(ProjectileSnapshot snapshot)
        {

            return LaunchProjectile(snapshot.mvt, snapshot.check, snapshot.data, snapshot.kv);
        }
    }
}