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
        private Action<TimerCountCycle> destroyVfx;
        private Type type;

        public virtual void InitVfx(string vfxName, GameObject obj, IEntity owner)
        {
            isRelease = false;
            this.owner = owner;
            this.vfxName = vfxName;
            rootObj = obj;
            rootT = rootObj.GetComponent<Transform>();
            id = rootObj.GetInstanceID();
            destroyVfx = Destroy;
            if (owner != null) type = owner.GetType();
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
                timer = Mgr.Timer.CycleCount(1, delay, false, destroyVfx);
            else
                Destroy(null);
        }

        public virtual void UpdateVfx()
        {
            if (Mgr.RPool.HasObject(type,owner))
                Destroy(null);
        }

        public virtual void PlayVfx() { }

        public virtual void StopVfx() { }

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
            OnRelease();
            Mgr.GPool.Release(vfxName, rootObj);
            rootT = null;
            rootObj = null;
            destroyVfx = null;
            owner = null;
        }

        private void Destroy(TimerCountCycle cycle)
        {
            Mgr.Vfx.DestroyVfx(id);
        }


    }

}