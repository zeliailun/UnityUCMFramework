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

        public void ChangeThink(double t)
        {
            delay = t;
        }

        public void IncreaseStack()
        => ++stackCount;

        public void DecreaseStack()
        => --stackCount;


        public double GetRemainingTime() => duration;

        public double GetElapsedTime() => origDuration - duration;


        public void ClearEvent()
        {
            clearEvt?.Invoke();

            clearEvt = null;
            evtDict.Clear();
        }

        protected void ClearBusEvent()
        {
            foreach (var pair in busEvtDict)
            {
                pair.Value?.Dispose();
            }

            busEvtDict.Clear();
        }


        public void RemoveEvent(string name, EntityId id = default)
        {
            if (evtDict.Remove((name, id), out var obj))
                ((Action)obj)();
        }

        public void RemoveBusFuncEvent<TResult>(EntityId id)
        {
            RemoveBusEventInternal(BusEventKey.Query<TResult>(id));
        }


        public void RemoveBusFuncEvent<TQuery, TResult>()
        {
            RemoveBusEventInternal(BusEventKey.QueryWithParam<TQuery, TResult>());
        }


        public void DestroySelf()
        {
            owner?.buffC?.RemoveBuff(this);
        }





        #region 运动控制器

        public void ApplyMotionController()
        {
            if (!isInterruptMotion) return;

            var allBuffs = GameEvtBus.QueryAllEntity<EvtBuffMotionInterrupted>(owner.entID);

            bool hasHigherPriority = false;

            if (allBuffs != null)
            {
                foreach (var evts in allBuffs)
                {
                    if (ReferenceEquals(evts.buff, this)) continue;

                    int existingPriority = evts.buff.GetMotionPriority();
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
                    foreach (var evts in allBuffs)
                    {
                        if (ReferenceEquals(evts.buff, this)) continue;

                        if (evts.buff.GetMotionPriority() < this.GetMotionPriority())
                        {
                            evts.buff.RemoveMotionController();
                        }
                    }
                }

                // 注册自己
                isInterruptMotion = false;
                GameEvtBus.AddEntityQuery<EvtBuffMotionInterrupted>(owner.entID, GetSelf);
            }
        }

        public void RemoveMotionController()
        {
            if (isInterruptMotion) return;
            isInterruptMotion = true;
            GameEvtBus.RemoveEntityQuery<EvtBuffMotionInterrupted>(owner.entID, GetSelf);
            OnMotionControllerInterrupted();
        }




        private EvtBuffMotionInterrupted GetSelf() => new(this);


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

        protected void AddFuncEvent<T>(string name, Func<T> func, EntityId id = default, int priority = 0)
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

        protected void AddFuncEvent<T1, T2>(string name, Func<T1, T2> func, EntityId id = default, int priority = 0)
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




        // ============================================================
        // BUS Action Event - Global
        // 对应 GameEvtBus.Add<TEvent>
        // ============================================================

        protected void AddBusActionEvent<TEvent>(
            Action<TEvent> action,
            int priority = 0,
            bool allowDuplicate = false)
            where TEvent : IBusEvent
        {
            var key = BusEventKey.Action<TEvent>();

            if (busEvtDict.TryGetValue(key, out _))
                return;

            EventHandle handle = GameEvtBus.Add(
                action,
                priority,
                allowDuplicate);

            busEvtDict.Add(key, handle);
        }

        protected void RemoveBusActionEvent<TEvent>()
            where TEvent : IBusEvent
        {
            RemoveBusEventInternal(BusEventKey.Action<TEvent>());
        }

        // ============================================================
        // BUS Action Event - Entity
        // 对应 GameEvtBus.AddEntity<TEvent>
        // ============================================================

        protected void AddBusActionEvent<TEvent>(
            EntityId id,
            Action<TEvent> action,
            int priority = 0,
            bool allowDuplicate = false)
            where TEvent : IBusEvent
        {
            var key = BusEventKey.Action<TEvent>(id);

            if (busEvtDict.TryGetValue(key, out _))
                return;

            EventHandle handle = GameEvtBus.AddEntity(
                id,
                action,
                priority,
                allowDuplicate);

            busEvtDict.Add(key, handle);
        }

        protected void RemoveBusActionEvent<TEvent>(EntityId id)
            where TEvent : IBusEvent
        {
            RemoveBusEventInternal(BusEventKey.Action<TEvent>(id));
        }

        // ============================================================
        // BUS Func Event - Global
        // 对应 GameEvtBus.AddQuery<TResult>
        // ============================================================

        protected void AddBusFuncEvent<TResult>(
            Func<TResult> func,
            int priority = 0,
            bool allowDuplicate = false)
        {
            var key = BusEventKey.Query<TResult>();

            if (busEvtDict.TryGetValue(key, out _))
                return;

            EventHandle handle = GameEvtBus.AddQuery(
                func,
                priority,
                allowDuplicate);

            busEvtDict.Add(key, handle);
        }

        protected void RemoveBusFuncEvent<TResult>()
        {
            RemoveBusEventInternal(BusEventKey.Query<TResult>());
        }

        // ============================================================
        // BUS Func Event - Entity
        // 对应 GameEvtBus.AddEntityQuery<TResult>
        // ============================================================

        protected void AddBusFuncEvent<TResult>(
            EntityId id,
            Func<TResult> func,
            int priority = 0,
            bool allowDuplicate = false)
        {
            var key = BusEventKey.Query<TResult>(id);

            if (busEvtDict.TryGetValue(key, out _))
                return;

            EventHandle handle = GameEvtBus.AddEntityQuery(
                id,
                func,
                priority,
                allowDuplicate);

            busEvtDict.Add(key, handle);
        }


        // ============================================================
        // BUS Func<TQuery, TResult> Event - Global
        // 对应 GameEvtBus.AddQuery<TQuery, TResult>
        // ============================================================

        protected void AddBusFuncEvent<TQuery, TResult>(
            Func<TQuery, TResult> func,
            int priority = 0,
            bool allowDuplicate = false) where TQuery : IBusQuery
        {
            var key = BusEventKey.QueryWithParam<TQuery, TResult>();

            if (busEvtDict.TryGetValue(key, out _))
                return;

            EventHandle handle = GameEvtBus.AddQuery(
                func,
                priority,
                allowDuplicate);

            busEvtDict.Add(key, handle);
        }


        // ============================================================
        // BUS Func<TQuery, TResult> Event - Entity
        // 对应 GameEvtBus.AddEntityQuery<TQuery, TResult>
        // ============================================================

        protected void AddBusFuncEvent<TQuery, TResult>(
            EntityId id,
            Func<TQuery, TResult> func,
            int priority = 0,
            bool allowDuplicate = false) where TQuery : IBusQuery
        {
            var key = BusEventKey.QueryWithParam<TQuery, TResult>(id);

            if (busEvtDict.TryGetValue(key, out _))
                return;

            EventHandle handle = GameEvtBus.AddEntityQuery(
                id,
                func,
                priority,
                allowDuplicate);

            busEvtDict.Add(key, handle);
        }

        protected void RemoveBusFuncEvent<TQuery, TResult>(EntityId id)
        {
            RemoveBusEventInternal(BusEventKey.QueryWithParam<TQuery, TResult>(id));
        }

        // ============================================================
        // BUS Remove / Clear
        // ============================================================

        private void RemoveBusEventInternal(BusEventKey key)
        {
            if (!busEvtDict.Remove(key, out EventHandle handle))
                return;

            handle?.Dispose();
        }

        protected void ClearAllEvent()
        {
            ClearEvent();
            ClearBusEvent();
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
            return stateDict.TryGetValue(id, out st);
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