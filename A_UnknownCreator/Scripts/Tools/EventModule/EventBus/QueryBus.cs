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

        private static readonly List<Listener> globalListeners = new();
        private static readonly Dictionary<EntityId, List<Listener>> entityListeners = new();

#if UNITY_EDITOR
        private static int queryingDepth;
#endif

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

#if UNITY_EDITOR
            WarnIfModifyWhileQuerying("Subscribe");
#endif

            if (!allowDuplicate)
                Remove(func);

            Listener listener = new Listener
            {
                func = func,
                priority = priority,
                isEntity = false
            };

            InsertByPriority(globalListeners, listener);

            return new EventHandle(() =>
            {
                RemoveListener(listener);
            });
        }

        public static void Remove(Func<TResult> func)
        {
            if (func == null)
                return;

#if UNITY_EDITOR
            WarnIfModifyWhileQuerying("Unsubscribe");
#endif

            RemoveFuncFromList(globalListeners, func);
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
            return globalListeners.Count > 0;
        }

        public static int GetListenerCount()
        {
            return globalListeners.Count;
        }

        public static void Clear()
        {
#if UNITY_EDITOR
            WarnIfModifyWhileQuerying("Clear");
#endif

            globalListeners.Clear();
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

#if UNITY_EDITOR
            WarnIfModifyWhileQuerying("SubscribeEntity");
#endif

            if (!allowDuplicate)
                RemoveEntity(id, func);

            Listener listener = new Listener
            {
                id = id,
                func = func,
                priority = priority,
                isEntity = true
            };

            List<Listener> list = GetOrCreateEntityList(id);
            InsertByPriority(list, listener);

            return new EventHandle(() =>
            {
                RemoveListener(listener);
            });
        }

        public static void RemoveEntity(EntityId id, Func<TResult> func)
        {
            if (func == null)
                return;

#if UNITY_EDITOR
            WarnIfModifyWhileQuerying("UnsubscribeEntity");
#endif

            if (!entityListeners.TryGetValue(id, out List<Listener> list))
                return;

            RemoveFuncFromList(list, func);

            if (list.Count == 0)
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
            return entityListeners.TryGetValue(id, out List<Listener> list) && list.Count > 0;
        }

        public static int GetEntityListenerCount(EntityId id)
        {
            return entityListeners.TryGetValue(id, out List<Listener> list) ? list.Count : 0;
        }

        public static void ClearEntity(EntityId id)
        {
#if UNITY_EDITOR
            WarnIfModifyWhileQuerying("ClearEntity");
#endif

            entityListeners.Remove(id);
        }

        // =========================
        // All
        // =========================

        public static void ClearAll()
        {
#if UNITY_EDITOR
            WarnIfModifyWhileQuerying("ClearAll");
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

        private static TResult QueryList(List<Listener> list)
        {
            if (EventBus.Interrupt)
                return default;

            if (list == null || list.Count == 0)
                return default;

#if UNITY_EDITOR
            queryingDepth++;
#endif

            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Listener listener = list[i];

                    if (listener == null || listener.func == null)
                        continue;

                    try
                    {
                        return listener.func.Invoke();
                    }
                    catch (Exception e)
                    {
                        UCMDebug.LogException(e);
                        return default;
                    }
                }

                return default;
            }
            finally
            {
#if UNITY_EDITOR
                queryingDepth--;
#endif
            }
        }

        private static bool TryQueryList(List<Listener> list, out TResult result)
        {
            result = default;

            if (EventBus.Interrupt)
                return false;

            if (list == null || list.Count == 0)
                return false;

#if UNITY_EDITOR
            queryingDepth++;
#endif

            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Listener listener = list[i];

                    if (listener == null || listener.func == null)
                        continue;

                    try
                    {
                        result = listener.func.Invoke();
                        return true;
                    }
                    catch (Exception e)
                    {
                        UCMDebug.LogException(e);
                        return false;
                    }
                }

                return false;
            }
            finally
            {
#if UNITY_EDITOR
                queryingDepth--;
#endif
            }
        }

        private static List<TResult> QueryAllList(List<Listener> list)
        {
            List<TResult> resultList = new List<TResult>();

            if (EventBus.Interrupt)
                return resultList;

            if (list == null || list.Count == 0)
                return resultList;

#if UNITY_EDITOR
            queryingDepth++;
#endif

            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Listener listener = list[i];

                    if (listener == null || listener.func == null)
                        continue;

                    try
                    {
                        resultList.Add(listener.func.Invoke());
                    }
                    catch (Exception e)
                    {
                        UCMDebug.LogException(e);
                    }
                }

                return resultList;
            }
            finally
            {
#if UNITY_EDITOR
                queryingDepth--;
#endif
            }
        }

        private static void RemoveListener(Listener listener)
        {
            if (listener == null)
                return;

#if UNITY_EDITOR
            WarnIfModifyWhileQuerying("RemoveListener");
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

        private static void RemoveFuncFromList(List<Listener> list, Func<TResult> func)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Listener listener = list[i];

                if (listener == null)
                    continue;

                if (Delegate.Equals(listener.func, func))
                    list.RemoveAt(i);
            }
        }

        private static void InsertByPriority(List<Listener> list, Listener listener)
        {
            int i = list.Count - 1;

            while (i >= 0 && list[i].priority <= listener.priority)
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
        private static void WarnIfModifyWhileQuerying(string operation)
        {
            if (queryingDepth <= 0)
                return;

            UCMDebug.LogWarning(
                $"QueryBus<{typeof(TResult).Name}> 正在 Query 时执行了 {operation}。当前版本没有延迟操作队列，可能导致监听顺序或索引异常。"
            );
        }
#endif
    }

    /// <summary>
    /// 带参数返回值查询总线。
    /// 全局查询：QueryBus<TQuery, TResult> + global List，不查字典。
    /// 实体查询：QueryBus<TQuery, TResult> + EntityId，才查 Dictionary。
    /// 查询规则：priority 高的先返回；相同 priority 取最后添加的那个。
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

        private static readonly List<Listener> globalListeners = new();
        private static readonly Dictionary<EntityId, List<Listener>> entityListeners = new();

#if UNITY_EDITOR
        private static bool isQuerying;
#endif

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

#if UNITY_EDITOR
            WarnIfModifyWhileQuerying("Subscribe");
#endif

            if (!allowDuplicate)
                Remove(func);

            Listener listener = new Listener
            {
                func = func,
                priority = priority,
                isEntity = false
            };

            InsertByPriority(globalListeners, listener);

            return new EventHandle(() =>
            {
                RemoveListener(listener);
            });
        }

        public static void Remove(Func<TQuery, TResult> func)
        {
            if (func == null)
                return;

#if UNITY_EDITOR
            WarnIfModifyWhileQuerying("Unsubscribe");
#endif

            RemoveFuncFromList(globalListeners, func);
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
            return globalListeners.Count > 0;
        }

        public static int GetListenerCount()
        {
            return globalListeners.Count;
        }

        public static void Clear()
        {
#if UNITY_EDITOR
            WarnIfModifyWhileQuerying("Clear");
#endif

            globalListeners.Clear();
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

#if UNITY_EDITOR
            WarnIfModifyWhileQuerying("SubscribeEntity");
#endif

            if (!allowDuplicate)
                RemoveEntity(id, func);

            Listener listener = new Listener
            {
                id = id,
                func = func,
                priority = priority,
                isEntity = true
            };

            List<Listener> list = GetOrCreateEntityList(id);
            InsertByPriority(list, listener);

            return new EventHandle(() =>
            {
                RemoveListener(listener);
            });
        }

        public static void RemoveEntity(EntityId id, Func<TQuery, TResult> func)
        {
            if (func == null)
                return;

#if UNITY_EDITOR
            WarnIfModifyWhileQuerying("UnsubscribeEntity");
#endif

            if (!entityListeners.TryGetValue(id, out List<Listener> list))
                return;

            RemoveFuncFromList(list, func);

            if (list.Count == 0)
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
            return entityListeners.TryGetValue(id, out List<Listener> list) && list.Count > 0;
        }

        public static int GetEntityListenerCount(EntityId id)
        {
            return entityListeners.TryGetValue(id, out List<Listener> list) ? list.Count : 0;
        }

        public static void ClearEntity(EntityId id)
        {
#if UNITY_EDITOR
            WarnIfModifyWhileQuerying("ClearEntity");
#endif

            entityListeners.Remove(id);
        }

        // =========================
        // All
        // =========================

        public static void ClearAll()
        {
#if UNITY_EDITOR
            WarnIfModifyWhileQuerying("ClearAll");
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

        private static TResult QueryList(List<Listener> list, TQuery query)
        {
            if (EventBus.Interrupt)
                return default;

            if (list == null || list.Count == 0)
                return default;

#if UNITY_EDITOR
            isQuerying = true;
#endif

            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Listener listener = list[i];

                    if (listener == null || listener.func == null)
                        continue;

                    try
                    {
                        return listener.func.Invoke(query);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        return default;
                    }
                }

                return default;
            }
            finally
            {
#if UNITY_EDITOR
                isQuerying = false;
#endif
            }
        }

        private static bool TryQueryList(List<Listener> list, TQuery query, out TResult result)
        {
            result = default;

            if (EventBus.Interrupt)
                return false;

            if (list == null || list.Count == 0)
                return false;

#if UNITY_EDITOR
            isQuerying = true;
#endif

            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Listener listener = list[i];

                    if (listener == null || listener.func == null)
                        continue;

                    try
                    {
                        result = listener.func.Invoke(query);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        return false;
                    }
                }

                return false;
            }
            finally
            {
#if UNITY_EDITOR
                isQuerying = false;
#endif
            }
        }

        private static List<TResult> QueryAllList(List<Listener> list, TQuery query)
        {
            List<TResult> resultList = new List<TResult>();

            if (EventBus.Interrupt)
                return resultList;

            if (list == null || list.Count == 0)
                return resultList;

#if UNITY_EDITOR
            isQuerying = true;
#endif

            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    Listener listener = list[i];

                    if (listener == null || listener.func == null)
                        continue;

                    try
                    {
                        resultList.Add(listener.func.Invoke(query));
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

                return resultList;
            }
            finally
            {
#if UNITY_EDITOR
                isQuerying = false;
#endif
            }
        }

        private static void RemoveListener(Listener listener)
        {
            if (listener == null)
                return;

#if UNITY_EDITOR
            WarnIfModifyWhileQuerying("RemoveListener");
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

        private static void RemoveFuncFromList(List<Listener> list, Func<TQuery, TResult> func)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Listener listener = list[i];

                if (listener == null)
                    continue;

                if (Delegate.Equals(listener.func, func))
                    list.RemoveAt(i);
            }
        }

        private static void InsertByPriority(List<Listener> list, Listener listener)
        {
            int i = list.Count - 1;

            while (i >= 0 && list[i].priority <= listener.priority)
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
        private static void WarnIfModifyWhileQuerying(string operation)
        {
            if (!isQuerying)
                return;

            Debug.LogWarning(
                $"QueryBus<{typeof(TQuery).Name}, {typeof(TResult).Name}> 正在 Query 时执行了 {operation}。当前版本没有延迟操作队列，可能导致监听顺序或索引异常。"
            );
        }
#endif
    }
}
