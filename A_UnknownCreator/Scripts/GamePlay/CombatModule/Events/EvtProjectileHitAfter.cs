using UnityEngine;

namespace UnknownCreator.Modules
{
    public readonly struct EvtProjectileHitAfter
    {
        public readonly Projectile proj;
        public readonly Unit target;
        public readonly GameObject obj;
        public readonly Vector3 pos;


        public EvtProjectileHitAfter(Projectile proj, Unit target, GameObject obj, Vector3 pos)
        {
            this.proj = proj;
            this.target = target;
            this.obj = obj;
            this.pos = pos;
        }

    }
}
