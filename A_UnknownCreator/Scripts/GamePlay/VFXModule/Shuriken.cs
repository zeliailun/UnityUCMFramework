using System;
using UnityEngine;
namespace UnknownCreator.Modules
{
    public class Shuriken : VfxBase
    {
        public Transform[] vfxSubs
        => subs ??= rootT.GetComponentsInChildren<Transform>();
        private Transform[] subs;
        public ParticleSystem vfx { private set; get; }

        public override void InitVfx(string vfxName, GameObject obj, IEntity owner)
        {
            base.InitVfx(vfxName, obj, owner);
            vfx = rootObj.GetComp<ParticleSystem>();
        }


        public override void PlayVfx()
        {
            if (!isRelease && !isPlaying)
                rootObj.SetActive(true);
        }

        public override void StopVfx()
        {
            if (!isRelease && isPlaying)
                rootObj.SetActive(false);
        }

        public override void PauseVfx(bool isPause)
        {

        }

        public void SetScale(float radius, bool isChangeSub)
        {
            if (isRelease || rootT == null) return;
            var rd = new Vector3(radius, radius, radius);
            rootT.localScale = rd;
            if (isChangeSub)
            {
                foreach (var ps in vfxSubs)
                    ps.localScale = rd;
            }
        }


        public override void OnRelease()
        {
            SetScale(1, true);
            vfx = null;
            subs = null;
        }

    }
}