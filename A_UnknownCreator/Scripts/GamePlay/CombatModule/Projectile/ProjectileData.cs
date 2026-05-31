using UnityEngine;
namespace UnknownCreator.Modules
{

    public class ProjectileData : IReference, ICopyable<ProjectileData>
    {
        public Unit owner { get; set; }

        public AbilityBase ability { get; set; }

        public Vector3 spawnPos { get; set; }

        public Quaternion spawnRot { get; set; }

        public Vector3 vfxScale { get; set; } = Vector3.one;

        public double durationMax { get; set; } = ProjectileGlobals.MaxDuration;

        public double distanceMax { get; set; } = ProjectileGlobals.MaxDistance;

        public string projName { get; set; }

        public bool isIgnoreDistanceMax { get; set; } = false;

        public bool isIgnoreDurationMax { get; set; } = false;

        public virtual ProjectileData Copy()
        {
            var proj = Mgr.RPool.Load<ProjectileData>();
            proj.owner = owner;
            proj.ability = ability;
            proj.spawnPos = spawnPos;
            proj.spawnRot = spawnRot;
            proj.vfxScale = vfxScale;
            proj.durationMax = durationMax;
            proj.distanceMax = distanceMax;
            proj.projName = projName;
            proj.isIgnoreDistanceMax = isIgnoreDistanceMax;
            proj.isIgnoreDurationMax = isIgnoreDurationMax;
            return proj;
        }


        public virtual void ObjRelease()
        {
            vfxScale = Vector3.one;
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