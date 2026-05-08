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
        void OnProjCheck(Projectile proj,ref List<ProjCheckInfo> results);
    }

}