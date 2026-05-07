using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    /// <summary>
    /// 外部统一入口。
    /// 全局事件不需要 EntityId，也不查字典。
    /// 实体事件使用 Entity 版本，才会走 EntityId 字典。
    /// </summary>
    public static class GameEvtBus
    {
        // =========================
        // 普通事件 - Global
        // =========================

        public static EventHandle Add<TEvent>(
            Action<TEvent> action,
            int priority = 0,
            bool allowDuplicate = false) where TEvent : IBusEvent
        {
            return Bus<TEvent>.Add(action, priority, allowDuplicate);
        }

        public static EventHandle AddOnce<TEvent>(
            Action<TEvent> action,
            int priority = 0,
            bool allowDuplicate = false) where TEvent : IBusEvent
        {
            return Bus<TEvent>.AddOnce(action, priority, allowDuplicate);
        }

        public static void Remove<TEvent>(
            Action<TEvent> action) where TEvent : IBusEvent
        {
            Bus<TEvent>.Remove(action);
        }

        public static void Send<TEvent>(
            TEvent eventData) where TEvent : IBusEvent
        {
            Bus<TEvent>.Send(eventData);
        }

        public static bool HasListener<TEvent>() where TEvent : IBusEvent
        {
            return Bus<TEvent>.HasListener();
        }

        public static int GetListenerCount<TEvent>() where TEvent : IBusEvent
        {
            return Bus<TEvent>.GetListenerCount();
        }

        public static void Clear<TEvent>() where TEvent : IBusEvent
        {
            Bus<TEvent>.Clear();
        }

        // =========================
        // 普通事件 - Entity
        // =========================

        public static EventHandle AddEntity<TEvent>(
            EntityId id,
            Action<TEvent> action,
            int priority = 0,
            bool allowDuplicate = false) where TEvent : IBusEvent
        {
            return Bus<TEvent>.AddEntity(id, action, priority, allowDuplicate);
        }

        public static EventHandle AddEntityOnce<TEvent>(
            EntityId id,
            Action<TEvent> action,
            int priority = 0,
            bool allowDuplicate = false) where TEvent : IBusEvent
        {
            return Bus<TEvent>.AddEntityOnce(id, action, priority, allowDuplicate);
        }

        public static void RemoveEntity<TEvent>(
            EntityId id,
            Action<TEvent> action) where TEvent : IBusEvent
        {
            Bus<TEvent>.RemoveEntity(id, action);
        }

        public static void SendEntity<TEvent>(
            EntityId id,
            TEvent eventData) where TEvent : IBusEvent
        {
            Bus<TEvent>.SendEntity(id, eventData);
        }

        public static bool HasEntityListener<TEvent>(
            EntityId id) where TEvent : IBusEvent
        {
            return Bus<TEvent>.HasEntityListener(id);
        }

        public static int GetEntityListenerCount<TEvent>(
            EntityId id) where TEvent : IBusEvent
        {
            return Bus<TEvent>.GetEntityListenerCount(id);
        }

        public static void ClearEntity<TEvent>(
            EntityId id) where TEvent : IBusEvent
        {
            Bus<TEvent>.ClearEntity(id);
        }

        // =========================
        // 无参数查询事件 Func<TResult> - Global
        // =========================

        public static EventHandle AddQuery<TResult>(
            Func<TResult> func,
            int priority = 1000,
            bool allowDuplicate = false)
        {
            return QueryBus<TResult>.Add(func, priority, allowDuplicate);
        }

        public static void RemoveQuery<TResult>(
            Func<TResult> func)
        {
            QueryBus<TResult>.Remove(func);
        }

        public static TResult Query<TResult>()
        {
            return QueryBus<TResult>.Query();
        }

        public static bool TryQuery<TResult>(
            out TResult result)
        {
            return QueryBus<TResult>.TryQuery(out result);
        }

        public static List<TResult> QueryAll<TResult>()
        {
            return QueryBus<TResult>.QueryAll();
        }

        public static void ClearQuery<TResult>()
        {
            QueryBus<TResult>.Clear();
        }

        // =========================
        // 无参数查询事件 Func<TResult> - Entity
        // =========================

        public static EventHandle AddEntityQuery<TResult>(
            EntityId id,
            Func<TResult> func,
            int priority = 1000,
            bool allowDuplicate = false)
        {
            return QueryBus<TResult>.AddEntity(id, func, priority, allowDuplicate);
        }

        public static void RemoveEntityQuery<TResult>(
            EntityId id,
            Func<TResult> func)
        {
            QueryBus<TResult>.RemoveEntity(id, func);
        }

        public static TResult QueryEntity<TResult>(
            EntityId id)
        {
            return QueryBus<TResult>.QueryEntity(id);
        }

        public static bool TryQueryEntity<TResult>(
            EntityId id,
            out TResult result)
        {
            return QueryBus<TResult>.TryQueryEntity(id, out result);
        }

        public static List<TResult> QueryAllEntity<TResult>(
            EntityId id)
        {
            return QueryBus<TResult>.QueryAllEntity(id);
        }

        public static void ClearEntityQuery<TResult>(
            EntityId id)
        {
            QueryBus<TResult>.ClearEntity(id);
        }

        // =========================
        // 带参数查询事件 Func<TQuery, TResult> - Global
        // =========================

        public static EventHandle AddQuery<TQuery, TResult>(
            Func<TQuery, TResult> func,
            int priority = 1000,
            bool allowDuplicate = false)
        {
            return QueryBus<TQuery, TResult>.Add(func, priority, allowDuplicate);
        }

        public static void RemoveQuery<TQuery, TResult>(
            Func<TQuery, TResult> func)
        {
            QueryBus<TQuery, TResult>.Remove(func);
        }

        public static TResult Query<TQuery, TResult>(
            TQuery query)
        {
            return QueryBus<TQuery, TResult>.Query(query);
        }

        public static bool TryQuery<TQuery, TResult>(
            TQuery query,
            out TResult result)
        {
            return QueryBus<TQuery, TResult>.TryQuery(query, out result);
        }

        public static List<TResult> QueryAll<TQuery, TResult>(
            TQuery query)
        {
            return QueryBus<TQuery, TResult>.QueryAll(query);
        }

        public static void ClearQuery<TQuery, TResult>()
        {
            QueryBus<TQuery, TResult>.Clear();
        }

        // =========================
        // 带参数查询事件 Func<TQuery, TResult> - Entity
        // =========================

        public static EventHandle AddEntityQuery<TQuery, TResult>(
            EntityId id,
            Func<TQuery, TResult> func,
            int priority = 1000,
            bool allowDuplicate = false)
        {
            return QueryBus<TQuery, TResult>.AddEntity(id, func, priority, allowDuplicate);
        }

        public static void RemoveEntityQuery<TQuery, TResult>(
            EntityId id,
            Func<TQuery, TResult> func)
        {
            QueryBus<TQuery, TResult>.RemoveEntity(id, func);
        }

        public static TResult QueryEntity<TQuery, TResult>(
            EntityId id,
            TQuery query)
        {
            return QueryBus<TQuery, TResult>.QueryEntity(id, query);
        }

        public static bool TryQueryEntity<TQuery, TResult>(
            EntityId id,
            TQuery query,
            out TResult result)
        {
            return QueryBus<TQuery, TResult>.TryQueryEntity(id, query, out result);
        }

        public static List<TResult> QueryAllEntity<TQuery, TResult>(
            EntityId id,
            TQuery query)
        {
            return QueryBus<TQuery, TResult>.QueryAllEntity(id, query);
        }

        public static void ClearEntityQuery<TQuery, TResult>(
            EntityId id)
        {
            QueryBus<TQuery, TResult>.ClearEntity(id);
        }

        // =========================
        // 全局
        // =========================

        public static bool Interrupt
        {
            get => EventBus.Interrupt;
            set => EventBus.Interrupt = value;
        }

        public static void ClearAll()
        {
            EventBus.ClearAll();
        }

#if UNITY_EDITOR
        public static void DebugDump()
        {
            EventBus.DebugDump();
        }
#endif
    }
}
