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
        public EntityId id { private set; get; }
        public bool isRelease { private set; get; }

        public virtual bool isPlaying => !isRelease && rootObj != null && rootObj.activeSelf;

        private ITimer timer;
        private Type ownerType;
        private bool isFollowing;
        private Transform followTarget;
        private Vector3 followOffset;

        public virtual void InitVfx(string vfxName, GameObject obj, IEntity owner)
        {
            isRelease = false;
            ClearFollowState();
            ClearTimer();

            this.owner = owner;
            this.vfxName = vfxName;
            ownerType = owner?.GetType();

            rootObj = obj;
            rootT = rootObj != null ? rootObj.transform : null;
            id = rootObj != null ? rootObj.GetEntityId() : default;
        }

        public virtual void DestroyVfx(float delay)
        {
            if (isRelease) return;

            ClearTimer();

            if (delay > 0f)
                timer = Mgr.Timer.CycleCount(1, delay, false, Destroy);
            else
                Destroy(null);
        }

        public virtual void DestroyImmediateVfx()
        {
            DestroyVfx(0f);
        }

        public virtual void UpdateVfx()
        {
            if (isRelease || rootT == null) return;

            if (owner == null) return;

            // owner 已经被对象池回收时，特效也应该自动销毁，避免挂在无效目标上。
            if (ownerType != null && Mgr.RPool.HasObject(ownerType, owner))
            {
                DestroyVfx(0f);
                return;
            }

            if (!isFollowing) return;

            rootT.position = followTarget == null
                ? owner.entP + followOffset
                : followTarget.position + followOffset;
        }

        public virtual void PlayVfx() { }

        public virtual void StopVfx() { }

        public virtual void RestartVfx()
        {
            StopVfx();
            PlayVfx();
        }

        public virtual void PauseVfx(bool isPause) { }

        public void SetFollow(int bodyID, Vector3 offset)
        {
            if (isRelease || owner == null) return;

            isFollowing = true;
            followOffset = offset;
            followTarget = owner.GetBodyPart(bodyID);
        }

        public void ClearFollow()
        {
            ClearFollowState();
        }

        public void SetParent(bool worldPositionStays)
        {
            if (isRelease || owner == null || rootT == null) return;

            rootT.SetParent(owner.entT, worldPositionStays);
        }

        public void ClearParent()
        {
            if (rootObj == null) return;

            Mgr.GPool.SetRoot(rootObj, false);
        }

        public virtual void OnRelease() { }

        public virtual void ObjRelease()
        {
            if (isRelease) return;

            ClearTimer();
            ClearFollowState();

            // 注意：OnRelease 必须在 isRelease = true 之前执行。
            // 否则子类如果调用 SetScale / StopVfx 这类带 isRelease 判断的方法，会直接被拦截。
            OnRelease();

            isRelease = true;

            if (rootObj != null)
                Mgr.GPool.Release(vfxName, rootObj);

            owner = null;
            rootObj = null;
            rootT = null;
            vfxName = null;
            ownerType = null;
            id = default;
        }

        private void Destroy(TimerCountCycle cycle)
        {
            if (isRelease) return;

            Mgr.Vfx.DestroyVfx(id);
        }

        private void ClearTimer()
        {
            if (timer != null && timer.IsValid())
                timer.DestroySelf();

            timer = null;
        }

        private void ClearFollowState()
        {
            isFollowing = false;
            followTarget = null;
            followOffset = Vector3.zero;
        }
    }
}
