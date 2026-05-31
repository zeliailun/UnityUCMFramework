using System.Collections.Generic;
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

        public long id { private set; get; }

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

        // NonAlloc 命中结果缓存。
        // Projectile 是对象池复用的，所以这个 List 只会跟随 Projectile 创建一次。
        private List<ProjCheckInfo> hitResults = new();

        private float timeCount;

        internal void InitProjectile(ProjectileData data, IProjMvt mvt, IProjCheck check, IVariableMgr kv)
        {
            this.obj = Mgr.GPool.Load(data.projName, true, false); ;
            this.check = check;
            this.mvt = mvt;
            this.data = data;
            this.kv = kv;
            id = GlobalID.GetUniqueID();
            objT = obj.GetComponent<Transform>();
            objT.localScale = data.vfxScale;
            objT.SetPositionAndRotation(this.data.spawnPos, this.data.spawnRot);
            timeCount = 0;
            hitResults.Clear();
            isPause = false;
            isRelease = false;
        }

        internal void UpdateProjectile(float deltaTime)
        {
            if (isRelease) return;


            if (!obj.activeSelf)
            {
                obj.SetActive(true);

                data.ability?.OnProjectileSpawn(this);
                if (isRelease) return;

                GameEvtBus.Send<EvtProjectileSpawned>(new(this, data, kv, data.owner));
            }

            timeCount += deltaTime;
            if ((!data.isIgnoreDurationMax && timeCount >= data.durationMax) ||
                !data.isIgnoreDistanceMax && (data.distanceMax <= 0 ||
                UnityGlobals.DistanceH(data.spawnPos, objT.position) >= data.distanceMax))
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

            GameEvtBus.Send<EvtProjectileMotion>(new(this, data, kv, data.owner));

            if (isRelease) return;

            check.OnProjCheck(this, ref hitResults);
            if (hitResults.IsValid())
            {

                for (int i = 0; i < hitResults.Count; i++)
                {
                    ProjCheckInfo result = hitResults[i];

                    if (!result.isHit)
                        continue;

                    (bool isOK, Unit target) = Mgr.Proj.projFilter.Invoke((this, result.target));

                    if (!isOK)
                        continue;

                    var evt = new EvtProjectileHitAfter(
                        this,
                        data.owner,
                        target,
                        result.target,
                        result.raycastHit,
                        result.isMultiTarget,
                        result.targetIndex
                    );

                    data.ability?.OnProjectileHit(evt);

                    if (isRelease) return;

                    GameEvtBus.Send<EvtProjectileHitAfter>(evt);
                }

                hitResults.Clear();
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
        }


        public void ClearTargets()
        {
            if (check is IProjHitCache cache)
                cache.ClearTargets();
            hitResults.Clear();
        }

        public ProjectileSnapshot Copy()
        {
            return new ProjectileSnapshot
            {
                mvt = mvt?.Copy(),
                check = check?.Copy(),
                data = data?.Copy(),
                kv = kv?.Copy()
            };
        }

        void IReference.ObjRelease()
        {
            if (isRelease) return;

            isRelease = true;
            data.ability?.OnProjectileDestroy(this);
            GameEvtBus.Send<EvtProjectileDestroy>(new(this, data, kv, data.owner));
            hitResults.Clear();
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




