using System;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public interface IProjectileMgr : IDearMgr
    {
        Func<Projectile, GameObject, (bool, Unit)> FilterProjectileHit { set; get; }

        Projectile LaunchProjectile<IMvt, ICheck, Data>(ProjectileInfo<IMvt, ICheck, Data> info)
        where IMvt : class, IProjMvt
        where ICheck : class, IProjCheck
        where Data : ProjectileData, new ();

        ProjectileInfo<IMvt, ICheck, Data> CreateProjectileData<IMvt, ICheck, Data>()
        where IMvt : class, IProjMvt
        where ICheck : class, IProjCheck
        where Data : ProjectileData, new();

        void ReleaseProjectile(Projectile proj);
        void ReleaseProjectile(int id);
        void ReleaseAllProjectile();
        Projectile GetProjectile(int id);

        bool IsValidProjectile(Projectile proj);
        bool IsValidProjectile(int id);
    }
}