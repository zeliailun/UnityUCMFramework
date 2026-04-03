using UnityEngine;

namespace UnknownCreator.Modules
{
    public sealed class Projectile : IReference
    {
        public ProjectileData data { private set; get; }

        public IVariableMgr kv { get; private set; }

        public IProjMvt mvt { private set; get; }

        public IProjCheck check { private set; get; }

        public GameObject obj { private set; get; }

        public Transform objT { private set; get; }

        public Vector3 beforePos { private set; get; }

        public EntityId id { private set; get; }

        private bool pause;
        public bool isPause
        {
            get => pause;
            set
            {

                if (value != pause)
                {
                    pause = value;
                    data.ability?.OnProjectilePause(this);
                }
            }
        }

        public bool isRelease { private set; get; }

        private float timeCount;

        internal void InitProjectile(GameObject obj, ProjectileData data, IProjMvt mvt, IProjCheck check, IVariableMgr kv)
        {
            this.obj = obj;
            this.check = check;
            this.mvt = mvt;
            this.data = data;
            this.kv = kv;
            id = obj.GetEntityId();
            objT = obj.GetComponent<Transform>();
            objT.localScale = Vector3.one;
            objT.SetPositionAndRotation(this.data.spawnPos, this.data.spawnRot);
            timeCount = 0;
            isPause = false;
            isRelease = false;
        }

        internal void UpdateProjectile()
        {
            if (isRelease) return;

            if (!obj.activeSelf)
            {
                obj.SetActive(true);

                data.ability?.OnProjectileSpawn(this);
                if (isRelease) return;

                Mgr.Event.Send<Projectile>(this, CombatEvtGlobals.OnProjectileSpawned);
            }

            timeCount += CustomTime.DeltaTime();
            if (data.distanceMax <= 0 ||
                timeCount >= data.durationMax ||
                UnityGlobals.DistanceH(data.spawnPos, objT.position) >= data.distanceMax)
            {
                Mgr.Proj.ReleaseProjectile(id);
                return;
            }

            if (isPause) return;

            beforePos = objT.position;

            mvt?.OnProjMvt(this);

            if (isRelease) return;

            data.ability?.OnProjectileMotion(this);
            if (isRelease) return;

            Mgr.Event.Send<Projectile>(this, CombatEvtGlobals.OnProjectileMotion);
            if (isRelease) return;

            var hitResults = check?.OnProjCheck(this);

            if (hitResults.IsValid())
            {
                for (int i = 0; i < hitResults.Count; i++)
                {
                    var result = hitResults[i];
                    if (result.isHit)
                    {
                        (bool isOK, Unit target) = Mgr.Proj.FilterProjectileHit(this, result.target);
                        if (isOK)
                        {
                            var evt = new EvtProjectileHitAfter(
                                this, target, result.target, result.raycastHit, result.isMultiTarget, result.targetIndex);

                            data.ability?.OnProjectileHit(evt);

                            if (!isRelease)
                                Mgr.Event.Send<EvtProjectileHitAfter>(evt, CombatEvtGlobals.OnProjectileHitAfter);
                        }
                    }
                }
            }

        }


        public void ReplaceMovement(IProjMvt newMvt)
        {
            if (isRelease || newMvt is null) return;

            if (mvt != null)
            {
                Mgr.RPool.Release(mvt);
                mvt = null;
            }
            mvt = newMvt;
            Mgr.Event.Send<Projectile>(this, CombatEvtGlobals.OnProjectileMvtReplaced);
        }

        public void ReplaceCheck(IProjCheck newCheck)
        {
            if (isRelease || newCheck is null) return;

            if (check != null)
            {
                Mgr.RPool.Release(check);
                check = null;
            }
            check = newCheck;
            Mgr.Event.Send<Projectile>(this, CombatEvtGlobals.OnProjectileCheckReplaced);
        }

        void IReference.ObjRelease()
        {
            if (isRelease) return;

            isRelease = true;
            data.ability?.OnProjectileDestroy(this);
            Mgr.Event.Send<Projectile>(this, CombatEvtGlobals.OnProjectileDestroy);
            Mgr.RPool.Release(check);
            Mgr.RPool.Release(mvt);
            Mgr.GPool.Release(data.projName, obj);
            Mgr.RPool.Release(data);
            Mgr.RPool.Release(kv);
            check = null;
            mvt = null;
            data = null;
            kv = null;
            obj = null;
            objT = null;
        }
    }

    public struct ProjCheckInfo
    {
        public bool isHit;
        public bool isMultiTarget;
        public int targetIndex;
        public GameObject target;
        public RaycastHit raycastHit;
    }
}




