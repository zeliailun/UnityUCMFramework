using System;
using UnityEngine;
namespace UnknownCreator.Modules
{
    public abstract class VfxBase : IVfx, IReference
    {
        public IEntity owner { private set; get; }

        public GameObject rootObj { private set; get; }

        public Transform rootT { private set; get; }

        public string vfxName { private set; get; }

        public int id { private set; get; }

        public bool isRelease { private set; get; }

        public bool isPlaying => rootObj.activeSelf;

        private ITimer timer;
        private Type type;

        public virtual void InitVfx(string vfxName, GameObject obj, IEntity owner)
        {
            isRelease = false;
            this.owner = owner;
            this.vfxName = vfxName;
            if (owner != null) type = owner.GetType();
            rootObj = obj;
            rootT = rootObj.GetComponent<Transform>();
            id = rootObj.GetInstanceID();
        }

        public virtual void DestroyVfx(float delay)
        {
            if (isRelease) return;

            if (timer.IsVaild())
            {
                timer.DestroySelf();
                timer = null;
            }

            if (delay > 0)
                timer = Mgr.Timer.CycleCount(1, delay, false, Destroy);
            else
                Destroy(null);
        }

        public virtual void DestroyImmediateVfx()
        {
            Destroy(null);
        }

        public virtual void UpdateVfx()
        {
            if (owner != null && Mgr.RPool.HasObject(type, owner))
                Destroy(null);
        }

        public virtual void PlayVfx() { }

        public virtual void StopVfx() { }
        public virtual void RestartVfx() { }

        public virtual void PauseVfx(bool isPause) { }

        public virtual void SetFollowOwner(bool worldPositionStays)
        {
            if (owner == null) return;

            rootT.SetParent(owner.entT, worldPositionStays);
        }

        public virtual void OnRelease()
        {

        }

        public virtual void ObjRelease()
        {
            if (isRelease) return;

            isRelease = true;

            timer.DestroySelf();
            timer = null;

            OnRelease();
            Mgr.GPool.Release(vfxName, rootObj);
            rootObj = null;
            rootT = null;
            owner = null;
            vfxName = null;
            type = null;
            id = -1;
        }

        private void Destroy(TimerCountCycle cycle)
        {
            Mgr.Vfx.DestroyVfx(id);
        }
    }

}