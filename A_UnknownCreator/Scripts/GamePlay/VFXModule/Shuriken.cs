using UnityEngine;

namespace UnknownCreator.Modules
{
    public class Shuriken : VfxBase
    {
        private bool isChangeSub;
        private Transform[] subs;
        private Vector3 rootDefaultScale;
        private Vector3[] subDefaultScales;
        private BodyParticleVfx bodyParticleVfx;
        private UnitVfxTarget bodyVfxTarget;

        public Transform[] vfxSubs
        {
            get
            {
                if (subs == null && rootT != null)
                    CacheSubs();

                return subs;
            }
        }

        public ParticleSystem vfx { private set; get; }

        public bool hasBodyParticleVfx =>
            bodyParticleVfx != null;


        public override void InitVfx(
        string vfxName,
        GameObject obj,
        IEntity owner)
        {
            base.InitVfx(vfxName, obj, owner);

            isChangeSub = false;

            vfx = rootObj != null
                ? rootObj.GetComp<ParticleSystem>()
                : null;

            bodyParticleVfx = rootObj != null
                ? rootObj.GetComponent<BodyParticleVfx>()
                : null;

            bodyVfxTarget = owner?.entT != null
                ? owner.ent.GetComp<UnitVfxTarget>()
                : null;

            if (bodyParticleVfx != null)
                bodyParticleVfx.ResetBinding();

            CacheSubs();

            rootDefaultScale = rootT != null
                ? rootT.localScale
                : Vector3.one;
        }


        public override void UpdateVfx()
        {
            base.UpdateVfx();

            // base.UpdateVfx 可能因为 owner 失效而立即销毁自己。
            if (isRelease)
                return;

            // 直接接入 VfxMgr.UpdateMGR -> IVfx.UpdateVfx 生命周期。
            if (bodyParticleVfx != null)
                bodyParticleVfx.UpdateBinding();
        }


        public override void PlayVfx()
        {
            if (isRelease || rootObj == null)
                return;

            rootObj.SetActive(true);

            // 普通粒子继续使用原 Shuriken 的根节点激活逻辑；
            // 仅额外启动运行时身体发射器。
            if (bodyParticleVfx != null)
                bodyParticleVfx.PlayBoundEmitters();
        }


        public override void StopVfx()
        {
            if (isRelease || rootObj == null)
                return;

            rootObj.SetActive(false);
        }


        public override void RestartVfx()
        {
            StopVfx();
            PlayVfx();
        }


        public bool BindBody()
        {
            if (isRelease ||
                bodyParticleVfx == null ||
                bodyVfxTarget == null)
            {
                return false;
            }

            return bodyParticleVfx.Bind(bodyVfxTarget);
        }


        public void ClearBodyVfxBinding()
        {
            if (isRelease)
                return;

            if (bodyParticleVfx != null)
                bodyParticleVfx.ResetBinding();
        }


        public void SetScale(
            float radius,
            bool isChangeSub)
        {
            SetScale(
                new Vector3(radius, radius, radius),
                isChangeSub);
        }


        public void SetScale(
            Vector3 scale,
            bool isChangeSub)
        {
            if (isRelease || rootT == null)
                return;

            rootT.localScale = scale;

            if (!isChangeSub)
                return;

            this.isChangeSub = true;

            Transform[] arr = vfxSubs;

            if (arr == null)
                return;

            for (int i = 0; i < arr.Length; i++)
            {
                Transform t = arr[i];

                if (t == null || t == rootT)
                    continue;

                t.localScale = scale;
            }
        }


        public override void OnRelease()
        {
            ResetScale();

            if (bodyParticleVfx != null)
                bodyParticleVfx.ResetBinding();

            isChangeSub = false;

            vfx = null;
            bodyParticleVfx = null;
            bodyVfxTarget = null;

            subs = null;
            subDefaultScales = null;
        }


        private void CacheSubs()
        {
            if (rootT == null)
            {
                subs = null;
                subDefaultScales = null;
                return;
            }

            subs =
                rootT.GetComponentsInChildren<Transform>(true);

            subDefaultScales =
                new Vector3[subs.Length];

            for (int i = 0; i < subs.Length; i++)
            {
                subDefaultScales[i] =
                    subs[i] != null
                        ? subs[i].localScale
                        : Vector3.one;
            }
        }


        private void ResetScale()
        {
            if (rootT == null)
                return;

            rootT.localScale = rootDefaultScale;

            if (!isChangeSub ||
                subs == null ||
                subDefaultScales == null)
            {
                return;
            }

            int count = Mathf.Min(
                subs.Length,
                subDefaultScales.Length);

            for (int i = 0; i < count; i++)
            {
                if (subs[i] != null)
                {
                    subs[i].localScale =
                        subDefaultScales[i];
                }
            }
        }
    }
}
