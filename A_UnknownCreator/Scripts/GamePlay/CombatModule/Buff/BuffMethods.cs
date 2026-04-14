using System;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public abstract partial class BuffBase
    {
        public void StartThink(double t)
        {
            delay = t;
            isEnableTimer = true;
        }

        public void StopThink()
        {
            isEnableTimer = false;
            timer = 0;
        }

        public void IncreaseStack()
        => ++stackCount;

        public void DecreaseStack()
        => --stackCount;



        public double GetRemainingTime()
        => origDuration - duration;


        public void DestroySelf()
        {
            owner?.buffC?.RemoveBuff(this);
        }


        #region 运动控制器

        public void ApplyMotionController()
        {
            if (!isInterruptMotion) return;

            var allBuffs = Mgr.Event.SendAllR<BuffBase>(UCMGameEvents.MotionInterrupted, owner.entID); 

            bool hasHigherPriority = false;

            if (allBuffs != null)
            {
                foreach (var existingBuff in allBuffs)
                {
                    if (ReferenceEquals(existingBuff, this)) continue;

                    int existingPriority = existingBuff.GetMotionPriority();
                    int myPriority = this.GetMotionPriority();

                    if (existingPriority > myPriority)
                    {
                        hasHigherPriority = true;
                        break;
                    }
                }
            }

            if (hasHigherPriority)
            {
                // 存在比自己高的 Buff，只打断自身
                this.RemoveMotionController();
                OnMotionControllerApplyFail();
            }
            else
            {
                // 打断所有比自己低的 Buff
                if (allBuffs != null)
                {
                    foreach (var existingBuff in allBuffs)
                    {
                        if (ReferenceEquals(existingBuff, this)) continue;

                        if (existingBuff.GetMotionPriority() < this.GetMotionPriority())
                        {
                            existingBuff.RemoveMotionController();
                        }
                    }
                }

                // 注册自己
                isInterruptMotion = false;
                Mgr.Event.AddR<BuffBase>(GetSelf, UCMGameEvents.MotionInterrupted, owner.entID);
            }
        }

        public void RemoveMotionController()
        {
            if (isInterruptMotion) return;
            isInterruptMotion = true;
            Mgr.Event.RemoveR<BuffBase>(GetSelf, UCMGameEvents.MotionInterrupted, owner.entID);
            OnMotionControllerInterrupted();
        }


   

        private BuffBase GetSelf() => this;


        #endregion


        #region 事件

        protected void AddActionEvent<T>(string name, Action<T> action, EntityId id = default, int priority = 0)
        {
            var key = (name, id);
            if (!evtDict.TryGetValue(key, out _))
            {
                Mgr.Event.Add<T>(action, name, id, priority);
                Action act = () => Mgr.Event.Remove<T>(action, name, id);
                evtDict.Add(key, act);
                clearEvt += act;
            }
        }

        protected void AddFuncEvent<T>(string name, Func<T> func, EntityId id = default, int priority = 1000)
        {
            var key = (name, id);
            if (!evtDict.TryGetValue(key, out _))
            {
                Mgr.Event.AddR<T>(func, name, id, priority);
                Action act = () => Mgr.Event.RemoveR<T>(func, name, id);
                evtDict.Add(key, act);
                clearEvt += act;
            }
        }

        protected void AddFuncEvent<T1, T2>(string name, Func<T1, T2> func, EntityId id = default, int priority = 1000)
        {
            var key = (name, id);
            if (!evtDict.TryGetValue(key, out _))
            {
                Mgr.Event.AddR<T1, T2>(func, name, id, priority);
                Action act = () => Mgr.Event.RemoveR<T1, T2>(func, name, id);
                evtDict.Add(key, act);
                clearEvt += act;
            }
        }

        protected void RemoveEvent(string name, EntityId id = default)
        {
            if (evtDict.Remove((name, id), out var obj))
                ((Action)obj)();


        }

        #endregion


        #region 统计

        public bool HasOrGetSSValue(string idName, CalcType type, out double value)
        {
            if (statsDict.TryGetValue((idName, (int)type), out var result))
            {
                value = result;
                return true;
            }
            value = -1;
            return false;
        }

        protected bool AddStats(string name, CalcType type, Func<double> callback)
        {
            var statKey = (name, type);
            if (statsSet.Contains(statKey)) return false;
            statsSet.Add(statKey);
            statsList.Add((name, type, callback));
            return true;
        }

        protected void ClearStats()
        {
            statsList.Clear();
            statsSet.Clear();
            bool isStacked = (IsStacked() && IsStatsStacked());
            foreach (var kvp in statsDict.Keys)
                owner.statsC.ClearStatsCalc(this, (CalcType)kvp.Item2, kvp.Item1, isStacked);
            statsDict.Clear();
        }

        #endregion


        #region 状态

        public bool HasOrGetSEValue(int id, out bool st)
        {
            return st = stateDict.TryGetValue(id, out _);
        }


        protected bool AddState(int id, Func<bool> callback)
        {
            if (stateSet.Contains(id)) return false;
            stateSet.Add(id);
            stateList.Add((id, callback));
            return true;
        }

        protected void ClearState()
        {
            stateList.Clear();
            stateSet.Clear();
            foreach (var kvp in stateDict)
                if (kvp.Value) owner.stateC.UpdateState(kvp.Key, -1);
            stateDict.Clear();
        }

        #endregion
    }
}