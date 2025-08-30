using UnityEngine;
namespace UnknownCreator.Modules
{

    public class ProjectileData : IReference
    {
        public Unit owner { get; set; }

        public AbilityBase ability { get; set; }

        public Vector3 spawnPos { get; set; }

        public Quaternion spawnRot { get; set; }

        public double durationMax { get; set; } = ProjectileGlobals.MaxDuration;

        public double distanceMax { get; set; } = ProjectileGlobals.MaxDistance;

        public string projName { get; set; }

        public virtual void ObjRelease()
        {
            spawnPos = Vector3.zero;
            spawnRot = Quaternion.identity;
            durationMax = ProjectileGlobals.MaxDuration;
            distanceMax = ProjectileGlobals.MaxDistance;
            projName = null;
            owner = null;
            ability = null;
        }
    }
}