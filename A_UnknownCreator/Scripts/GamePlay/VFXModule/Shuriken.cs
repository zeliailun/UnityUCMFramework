using UnityEngine;
namespace UnknownCreator.Modules
{
    public class Shuriken : VfxBase
    {
        private bool isChangeSub;

        public Transform[] vfxSubs => subs ??= rootT.GetComponentsInChildren<Transform>();
        private Transform[] subs;

        public ParticleSystem vfx { private set; get; }

        public override void InitVfx(string vfxName, GameObject obj, IEntity owner)
        {
            base.InitVfx(vfxName, obj, owner);
            isChangeSub = false;
            vfx = rootObj.GetComp<ParticleSystem>();
        }


        public override void PlayVfx()
        {
            if (!isRelease)
                rootObj.SetActive(true);
        }

        public override void StopVfx()
        {
            if (!isRelease)
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

        public void SetScale(Vector3 radius, bool isChangeSub)
        {
            if (isRelease || rootT == null) return;
            rootT.localScale = radius;
            if (isChangeSub)
            {
                this.isChangeSub = isChangeSub;
                foreach (var ps in vfxSubs)
                    ps.localScale = radius;
            }
        }


        public override void OnRelease()
        {

            SetScale(1, isChangeSub);
            vfx = null;
            subs = null;
        }

    }
}