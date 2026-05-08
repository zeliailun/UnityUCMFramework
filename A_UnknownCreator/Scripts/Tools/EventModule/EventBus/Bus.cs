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
    ///
    /// 发送期间允许 Add / Remove / Clear：
    /// - Remove / Once：当前位置置 null，占位，不改变当前 List 索引。
    /// - Add：进入 pendingAdds，本轮 Send 不执行，最外层 Send 结束后按 priority 插入。
    /// - Clear：当前已存在监听置 null，pendingAdds 中对应监听移除。
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

        private struct PendingAdd
        {
            public Listener listener;
        }

        private static readonly List<Listener> globalListeners = new();
        private static readonly Dictionary<EntityId, List<Listener>> entityListeners = new();

        private static int publishingDepth;
        private static bool globalDirty;
        private static readonly HashSet<EntityId> dirtyEntityIds = new();
        private static readonly List<EntityId> emptyEntityIds = new();
        private static readonly List<PendingAdd> pendingAdds = new();

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

            if (!allowDuplicate)
                Remove(action);

            Listener listener = new Listener
            {
                action = action,
                priority = priority,
                once = false,
                isEntity = false
            };

            AddListener(listener);

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

            if (!allowDuplicate)
                Remove(action);

            Listener listener = new Listener
            {
                action = action,
                priority = priority,
                once = true,
                isEntity = false
            };

            AddListener(listener);

            return new EventHandle(() =>
            {
                RemoveListener(listener);
            });
        }

        public static void Remove(Action<TEvent> action)
        {
            if (action == null)
                return;

            RemoveActionFromPendingAdds(action, false, default);
            RemoveActionFromList(globalListeners, action, false, default);
        }

        public static void Send(TEvent eventData)
        {
            if (EventBus.Interrupt)
                return;

            PublishList(eventData, globalListeners);
        }

        public static bool HasListener()
        {
            return HasAliveListener(globalListeners);
        }

        public static int GetListenerCount()
        {
            return CountAliveListeners(globalListeners);
        }

        public static void Clear()
        {
            if (publishingDepth > 0)
            {
                MarkAllRemoveLater(globalListeners, false, default);
                RemovePendingAddsByKey(false, default);
                return;
            }

            globalListeners.Clear();
            RemovePendingAddsByKey(false, default);
            globalDirty = false;
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

            AddListener(listener);

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

            AddListener(listener);

            return new EventHandle(() =>
            {
                RemoveListener(listener);
            });
        }

        public static void RemoveEntity(EntityId id, Action<TEvent> action)
        {
            if (action == null)
                return;

            RemoveActionFromPendingAdds(action, true, id);

            if (!entityListeners.TryGetValue(id, out List<Listener> list))
                return;

            RemoveActionFromList(list, action, true, id);

            if (publishingDepth <= 0 && list.Count == 0)
                entityListeners.Remove(id);
        }

        public static void SendEntity(EntityId id, TEvent eventData)
        {
            if (EventBus.Interrupt)
                return;

            if (!entityListeners.TryGetValue(id, out List<Listener> list))
                return;

            PublishList(eventData, list);
        }

        public static bool HasEntityListener(EntityId id)
        {
            return entityListeners.TryGetValue(id, out List<Listener> list) && HasAliveListener(list);
        }

        public static int GetEntityListenerCount(EntityId id)
        {
            return entityListeners.TryGetValue(id, out List<Listener> list) ? CountAliveListeners(list) : 0;
        }

        public static void ClearEntity(EntityId id)
        {
            RemovePendingAddsByKey(true, id);

            if (!entityListeners.TryGetValue(id, out List<Listener> list))
                return;

            if (publishingDepth > 0)
            {
                MarkAllRemoveLater(list, true, id);
                return;
            }

            entityListeners.Remove(id);
        }

        // =========================
        // All
        // =========================

        public static void ClearAll()
        {
            if (publishingDepth > 0)
            {
                MarkAllRemoveLater(globalListeners, false, default);

                foreach (var pair in entityListeners)
                {
                    MarkAllRemoveLater(pair.Value, true, pair.Key);
                }

                pendingAdds.Clear();
                return;
            }

            globalListeners.Clear();
            entityListeners.Clear();

            globalDirty = false;
            dirtyEntityIds.Clear();
            emptyEntityIds.Clear();
            pendingAdds.Clear();
        }

        private static void AddListener(Listener listener)
        {
            if (listener == null || listener.action == null)
                return;

            if (publishingDepth > 0)
            {
                pendingAdds.Add(new PendingAdd
                {
                    listener = listener
                });

                return;
            }

            AddListenerDirect(listener);
        }

        private static void AddListenerDirect(Listener listener)
        {
            if (listener == null || listener.action == null)
                return;

            if (!listener.isEntity)
            {
                InsertByPriority(globalListeners, listener);
                return;
            }

            List<Listener> list = GetOrCreateEntityList(listener.id);
            InsertByPriority(list, listener);
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

            BeginPublish();

            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Listener listener = list[i];

                    if (listener == null)
                        continue;

                    Action<TEvent> action = listener.action;

                    if (action == null)
                    {
                        MarkRemoveLater(list, i, listener);
                        continue;
                    }

                    // Once 先标记删除，再执行。
                    // 这样回调里递归 Send 同事件时，Once 不会重复触发。
                    if (listener.once)
                    {
                        MarkRemoveLater(list, i, listener);
                    }

                    try
                    {
                        action.Invoke(eventData);
                    }
                    catch (Exception e)
                    {
                        UCMDebug.LogException(e);
                    }
                }
            }
            finally
            {
                EndPublish();
            }
        }

        private static void BeginPublish()
        {
            publishingDepth++;
        }

        private static void EndPublish()
        {
            publishingDepth--;

            if (publishingDepth <= 0)
            {
                publishingDepth = 0;
                FlushPendingChanges();
            }
        }

        private static void FlushPendingChanges()
        {
            if (globalDirty)
            {
                RemoveNullSlots(globalListeners);
                globalDirty = false;
            }

            if (dirtyEntityIds.Count > 0)
            {
                foreach (EntityId id in dirtyEntityIds)
                {
                    if (!entityListeners.TryGetValue(id, out List<Listener> list))
                        continue;

                    RemoveNullSlots(list);

                    if (list.Count == 0)
                        emptyEntityIds.Add(id);
                }

                dirtyEntityIds.Clear();

                for (int i = 0; i < emptyEntityIds.Count; i++)
                {
                    entityListeners.Remove(emptyEntityIds[i]);
                }

                emptyEntityIds.Clear();
            }

            if (pendingAdds.Count > 0)
            {
                for (int i = 0; i < pendingAdds.Count; i++)
                {
                    Listener listener = pendingAdds[i].listener;

                    if (listener != null && listener.action != null)
                        AddListenerDirect(listener);
                }

                pendingAdds.Clear();
            }
        }

        private static void RemoveListener(Listener listener)
        {
            if (listener == null)
                return;

            RemovePendingListener(listener);

            if (!listener.isEntity)
            {
                RemoveListenerFromList(globalListeners, listener, false, default);
                return;
            }

            if (!entityListeners.TryGetValue(listener.id, out List<Listener> list))
                return;

            RemoveListenerFromList(list, listener, true, listener.id);

            if (publishingDepth <= 0 && list.Count == 0)
                entityListeners.Remove(listener.id);
        }

        private static void RemoveListenerFromList(
            List<Listener> list,
            Listener listener,
            bool isEntity,
            EntityId id)
        {
            if (list == null || listener == null)
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(list[i], listener))
                    continue;

                if (publishingDepth > 0)
                {
                    MarkRemoveLater(list, i, listener);
                }
                else
                {
                    list.RemoveAt(i);
                }

                break;
            }
        }

        private static void RemoveActionFromList(
            List<Listener> list,
            Action<TEvent> action,
            bool isEntity,
            EntityId id)
        {
            if (list == null || action == null)
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                Listener listener = list[i];

                if (listener == null)
                    continue;

                if (!Delegate.Equals(listener.action, action))
                    continue;

                if (publishingDepth > 0)
                {
                    MarkRemoveLater(list, i, listener);
                }
                else
                {
                    list.RemoveAt(i);
                }
            }
        }

        private static void MarkRemoveLater(List<Listener> list, int index, Listener listener)
        {
            if (list == null || index < 0 || index >= list.Count)
                return;

            if (list[index] == null)
                return;

            list[index] = null;

            if (listener != null && listener.isEntity)
            {
                dirtyEntityIds.Add(listener.id);
            }
            else
            {
                globalDirty = true;
            }
        }

        private static void MarkAllRemoveLater(List<Listener> list, bool isEntity, EntityId id)
        {
            if (list == null || list.Count == 0)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                    list[i] = null;
            }

            if (isEntity)
                dirtyEntityIds.Add(id);
            else
                globalDirty = true;
        }

        private static void RemoveNullSlots(List<Listener> list)
        {
            if (list == null)
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == null)
                    list.RemoveAt(i);
            }
        }

        private static bool RemovePendingListener(Listener listener)
        {
            bool removed = false;

            for (int i = pendingAdds.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(pendingAdds[i].listener, listener))
                    continue;

                pendingAdds.RemoveAt(i);
                removed = true;
            }

            return removed;
        }

        private static void RemoveActionFromPendingAdds(
            Action<TEvent> action,
            bool isEntity,
            EntityId id)
        {
            if (action == null || pendingAdds.Count == 0)
                return;

            for (int i = pendingAdds.Count - 1; i >= 0; i--)
            {
                Listener listener = pendingAdds[i].listener;

                if (listener == null)
                {
                    pendingAdds.RemoveAt(i);
                    continue;
                }

                if (listener.isEntity != isEntity)
                    continue;

                if (isEntity && !EqualityComparer<EntityId>.Default.Equals(listener.id, id))
                    continue;

                if (Delegate.Equals(listener.action, action))
                    pendingAdds.RemoveAt(i);
            }
        }

        private static void RemovePendingAddsByKey(bool isEntity, EntityId id)
        {
            for (int i = pendingAdds.Count - 1; i >= 0; i--)
            {
                Listener listener = pendingAdds[i].listener;

                if (listener == null)
                {
                    pendingAdds.RemoveAt(i);
                    continue;
                }

                if (listener.isEntity != isEntity)
                    continue;

                if (isEntity && !EqualityComparer<EntityId>.Default.Equals(listener.id, id))
                    continue;

                pendingAdds.RemoveAt(i);
            }
        }

        private static void InsertByPriority(List<Listener> list, Listener listener)
        {
            int i = list.Count - 1;

            while (i >= 0)
            {
                Listener current = list[i];

                if (current == null)
                {
                    i--;
                    continue;
                }

                if (current.priority >= listener.priority)
                    break;

                i--;
            }

            list.Insert(i + 1, listener);
        }

        private static bool HasAliveListener(List<Listener> list)
        {
            if (list == null)
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                Listener listener = list[i];

                if (listener != null && listener.action != null)
                    return true;
            }

            return false;
        }

        private static int CountAliveListeners(List<Listener> list)
        {
            if (list == null)
                return 0;

            int count = 0;

            for (int i = 0; i < list.Count; i++)
            {
                Listener listener = list[i];

                if (listener != null && listener.action != null)
                    count++;
            }

            return count;
        }

        private static int GetTotalListenerCount()
        {
            int count = CountAliveListeners(globalListeners);

            foreach (var pair in entityListeners)
            {
                count += CountAliveListeners(pair.Value);
            }

            return count;
        }
    }
}
