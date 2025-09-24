using System.Collections.Generic;

namespace UnknownCreator.Modules
{

    public interface IProjMvt
    {
        void OnProjMvt(Projectile proj);
    }

    public interface IProjCheck
    {
        List<ProjCheckInfo> OnProjCheck(Projectile proj);
    }

}