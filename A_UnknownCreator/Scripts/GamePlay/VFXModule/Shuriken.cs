using UnityEngine;

namespace UnknownCreator.Modules
{
    public class Shuriken : VfxBase
    {
        private bool isChangeSub;
        private Transform[] subs;
        private Vector3 rootDefaultScale;
        private Vector3[] subDefaultScales;

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

        public override void InitVfx(string vfxName, GameObject obj, IEntity owner)
        {
            base.InitVfx(vfxName, obj, owner);

            isChangeSub = false;
            vfx = rootObj != null ? rootObj.GetComp<ParticleSystem>() : null;

            CacheSubs();
            rootDefaultScale = rootT != null ? rootT.localScale : Vector3.one;
        }

        public override void PlayVfx()
        {
            if (isRelease || rootObj == null) return;

            rootObj.SetActive(true);
        }

        public override void StopVfx()
        {
            if (isRelease || rootObj == null) return;

            rootObj.SetActive(false);
        }

        public override void RestartVfx()
        {
            StopVfx();
            PlayVfx();
        }

        public void SetScale(float radius, bool isChangeSub)
        {
            SetScale(new Vector3(radius, radius, radius), isChangeSub);
        }

        public void SetScale(Vector3 scale, bool isChangeSub)
        {
            if (isRelease || rootT == null) return;

            rootT.localScale = scale;

            if (!isChangeSub) return;

            this.isChangeSub = true;

            Transform[] arr = vfxSubs;
            if (arr == null) return;

            for (int i = 0; i < arr.Length; i++)
            {
                Transform t = arr[i];
                if (t == null || t == rootT) continue;

                t.localScale = scale;
            }
        }

        public override void OnRelease()
        {
            ResetScale();

            isChangeSub = false;
            vfx = null;
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

            subs = rootT.GetComponentsInChildren<Transform>(true);
            subDefaultScales = new Vector3[subs.Length];

            for (int i = 0; i < subs.Length; i++)
                subDefaultScales[i] = subs[i] != null ? subs[i].localScale : Vector3.one;
        }

        private void ResetScale()
        {
            if (rootT == null) return;

            rootT.localScale = rootDefaultScale;

            if (!isChangeSub || subs == null || subDefaultScales == null) return;

            int count = Mathf.Min(subs.Length, subDefaultScales.Length);
            for (int i = 0; i < count; i++)
            {
                if (subs[i] != null)
                    subs[i].localScale = subDefaultScales[i];
            }
        }
    }
}
