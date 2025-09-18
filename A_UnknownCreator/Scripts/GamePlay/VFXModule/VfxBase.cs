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

        public virtual bool isPlaying => rootObj.activeSelf;

        private ITimer timer;
        private Type type;
        private bool isFollowing;
        private Transform followTarget;
        private Vector3 followOffset;

        public virtual void InitVfx(string vfxName, GameObject obj, IEntity owner)
        {
            isRelease = false;
            isFollowing = false;
            followTarget = null;
            followOffset = Vector3.zero;

            this.owner = owner;
            this.vfxName = vfxName;
            if (owner != null)
                type = owner.GetType();

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

            if (owner != null)
            {

                if (Mgr.RPool.HasObject(type, owner))
                {
                    Destroy(null);
                    return;
                }


                if (isFollowing)
                {

                    rootT.position = followTarget == null ? owner.entP + followOffset : followTarget.position + followOffset;
                }

            }
        }

        public virtual void PlayVfx() { }

        public virtual void StopVfx() { }
        public virtual void RestartVfx() { }

        public virtual void PauseVfx(bool isPause) { }

        public void SetFollow(int bodyID, Vector3 offset)
        {
            if (owner != null)
            {
                isFollowing = true;
                followOffset = offset;
                followTarget = owner.GetBodyPart(bodyID);
            }

        }

        public void ClearFollow()
        {
            isFollowing = false;
            followTarget = null;
            followOffset = Vector3.zero;
        }

        public void SetParent(bool worldPositionStays)
        {
            if (owner == null) return;

            rootT.SetParent(owner.entT, worldPositionStays);
        }

        public void ClearParent()
        {
            Mgr.GPool.SetRoot(rootObj, false);
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