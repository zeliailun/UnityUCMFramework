using System.Collections.Generic;


namespace UnknownCreator.Modules
{

    public interface IProjMvt : ICopyable<IProjMvt>
    {
        float sp { get; set; }

        void OnProjMvt(Projectile proj);
    }

    public interface IProjCheck : ICopyable<IProjCheck>
    {
        void OnProjCheck(Projectile proj, ref List<ProjCheckInfo> results);
    }

    public interface IProjHitCache
    {
        void ClearTargets();
    }

}