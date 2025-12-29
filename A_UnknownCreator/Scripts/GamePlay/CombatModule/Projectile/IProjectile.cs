using System.Collections.Generic;
using UnityEngine;


namespace UnknownCreator.Modules
{

    public interface IProjMvt
    {
        float sp {  get; set; }
        void OnProjMvt(Projectile proj);
    }

    public interface IProjCheck
    {
        List<ProjCheckInfo> OnProjCheck(Projectile proj);
    }

}