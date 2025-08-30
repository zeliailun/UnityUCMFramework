using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    /*
    public interface IProjectile
    {
        bool isPause { get; set; }

        bool isActivated { get; }

        int id { get; }

        Vector3 beforePos { get; }

        List<int> targetList { get; }

        GameObject obj { get; }

        ProjectileData data { get; }

        IProjectileMotion motion { get; }

        IProjectileCheck check { get; }

        void InitProjectile(ProjectileData oldData, GameObject go, IProjectileCheck check, IProjectileMotion motion);

        void UpdateProjectile();
    }
    */

    public interface IProjMvt
    {
        void OnProjMvt(Projectile proj);
    }

    public interface IProjCheck
    {
        List<ProjCheckInfo> OnProjCheck(Projectile proj);
    }

}