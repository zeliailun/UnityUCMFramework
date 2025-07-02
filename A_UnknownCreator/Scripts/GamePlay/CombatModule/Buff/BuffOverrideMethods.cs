using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnknownCreator.Modules
{
    public abstract partial class BuffBase
    {
        /// <summary>
        /// UI图标
        /// </summary>
        /// <returns></returns>
        public virtual Texture2D GetIcon() => null;

        /// <summary>
        /// 运动控制器优先级
        /// </summary>
        /// <returns></returns>
        public virtual int GetMotionPriority() => 0;

        /// <summary>
        /// 是内部BUFF吗
        /// </summary>
        /// <returns></returns>
       public virtual bool IsInternal() => false;

        /// <summary>
        /// 是否隐藏UI
        /// </summary>
        /// <returns></returns>
        public virtual bool IsHidden() => true;

        /// <summary>
        /// 是负面BUFF吗
        /// </summary>
        /// <returns></returns>
        public virtual bool IsDebuff() => false;

        /// <summary>
        /// 是否可以叠加
        /// </summary>
        /// <returns></returns>
        public virtual bool IsStacked() => false;

        /// <summary>
        /// 是否可以驱散
        /// </summary>
        /// <returns></returns>
        public virtual bool IsPurgable() => true;

        /// <summary>
        /// 是否在单位死亡时移除
        /// </summary>
        /// <returns></returns>
        public virtual bool IsDeathRemove() => true;

        /// <summary>
        /// 统计数据是否可以叠加
        /// </summary>
        /// <returns></returns>
        public virtual bool IsStatsStacked() => false;

        //==============================================================================================================

        /// <summary>
        /// 初始化完成时调用,还未添加到列表中
        /// </summary>
        public virtual void OnInitialized() { }

        /// <summary>
        /// 创建完成时调用
        /// </summary>
        public virtual void OnCreated() { }

        /// <summary>
        /// 当重复添加不可叠加的BUFF时调用
        /// </summary>
        public virtual void OnRefresh() { }

        /// <summary>
        /// 销毁前调用，在该函数中调用销毁函数无效
        /// </summary>
        public virtual void OnRemove(bool isClear) { }

        /// <summary>
        /// 进入对象池前调用
        /// </summary>
        protected virtual void OnRelease() { }

        /// <summary>
        /// 在对象池中销毁时调用
        /// </summary>
        protected virtual void OnDestroy() { }

        /// <summary>
        /// 遍历结尾时调用
        /// </summary>
        protected virtual void OnUpdate() { }

        public virtual void OnProjectileHit(int projID, Vector3 position, GameObject target, IVariableMgr kv) { }

        public virtual void OnProjectileMotion(int projID, Vector3 position, IVariableMgr kv) { }

        public virtual void OnProjectilePause(int projID, Vector3 position, IVariableMgr kv) { }

        /// <summary>
        /// 当堆叠计数改变时调用
        /// </summary>
        /// <param name="newValue"></param>
        /// <param name="oldValue"></param>
        protected virtual void OnStackCountChanged(int newValue, int oldValue) { }


        /// <summary>
        /// 当前持续时间改变时
        /// </summary>
        /// <param name="dur"></param>
        protected virtual void OnDurationChanged(double newDuration, double oldDuration)
        {

        }

        /// <summary>
        /// 计时器调用的固定方法
        /// </summary>
        protected virtual void OnIntervalThink() { }

        protected virtual void OnUpdateMotionController() { }

        protected virtual void OnMotionControllerInterrupted() { }

    }
}
