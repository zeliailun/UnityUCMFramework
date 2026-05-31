using UnityEngine;

namespace UnknownCreator.Modules
{
    public interface IProjectileMgr : IDearMgr
    {
        FilterSlot<(Projectile, GameObject), (bool, Unit)> projFilter { get; }

        Projectile LaunchProjectile(ProjectileSnapshot snapshot);

        Projectile LaunchProjectile(IProjMvt mvt, IProjCheck check, ProjectileData data, IVariableMgr kv);

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