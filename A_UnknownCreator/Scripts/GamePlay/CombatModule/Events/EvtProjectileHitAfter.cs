using UnityEngine;

namespace UnknownCreator.Modules
{
    public readonly struct EvtProjectileHitAfter : IBusEvent
    {
        public readonly Projectile proj;
        public readonly Unit attacker;
        public readonly Unit target;
        public readonly GameObject obj;
        public readonly RaycastHit raycastHit;
        public readonly bool isMultiTarget;
        public readonly int targetIndex;

        public EvtProjectileHitAfter(Projectile proj, Unit attacker, Unit target, GameObject obj, RaycastHit raycastHit, bool isMultiTarget, int targetIndex)
        {
            this.proj = proj;
            this.attacker = attacker;
            this.target = target;
            this.obj = obj;
            this.raycastHit = raycastHit;
            this.isMultiTarget = isMultiTarget;
            this.targetIndex = targetIndex;
        }

    }
}
