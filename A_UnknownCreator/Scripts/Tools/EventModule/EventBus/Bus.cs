using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    /// <summary>
    /// 无返回值事件总线。
    /// 全局事件：Bus<TEvent> + global List，不查字典。
    /// 实体事件：Bus<TEvent> + EntityId，才查 Dictionary。
    /// 执行顺序：priority 高的先执行；相同 priority 按添加顺序执行。
    /// </summary>
    public static class Bus<TEvent> where TEvent : IBusEvent
    {
        private sealed class Listener
        {
            public EntityId id;
            public bool isEntity;
            public Action<TEvent> action;
            public int priority;
            public bool once;
        }

        private sealed class Control : IBusControl
        {
            public void ClearAll()
            {
                Bus<TEvent>.ClearAll();
            }

            public int ListenerCount => Bus<TEvent>.GetTotalListenerCount();

            public string DebugName => $"Bus<{typeof(TEvent).Name}>";
        }

        private static readonly List<Listener> globalListeners = new();
        private static readonly Dictionary<EntityId, List<Listener>> entityListeners = new();

#if UNITY_EDITOR
        private static int publishingDepth;
#endif

        static Bus()
        {
            EventBus.Register(new Control());
        }

        // =========================
        // Global
        // =========================

        public static EventHandle Add(
            Action<TEvent> action,
            int priority = 0,
            bool allowDuplicate = false)
        {
            if (action == null)
                return new EventHandle(null);

#if UNITY_EDITOR
            WarnIfModifyWhilePublishing("Subscribe");
#endif

            if (!allowDuplicate)
                Remove(action);

            Listener listener = new Listener
            {
                action = action,
                priority = priority,
                once = false,
                isEntity = false
            };

            InsertByPriority(globalListeners, listener);

            return new EventHandle(() =>
            {
                RemoveListener(listener);
            });
        }

        public static EventHandle AddOnce(
            Action<TEvent> action,
            int priority = 0,
            bool allowDuplicate = false)
        {
            if (action == null)
                return new EventHandle(null);

#if UNITY_EDITOR
            WarnIfModifyWhilePublishing("SubscribeOnce");
#endif

            if (!allowDuplicate)
                Remove(action);

            Listener listener = new Listener
            {
                action = action,
                priority = priority,
                once = true,
                isEntity = false
            };

            InsertByPriority(globalListeners, listener);

            return new EventHandle(() =>
            {
                RemoveListener(listener);
            });
        }

        public static void Remove(Action<TEvent> action)
        {
            if (action == null)
                return;

#if UNITY_EDITOR
            WarnIfModifyWhilePublishing("Unsubscribe");
#endif

            RemoveActionFromList(globalListeners, action);
        }

        public static void Send(TEvent eventData)
        {
            if (EventBus.Interrupt)
                return;

            PublishList(eventData, globalListeners);
        }

        public static bool HasListener()
        {
            return globalListeners.Count > 0;
        }

        public static int GetListenerCount()
        {
            return globalListeners.Count;
        }

        public static void Clear()
        {
#if UNITY_EDITOR
            WarnIfModifyWhilePublishing("Clear");
#endif

            globalListeners.Clear();
        }

        // =========================
        // Entity
        // =========================

        public static EventHandle AddEntity(
            EntityId id,
            Action<TEvent> action,
            int priority = 0,
            bool allowDuplicate = false)
        {
            if (action == null)
                return new EventHandle(null);

#if UNITY_EDITOR
            WarnIfModifyWhilePublishing("SubscribeEntity");
#endif

            if (!allowDuplicate)
                RemoveEntity(id, action);

            Listener listener = new Listener
            {
                id = id,
                action = action,
                priority = priority,
                once = false,
                isEntity = true
            };

            List<Listener> list = GetOrCreateEntityList(id);
            InsertByPriority(list, listener);

            return new EventHandle(() =>
            {
                RemoveListener(listener);
            });
        }

        public static EventHandle AddEntityOnce(
            EntityId id,
            Action<TEvent> action,
            int priority = 0,
            bool allowDuplicate = false)
        {
            if (action == null)
                return new EventHandle(null);

#if UNITY_EDITOR
            WarnIfModifyWhilePublishing("SubscribeEntityOnce");
#endif

            if (!allowDuplicate)
                RemoveEntity(id, action);

            Listener listener = new Listener
            {
                id = id,
                action = action,
                priority = priority,
                once = true,
                isEntity = true
            };

            List<Listener> list = GetOrCreateEntityList(id);
            InsertByPriority(list, listener);

            return new EventHandle(() =>
            {
                RemoveListener(listener);
            });
        }

        public static void RemoveEntity(EntityId id, Action<TEvent> action)
        {
            if (action == null)
                return;

#if UNITY_EDITOR
            WarnIfModifyWhilePublishing("UnsubscribeEntity");
#endif

            if (!entityListeners.TryGetValue(id, out List<Listener> list))
                return;

            RemoveActionFromList(list, action);

            if (list.Count == 0)
                entityListeners.Remove(id);
        }

        public static void SendEntity(EntityId id, TEvent eventData)
        {
            if (EventBus.Interrupt)
                return;

            if (!entityListeners.TryGetValue(id, out List<Listener> list))
                return;

            PublishList(eventData, list);

            if (list.Count == 0)
                entityListeners.Remove(id);
        }

        public static bool HasEntityListener(EntityId id)
        {
            return entityListeners.TryGetValue(id, out List<Listener> list) && list.Count > 0;
        }

        public static int GetEntityListenerCount(EntityId id)
        {
            return entityListeners.TryGetValue(id, out List<Listener> list) ? list.Count : 0;
        }

        public static void ClearEntity(EntityId id)
        {
#if UNITY_EDITOR
            WarnIfModifyWhilePublishing("ClearEntity");
#endif

            entityListeners.Remove(id);
        }

        // =========================
        // All
        // =========================

        public static void ClearAll()
        {
#if UNITY_EDITOR
            WarnIfModifyWhilePublishing("ClearAll");
#endif

            globalListeners.Clear();
            entityListeners.Clear();
        }

        private static List<Listener> GetOrCreateEntityList(EntityId id)
        {
            if (!entityListeners.TryGetValue(id, out List<Listener> list))
            {
                list = new List<Listener>();
                entityListeners.Add(id, list);
            }

            return list;
        }

        private static void PublishList(TEvent eventData, List<Listener> list)
        {
            if (list == null || list.Count == 0)
                return;

#if UNITY_EDITOR
            publishingDepth++;
#endif

            try
            {
                for (int i = 0; i < list.Count;)
                {
                    Listener listener = list[i];

                    if (listener == null || listener.action == null)
                    {
                        list.RemoveAt(i);
                        continue;
                    }

                    try
                    {
                        listener.action.Invoke(eventData);
                    }
                    catch (Exception e)
                    {
                        UCMDebug.LogException(e);
                    }

                    if (listener.once)
                    {
                        list.RemoveAt(i);
                        continue;
                    }

                    i++;
                }
            }
            finally
            {
#if UNITY_EDITOR
                publishingDepth--;
#endif
            }
        }

        private static void RemoveListener(Listener listener)
        {
            if (listener == null)
                return;

#if UNITY_EDITOR
            WarnIfModifyWhilePublishing("RemoveListener");
#endif

            if (!listener.isEntity)
            {
                globalListeners.Remove(listener);
                return;
            }

            if (!entityListeners.TryGetValue(listener.id, out List<Listener> list))
                return;

            list.Remove(listener);

            if (list.Count == 0)
                entityListeners.Remove(listener.id);
        }

        private static void RemoveActionFromList(List<Listener> list, Action<TEvent> action)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Listener listener = list[i];

                if (listener == null)
                    continue;

                if (Delegate.Equals(listener.action, action))
                    list.RemoveAt(i);
            }
        }

        private static void InsertByPriority(List<Listener> list, Listener listener)
        {
            int i = list.Count - 1;

            while (i >= 0 && list[i].priority < listener.priority)
            {
                i--;
            }

            list.Insert(i + 1, listener);
        }

        private static int GetTotalListenerCount()
        {
            int count = globalListeners.Count;

            foreach (var pair in entityListeners)
            {
                count += pair.Value.Count;
            }

            return count;
        }

#if UNITY_EDITOR
        private static void WarnIfModifyWhilePublishing(string operation)
        {
            if (publishingDepth <= 0)
                return;

            UCMDebug.LogWarning(
                $"Bus<{typeof(TEvent).Name}> 正在 Publish 时执行了 {operation}。当前版本没有延迟操作队列，可能导致监听顺序或索引异常。"
            );
        }
#endif
    }
}
