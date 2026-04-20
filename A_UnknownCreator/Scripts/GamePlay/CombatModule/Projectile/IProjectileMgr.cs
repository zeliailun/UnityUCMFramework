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
        where Data : ProjectileData, new();

        void ReleaseProjectile(Projectile proj);
        void ReleaseProjectile(long id);
        void ReleaseAllProjectile();
        Projectile GetProjectile(long id);

        bool IsValidProjectile(Projectile proj);
        bool IsValidProjectile(long id);
    }
}