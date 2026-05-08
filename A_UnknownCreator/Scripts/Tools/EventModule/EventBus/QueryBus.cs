using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    /// <summary>
    /// 无参数返回值查询总线。
    /// 全局查询：QueryBus<TResult> + global List，不查字典。
    /// 实体查询：QueryBus<TResult> + EntityId，才查 Dictionary。
    /// 查询规则：priority 高的先返回；相同 priority 取最后添加的那个。
    ///
    /// Query 期间允许 Add / Remove / Clear：
    /// - Remove：当前位置置 null，占位，不改变当前 List 索引。
    /// - Add：进入 pendingAdds，本轮 Query 不生效，最外层 Query 结束后按 priority 插入。
    /// - Clear：当前已存在监听置 null，pendingAdds 中对应监听移除。
    /// </summary>
    public static class QueryBus<TResult>
    {
        private sealed class Listener
        {
            public EntityId id;
            public bool isEntity;
            public Func<TResult> func;
            public int priority;
        }

        private sealed class Control : IBusControl
        {
            public void ClearAll()
            {
                QueryBus<TResult>.ClearAll();
            }

            public int ListenerCount => QueryBus<TResult>.GetTotalListenerCount();

            public string DebugName => $"QueryBus<{typeof(TResult).Name}>";
        }

        private struct PendingAdd
        {
            public Listener listener;
        }

        private static readonly List<Listener> globalListeners = new();
        private static readonly Dictionary<EntityId, List<Listener>> entityListeners = new();

        private static int queryingDepth;
        private static bool globalDirty;
        private static readonly HashSet<EntityId> dirtyEntityIds = new();
        private static readonly List<EntityId> emptyEntityIds = new();
        private static readonly List<PendingAdd> pendingAdds = new();

        static QueryBus()
        {
            EventBus.Register(new Control());
        }

        // =========================
        // Global
        // =========================

        public static EventHandle Add(
            Func<TResult> func,
            int priority = 1000,
            bool allowDuplicate = false)
        {
            if (func == null)
                return new EventHandle(null);

            if (!allowDuplicate)
                Remove(func);

            Listener listener = new Listener
            {
                func = func,
                priority = priority,
                isEntity = false
            };

            AddListener(listener);

            return new EventHandle(() =>
            {
                RemoveListener(listener);
            });
        }

        public static void Remove(Func<TResult> func)
        {
            if (func == null)
                return;

            RemoveFuncFromPendingAdds(func, false, default);
            RemoveFuncFromList(globalListeners, func, false, default);
        }

        public static TResult Query()
        {
            return QueryList(globalListeners);
        }

        public static bool TryQuery(out TResult result)
        {
            return TryQueryList(globalListeners, out result);
        }

        public static List<TResult> QueryAll()
        {
            return QueryAllList(globalListeners);
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
            if (queryingDepth > 0)
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
            Func<TResult> func,
            int priority = 1000,
            bool allowDuplicate = false)
        {
            if (func == null)
                return new EventHandle(null);

            if (!allowDuplicate)
                RemoveEntity(id, func);

            Listener listener = new Listener
            {
                id = id,
                func = func,
                priority = priority,
                isEntity = true
            };

            AddListener(listener);

            return new EventHandle(() =>
            {
                RemoveListener(listener);
            });
        }

        public static void RemoveEntity(EntityId id, Func<TResult> func)
        {
            if (func == null)
                return;

            RemoveFuncFromPendingAdds(func, true, id);

            if (!entityListeners.TryGetValue(id, out List<Listener> list))
                return;

            RemoveFuncFromList(list, func, true, id);

            if (queryingDepth <= 0 && list.Count == 0)
                entityListeners.Remove(id);
        }

        public static TResult QueryEntity(EntityId id)
        {
            if (!entityListeners.TryGetValue(id, out List<Listener> list))
                return default;

            return QueryList(list);
        }

        public static bool TryQueryEntity(EntityId id, out TResult result)
        {
            result = default;

            if (!entityListeners.TryGetValue(id, out List<Listener> list))
                return false;

            return TryQueryList(list, out result);
        }

        public static List<TResult> QueryAllEntity(EntityId id)
        {
            if (!entityListeners.TryGetValue(id, out List<Listener> list))
                return new List<TResult>();

            return QueryAllList(list);
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

            if (queryingDepth > 0)
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
            if (queryingDepth > 0)
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
            if (listener == null || listener.func == null)
                return;

            if (queryingDepth > 0)
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
            if (listener == null || listener.func == null)
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

        private static TResult QueryList(List<Listener> list)
        {
            if (EventBus.Interrupt)
                return default;

            if (list == null || list.Count == 0)
                return default;

            TResult returnValue = default;
            bool hasValue = false;

            BeginQuery();

            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Listener listener = list[i];

                    if (listener == null)
                        continue;

                    Func<TResult> func = listener.func;

                    if (func == null)
                    {
                        MarkRemoveLater(list, i, listener);
                        continue;
                    }

                    try
                    {
                        returnValue = func.Invoke();
                        hasValue = true;
                        break;
                    }
                    catch (Exception e)
                    {
                        UCMDebug.LogException(e);
                        returnValue = default;
                        hasValue = false;
                        break;
                    }
                }
            }
            finally
            {
                EndQuery();
            }

            return hasValue ? returnValue : default;
        }

        private static bool TryQueryList(List<Listener> list, out TResult result)
        {
            result = default;

            if (EventBus.Interrupt)
                return false;

            if (list == null || list.Count == 0)
                return false;

            TResult returnValue = default;
            bool hasValue = false;

            BeginQuery();

            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Listener listener = list[i];

                    if (listener == null)
                        continue;

                    Func<TResult> func = listener.func;

                    if (func == null)
                    {
                        MarkRemoveLater(list, i, listener);
                        continue;
                    }

                    try
                    {
                        returnValue = func.Invoke();
                        hasValue = true;
                        break;
                    }
                    catch (Exception e)
                    {
                        UCMDebug.LogException(e);
                        returnValue = default;
                        hasValue = false;
                        break;
                    }
                }
            }
            finally
            {
                EndQuery();
            }

            result = returnValue;
            return hasValue;
        }

        private static List<TResult> QueryAllList(List<Listener> list)
        {
            List<TResult> resultList = new List<TResult>();

            if (EventBus.Interrupt)
                return resultList;

            if (list == null || list.Count == 0)
                return resultList;

            BeginQuery();

            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Listener listener = list[i];

                    if (listener == null)
                        continue;

                    Func<TResult> func = listener.func;

                    if (func == null)
                    {
                        MarkRemoveLater(list, i, listener);
                        continue;
                    }

                    try
                    {
                        resultList.Add(func.Invoke());
                    }
                    catch (Exception e)
                    {
                        UCMDebug.LogException(e);
                    }
                }
            }
            finally
            {
                EndQuery();
            }

            return resultList;
        }

        private static void BeginQuery()
        {
            queryingDepth++;
        }

        private static void EndQuery()
        {
            queryingDepth--;

            if (queryingDepth <= 0)
            {
                queryingDepth = 0;
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

                    if (listener != null && listener.func != null)
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

            if (queryingDepth <= 0 && list.Count == 0)
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

                if (queryingDepth > 0)
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

        private static void RemoveFuncFromList(
            List<Listener> list,
            Func<TResult> func,
            bool isEntity,
            EntityId id)
        {
            if (list == null || func == null)
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                Listener listener = list[i];

                if (listener == null)
                    continue;

                if (!Delegate.Equals(listener.func, func))
                    continue;

                if (queryingDepth > 0)
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

        private static void RemoveFuncFromPendingAdds(
            Func<TResult> func,
            bool isEntity,
            EntityId id)
        {
            if (func == null || pendingAdds.Count == 0)
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

                if (Delegate.Equals(listener.func, func))
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

                // QueryBus 的规则：相同 priority 取最后添加的那个。
                // 所以新 listener 要插到同优先级旧 listener 前面。
                if (current.priority > listener.priority)
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

                if (listener != null && listener.func != null)
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

                if (listener != null && listener.func != null)
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

    /// <summary>
    /// 带参数返回值查询总线。
    /// 全局查询：QueryBus<TQuery, TResult> + global List，不查字典。
    /// 实体查询：QueryBus<TQuery, TResult> + EntityId，才查 Dictionary。
    /// 查询规则：priority 高的先返回；相同 priority 取最后添加的那个。
    ///
    /// Query 期间允许 Add / Remove / Clear：
    /// - Remove：当前位置置 null，占位，不改变当前 List 索引。
    /// - Add：进入 pendingAdds，本轮 Query 不生效，最外层 Query 结束后按 priority 插入。
    /// - Clear：当前已存在监听置 null，pendingAdds 中对应监听移除。
    /// </summary>
    public static class QueryBus<TQuery, TResult>
    {
        private sealed class Listener
        {
            public EntityId id;
            public bool isEntity;
            public Func<TQuery, TResult> func;
            public int priority;
        }

        private sealed class Control : IBusControl
        {
            public void ClearAll()
            {
                QueryBus<TQuery, TResult>.ClearAll();
            }

            public int ListenerCount => QueryBus<TQuery, TResult>.GetTotalListenerCount();

            public string DebugName => $"QueryBus<{typeof(TQuery).Name}, {typeof(TResult).Name}>";
        }

        private struct PendingAdd
        {
            public Listener listener;
        }

        private static readonly List<Listener> globalListeners = new();
        private static readonly Dictionary<EntityId, List<Listener>> entityListeners = new();

        private static int queryingDepth;
        private static bool globalDirty;
        private static readonly HashSet<EntityId> dirtyEntityIds = new();
        private static readonly List<EntityId> emptyEntityIds = new();
        private static readonly List<PendingAdd> pendingAdds = new();

        static QueryBus()
        {
            EventBus.Register(new Control());
        }

        // =========================
        // Global
        // =========================

        public static EventHandle Add(
            Func<TQuery, TResult> func,
            int priority = 1000,
            bool allowDuplicate = false)
        {
            if (func == null)
                return new EventHandle(null);

            if (!allowDuplicate)
                Remove(func);

            Listener listener = new Listener
            {
                func = func,
                priority = priority,
                isEntity = false
            };

            AddListener(listener);

            return new EventHandle(() =>
            {
                RemoveListener(listener);
            });
        }

        public static void Remove(Func<TQuery, TResult> func)
        {
            if (func == null)
                return;

            RemoveFuncFromPendingAdds(func, false, default);
            RemoveFuncFromList(globalListeners, func, false, default);
        }

        public static TResult Query(TQuery query)
        {
            return QueryList(globalListeners, query);
        }

        public static bool TryQuery(TQuery query, out TResult result)
        {
            return TryQueryList(globalListeners, query, out result);
        }

        public static List<TResult> QueryAll(TQuery query)
        {
            return QueryAllList(globalListeners, query);
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
            if (queryingDepth > 0)
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
            Func<TQuery, TResult> func,
            int priority = 1000,
            bool allowDuplicate = false)
        {
            if (func == null)
                return new EventHandle(null);

            if (!allowDuplicate)
                RemoveEntity(id, func);

            Listener listener = new Listener
            {
                id = id,
                func = func,
                priority = priority,
                isEntity = true
            };

            AddListener(listener);

            return new EventHandle(() =>
            {
                RemoveListener(listener);
            });
        }

        public static void RemoveEntity(EntityId id, Func<TQuery, TResult> func)
        {
            if (func == null)
                return;

            RemoveFuncFromPendingAdds(func, true, id);

            if (!entityListeners.TryGetValue(id, out List<Listener> list))
                return;

            RemoveFuncFromList(list, func, true, id);

            if (queryingDepth <= 0 && list.Count == 0)
                entityListeners.Remove(id);
        }

        public static TResult QueryEntity(EntityId id, TQuery query)
        {
            if (!entityListeners.TryGetValue(id, out List<Listener> list))
                return default;

            return QueryList(list, query);
        }

        public static bool TryQueryEntity(EntityId id, TQuery query, out TResult result)
        {
            result = default;

            if (!entityListeners.TryGetValue(id, out List<Listener> list))
                return false;

            return TryQueryList(list, query, out result);
        }

        public static List<TResult> QueryAllEntity(EntityId id, TQuery query)
        {
            if (!entityListeners.TryGetValue(id, out List<Listener> list))
                return new List<TResult>();

            return QueryAllList(list, query);
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

            if (queryingDepth > 0)
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
            if (queryingDepth > 0)
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
            if (listener == null || listener.func == null)
                return;

            if (queryingDepth > 0)
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
            if (listener == null || listener.func == null)
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

        private static TResult QueryList(List<Listener> list, TQuery query)
        {
            if (EventBus.Interrupt)
                return default;

            if (list == null || list.Count == 0)
                return default;

            TResult returnValue = default;
            bool hasValue = false;

            BeginQuery();

            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Listener listener = list[i];

                    if (listener == null)
                        continue;

                    Func<TQuery, TResult> func = listener.func;

                    if (func == null)
                    {
                        MarkRemoveLater(list, i, listener);
                        continue;
                    }

                    try
                    {
                        returnValue = func.Invoke(query);
                        hasValue = true;
                        break;
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        returnValue = default;
                        hasValue = false;
                        break;
                    }
                }
            }
            finally
            {
                EndQuery();
            }

            return hasValue ? returnValue : default;
        }

        private static bool TryQueryList(List<Listener> list, TQuery query, out TResult result)
        {
            result = default;

            if (EventBus.Interrupt)
                return false;

            if (list == null || list.Count == 0)
                return false;

            TResult returnValue = default;
            bool hasValue = false;

            BeginQuery();

            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Listener listener = list[i];

                    if (listener == null)
                        continue;

                    Func<TQuery, TResult> func = listener.func;

                    if (func == null)
                    {
                        MarkRemoveLater(list, i, listener);
                        continue;
                    }

                    try
                    {
                        returnValue = func.Invoke(query);
                        hasValue = true;
                        break;
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        returnValue = default;
                        hasValue = false;
                        break;
                    }
                }
            }
            finally
            {
                EndQuery();
            }

            result = returnValue;
            return hasValue;
        }

        private static List<TResult> QueryAllList(List<Listener> list, TQuery query)
        {
            List<TResult> resultList = new List<TResult>();

            if (EventBus.Interrupt)
                return resultList;

            if (list == null || list.Count == 0)
                return resultList;

            BeginQuery();

            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Listener listener = list[i];

                    if (listener == null)
                        continue;

                    Func<TQuery, TResult> func = listener.func;

                    if (func == null)
                    {
                        MarkRemoveLater(list, i, listener);
                        continue;
                    }

                    try
                    {
                        resultList.Add(func.Invoke(query));
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            finally
            {
                EndQuery();
            }

            return resultList;
        }

        private static void BeginQuery()
        {
            queryingDepth++;
        }

        private static void EndQuery()
        {
            queryingDepth--;

            if (queryingDepth <= 0)
            {
                queryingDepth = 0;
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

                    if (listener != null && listener.func != null)
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

            if (queryingDepth <= 0 && list.Count == 0)
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

                if (queryingDepth > 0)
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

        private static void RemoveFuncFromList(
            List<Listener> list,
            Func<TQuery, TResult> func,
            bool isEntity,
            EntityId id)
        {
            if (list == null || func == null)
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                Listener listener = list[i];

                if (listener == null)
                    continue;

                if (!Delegate.Equals(listener.func, func))
                    continue;

                if (queryingDepth > 0)
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

        private static void RemoveFuncFromPendingAdds(
            Func<TQuery, TResult> func,
            bool isEntity,
            EntityId id)
        {
            if (func == null || pendingAdds.Count == 0)
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

                if (Delegate.Equals(listener.func, func))
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

                // QueryBus 的规则：相同 priority 取最后添加的那个。
                // 所以新 listener 要插到同优先级旧 listener 前面。
                if (current.priority > listener.priority)
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

                if (listener != null && listener.func != null)
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

                if (listener != null && listener.func != null)
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
