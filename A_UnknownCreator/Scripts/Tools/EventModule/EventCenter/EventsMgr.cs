using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public sealed class EventsMgr : IEventsMgr
    {
        internal Dictionary<(EntityId, string), List<IEvent>> delegateDict = new();

        /// <summary>
        /// 是否中断事件发送，默认不中断。
        /// </summary>
        public bool interrupt { get; set; } = false;

        /// <summary>
        /// 当前事件发送嵌套深度。
        /// Send 里又触发 Send 时会递增。
        /// 只有回到 0，才真正释放和压缩列表。
        /// </summary>
        private int sendingDepth = 0;

        /// <summary>
        /// Send 期间被移除的事件，先放这里，等所有 Send 结束后再 Release。
        /// </summary>
        private readonly List<IEvent> pendingRelease = new();

        /// <summary>
        /// 防止同一个事件对象重复进入 pendingRelease。
        /// </summary>
        private readonly HashSet<IEvent> pendingReleaseSet = new();

        /// <summary>
        /// Send 期间产生了 null 占位符的 key。
        /// Flush 时只压缩这些列表。
        /// </summary>
        private readonly HashSet<(EntityId, string)> dirtyKeys = new();

        /// <summary>
        /// Send 期间新增的事件，延迟到 Send 结束后再插入。
        /// 这样不会影响当前 Send 的遍历顺序。
        /// </summary>
        private readonly List<PendingAdd> pendingAdds = new();

        private struct PendingAdd
        {
            public string key;
            public EntityId id;
            public IEvent evt;
        }

        void IDearMgr.WorkWork()
        {
            delegateDict ??= new();
        }

        void IDearMgr.DoNothing()
        {
            ClearAllEvent();
            interrupt = false;
        }

        //==========================================================================================================================//
        // 工具方法
        //==========================================================================================================================//

        private void BeginSend()
        {
            sendingDepth++;
        }

        private void EndSend()
        {
            sendingDepth--;

            if (sendingDepth <= 0)
            {
                sendingDepth = 0;
                FlushPendingChanges();
            }
        }

        private void AddPendingRelease(IEvent evt)
        {
            if (evt == null)
                return;

            if (pendingReleaseSet.Add(evt))
            {
                pendingRelease.Add(evt);
            }
        }

        private void MarkRemoveLater((EntityId, string) compositeKey, List<IEvent> list, int index)
        {
            if (list == null || index < 0 || index >= list.Count)
                return;

            IEvent evt = list[index];

            if (evt == null)
                return;

            list[index] = null;
            dirtyKeys.Add(compositeKey);
            AddPendingRelease(evt);
        }

        private void RemoveKeyIfEmpty((EntityId, string) compositeKey, List<IEvent> list)
        {
            if (list == null || list.Count > 0)
                return;

            if (delegateDict != null &&
                delegateDict.TryGetValue(compositeKey, out var currentList) &&
                ReferenceEquals(currentList, list))
            {
                delegateDict.Remove(compositeKey);
            }
        }

        private void RemoveNullSlots((EntityId, string) compositeKey, List<IEvent> list)
        {
            if (list == null)
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == null)
                    list.RemoveAt(i);
            }

            RemoveKeyIfEmpty(compositeKey, list);
        }

        private bool HasAliveEvent(List<IEvent> list)
        {
            if (list == null)
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                    return true;
            }

            return false;
        }

        private bool IsSameKey(PendingAdd pendingAdd, string key, EntityId id)
        {
            return pendingAdd.id.Equals(id) && pendingAdd.key == key;
        }

        private void RemoveMatchingPendingAdds(Delegate del, string key, EntityId id)
        {
            if (del == null || pendingAdds.Count == 0)
                return;

            for (int i = pendingAdds.Count - 1; i >= 0; i--)
            {
                PendingAdd add = pendingAdds[i];

                if (!IsSameKey(add, key, id))
                    continue;

                if (add.evt == null)
                {
                    pendingAdds.RemoveAt(i);
                    continue;
                }

                if (add.evt.IsSameDelegate(del))
                {
                    AddPendingRelease(add.evt);
                    pendingAdds.RemoveAt(i);
                }
            }
        }

        private void FlushPendingChanges()
        {
            // 1. 真正释放 Send 期间被标记删除的事件对象。
            for (int i = 0; i < pendingRelease.Count; i++)
            {
                if (pendingRelease[i] != null)
                    Mgr.RPool.Release(pendingRelease[i]);
            }

            pendingRelease.Clear();
            pendingReleaseSet.Clear();

            // 2. 压缩有 null 占位符的列表。
            if (delegateDict != null && dirtyKeys.Count > 0)
            {
                foreach (var compositeKey in dirtyKeys)
                {
                    if (delegateDict.TryGetValue(compositeKey, out var list))
                    {
                        RemoveNullSlots(compositeKey, list);
                    }
                }
            }

            dirtyKeys.Clear();

            // 3. 应用 Send 期间新增的事件。
            if (pendingAdds.Count > 0)
            {
                for (int i = 0; i < pendingAdds.Count; i++)
                {
                    PendingAdd add = pendingAdds[i];

                    if (add.evt != null)
                    {
                        AddInternalDirect(add.key, add.id, add.evt);
                    }
                }

                pendingAdds.Clear();
            }
        }

        //==========================================================================================================================//
        // 清理事件
        //==========================================================================================================================//

        public void ClearAllEvent()
        {
            if (delegateDict == null)
                return;

            if (sendingDepth > 0)
            {
                foreach (var kv in delegateDict)
                {
                    var compositeKey = kv.Key;
                    var list = kv.Value;

                    if (list == null)
                        continue;

                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] != null)
                            MarkRemoveLater(compositeKey, list, i);
                    }
                }

                delegateDict.Clear();
                dirtyKeys.Clear();

                // ClearAllEvent 的语义是全部清掉，所以 Send 期间已经排队的 Add 也要清掉。
                for (int i = 0; i < pendingAdds.Count; i++)
                {
                    AddPendingRelease(pendingAdds[i].evt);
                }

                pendingAdds.Clear();
                return;
            }

            foreach (var result1 in delegateDict.Values)
            {
                if (result1 == null)
                    continue;

                for (int i = 0; i < result1.Count; i++)
                {
                    if (result1[i] != null)
                        Mgr.RPool.Release(result1[i]);
                }

                result1.Clear();
            }

            delegateDict.Clear();

            pendingRelease.Clear();
            pendingReleaseSet.Clear();
            dirtyKeys.Clear();
            pendingAdds.Clear();
        }

        public void ClearEvent(string key, EntityId id = default)
        {
            if (delegateDict == null)
                return;

            var compositeKey = (id, key);

            if (sendingDepth > 0)
            {
                if (delegateDict.TryGetValue(compositeKey, out var list) && list != null)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] != null)
                            MarkRemoveLater(compositeKey, list, i);
                    }

                    delegateDict.Remove(compositeKey);
                    dirtyKeys.Remove(compositeKey);
                }

                // 如果 Send 期间已经 Add 了同 key 的事件，ClearEvent 也应该把它们清掉。
                for (int i = pendingAdds.Count - 1; i >= 0; i--)
                {
                    if (IsSameKey(pendingAdds[i], key, id))
                    {
                        AddPendingRelease(pendingAdds[i].evt);
                        pendingAdds.RemoveAt(i);
                    }
                }

                return;
            }

            if (!delegateDict.Remove(compositeKey, out var removeList))
                return;

            if (removeList == null)
                return;

            for (int i = 0; i < removeList.Count; i++)
            {
                if (removeList[i] != null)
                    Mgr.RPool.Release(removeList[i]);
            }

            removeList.Clear();
        }

        //==========================================================================================================================//
        // 通用添加 / 移除
        //==========================================================================================================================//

        public void Remove<T>(Delegate del, string key, EntityId id = default)
            where T : class, IEvent, new()
        {
            if (del == null)
                return;

            var compositeKey = (id, key);

            if (delegateDict != null && delegateDict.TryGetValue(compositeKey, out var list) && list != null)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    IEvent evt = list[i];

                    if (evt == null)
                    {
                        if (sendingDepth <= 0)
                            list.RemoveAt(i);

                        continue;
                    }

                    if (evt.IsSameDelegate(del))
                    {
                        if (sendingDepth > 0)
                        {
                            MarkRemoveLater(compositeKey, list, i);
                        }
                        else
                        {
                            Mgr.RPool.Release(evt);
                            list.RemoveAt(i);
                            RemoveKeyIfEmpty(compositeKey, list);
                        }

                        break;
                    }
                }

                if (sendingDepth <= 0)
                    RemoveKeyIfEmpty(compositeKey, list);
            }

            // 关键补丁：Send 期间 Add 的事件还在 pendingAdds 里，Remove 也要能取消它们。
            if (sendingDepth > 0)
            {
                RemoveMatchingPendingAdds(del, key, id);
            }
        }

        private void AddInternal(string key, EntityId id, IEvent evt)
        {
            if (evt == null)
                return;

            delegateDict ??= new();

            if (sendingDepth > 0)
            {
                pendingAdds.Add(new PendingAdd
                {
                    key = key,
                    id = id,
                    evt = evt
                });

                return;
            }

            AddInternalDirect(key, id, evt);
        }

        private void AddInternalDirect(string key, EntityId id, IEvent evt)
        {
            if (evt == null)
                return;

            delegateDict ??= new();

            var compositeKey = (id, key);

            if (!delegateDict.TryGetValue(compositeKey, out var list))
            {
                list = new List<IEvent>();
                delegateDict[compositeKey] = list;
            }

            // priority 高的排前面。
            int i = list.Count - 1;

            while (i >= 0)
            {
                IEvent current = list[i];

                if (current == null)
                {
                    i--;
                    continue;
                }

                if (current.priority >= evt.priority)
                    break;

                i--;
            }

            list.Insert(i + 1, evt);
        }

        public bool HasEvent(string key, EntityId id = default)
        {
            if (delegateDict == null)
                return false;

            if (!delegateDict.TryGetValue((id, key), out var list))
                return false;

            return HasAliveEvent(list);
        }

        //==========================================================================================================================//
        // 添加无返回事件
        //==========================================================================================================================//

        public void Add(Action action, string s, EntityId id = default, int priority = 0)
        {
            if (action is null)
                return;

            AddInternal(s, id, Mgr.RPool.Load<CAction>().SetDelegate(action, priority));
        }

        public void Add<U>(Action<U> action, string s, EntityId id = default, int priority = 0)
        {
            if (action is null)
                return;

            AddInternal(s, id, Mgr.RPool.Load<CAction<U>>().SetDelegate(action, priority));
        }

        public void AddOnce(Action action, string s, EntityId id = default, int priority = 0)
        {
            if (action is null)
                return;

            AddInternal(s, id, Mgr.RPool.Load<CAction>().SetDelegate(action, priority, true));
        }

        public void AddOnce<T>(Action<T> action, string s, EntityId id = default, int priority = 0)
        {
            if (action is null)
                return;

            AddInternal(s, id, Mgr.RPool.Load<CAction<T>>().SetDelegate(action, priority, true));
        }

        //==========================================================================================================================//
        // 移除无返回事件
        //==========================================================================================================================//

        public void Remove(Action action, string s, EntityId id = default)
        {
            Remove<CAction>(action, s, id);
        }

        public void Remove<U>(Action<U> action, string s, EntityId id = default)
        {
            Remove<CAction<U>>(action, s, id);
        }

        //==========================================================================================================================//
        // 发送无返回事件
        //==========================================================================================================================//

        public void Send(string s, EntityId id = default)
        {
            var compositeKey = (id, s);

            if (interrupt ||
                delegateDict == null ||
                !delegateDict.TryGetValue(compositeKey, out var result) ||
                result == null ||
                result.Count == 0)
            {
                return;
            }

            BeginSend();

            try
            {
                for (int i = 0; i < result.Count; i++)
                {
                    IEvent evt = result[i];

                    if (evt == null)
                        continue;

                    if (evt is CAction action)
                    {
                        Action target = action.target;

                        if (target == null)
                        {
                            MarkRemoveLater(compositeKey, result, i);
                            continue;
                        }

                        // Once 事件先标记删除，再执行。
                        // 这样回调里再次 Send 同事件时，不会重复触发这个 Once。
                        if (action.once)
                        {
                            MarkRemoveLater(compositeKey, result, i);
                        }

                        target.Invoke();
                    }
                }
            }
            finally
            {
                EndSend();
            }
        }

        public void Send<U>(U info, string s, EntityId id = default)
        {
            var compositeKey = (id, s);

            if (interrupt ||
                delegateDict == null ||
                !delegateDict.TryGetValue(compositeKey, out var result) ||
                result == null ||
                result.Count == 0)
            {
                return;
            }

            BeginSend();

            try
            {
                for (int i = 0; i < result.Count; i++)
                {
                    IEvent evt = result[i];

                    if (evt == null)
                        continue;

                    if (evt is CAction<U> action)
                    {
                        Action<U> target = action.target;

                        if (target == null)
                        {
                            MarkRemoveLater(compositeKey, result, i);
                            continue;
                        }

                        if (action.once)
                        {
                            MarkRemoveLater(compositeKey, result, i);
                        }

                        target.Invoke(info);
                    }
                }
            }
            finally
            {
                EndSend();
            }
        }

        //==========================================================================================================================//
        // 添加有返回值事件
        //==========================================================================================================================//

        public void AddR<X>(Func<X> func, string s, EntityId id = default, int priority = 0)
        {
            if (func is null)
                return;

            AddInternal(s, id, Mgr.RPool.Load<CFunc<X>>().SetDelegate(func, priority));
        }

        public void AddR<X, X1>(Func<X, X1> func, string s, EntityId id = default, int priority = 0)
        {
            if (func is null)
                return;

            AddInternal(s, id, Mgr.RPool.Load<CFunc<X, X1>>().SetDelegate(func, priority));
        }

        //==========================================================================================================================//
        // 移除有返回值事件
        //==========================================================================================================================//

        public void RemoveR<X>(Func<X> func, string s, EntityId id = default)
        {
            Remove<CFunc<X>>(func, s, id);
        }

        public void RemoveR<X, X1>(Func<X, X1> func, string s, EntityId id = default)
        {
            Remove<CFunc<X, X1>>(func, s, id);
        }

        //==========================================================================================================================//
        // 发送有返回值事件
        //==========================================================================================================================//

        public X SendR<X>(string s, EntityId id = default)
        {
            var compositeKey = (id, s);

            if (interrupt ||
                delegateDict == null ||
                !delegateDict.TryGetValue(compositeKey, out var result) ||
                result == null ||
                result.Count == 0)
            {
                return default;
            }

            BeginSend();

            try
            {
                // priority 高的在前面，所以从 0 开始找。
                for (int i = 0; i < result.Count; i++)
                {
                    IEvent evt = result[i];

                    if (evt == null)
                        continue;

                    if (evt is not CFunc<X> func)
                        continue;

                    Func<X> target = func.target;

                    if (target == null)
                    {
                        MarkRemoveLater(compositeKey, result, i);
                        continue;
                    }

                    return target.Invoke();
                }

                return default;
            }
            finally
            {
                EndSend();
            }
        }

        public X1 SendR<X, X1>(X info, string s, EntityId id = default)
        {
            var compositeKey = (id, s);

            if (interrupt ||
                delegateDict == null ||
                !delegateDict.TryGetValue(compositeKey, out var result) ||
                result == null ||
                result.Count == 0)
            {
                return default;
            }

            BeginSend();

            try
            {
                // priority 高的在前面，所以从 0 开始找。
                for (int i = 0; i < result.Count; i++)
                {
                    IEvent evt = result[i];

                    if (evt == null)
                        continue;

                    if (evt is not CFunc<X, X1> func)
                        continue;

                    Func<X, X1> target = func.target;

                    if (target == null)
                    {
                        MarkRemoveLater(compositeKey, result, i);
                        continue;
                    }

                    return target.Invoke(info);
                }

                return default;
            }
            finally
            {
                EndSend();
            }
        }

        public List<X> SendAllR<X>(string s, EntityId id = default)
        {
            var compositeKey = (id, s);

            if (interrupt ||
                delegateDict == null ||
                !delegateDict.TryGetValue(compositeKey, out var result) ||
                result == null ||
                result.Count == 0)
            {
                return null;
            }

            List<X> list = new List<X>();

            BeginSend();

            try
            {
                // priority 高的在前面，所以从 0 开始执行。
                for (int i = 0; i < result.Count; i++)
                {
                    IEvent evt = result[i];

                    if (evt == null)
                        continue;

                    if (evt is not CFunc<X> funcEvt)
                        continue;

                    Func<X> target = funcEvt.target;

                    if (target == null)
                    {
                        MarkRemoveLater(compositeKey, result, i);
                        continue;
                    }

                    list.Add(target.Invoke());
                }

                return list;
            }
            finally
            {
                EndSend();
            }
        }

        public List<X1> SendAllR<X, X1>(X info, string s, EntityId id = default)
        {
            var compositeKey = (id, s);

            if (interrupt ||
                delegateDict == null ||
                !delegateDict.TryGetValue(compositeKey, out var result) ||
                result == null ||
                result.Count == 0)
            {
                return null;
            }

            List<X1> list = new List<X1>();

            BeginSend();

            try
            {
                // priority 高的在前面，所以从 0 开始执行。
                for (int i = 0; i < result.Count; i++)
                {
                    IEvent evt = result[i];

                    if (evt == null)
                        continue;

                    if (evt is not CFunc<X, X1> funcEvt)
                        continue;

                    Func<X, X1> target = funcEvt.target;

                    if (target == null)
                    {
                        MarkRemoveLater(compositeKey, result, i);
                        continue;
                    }

                    list.Add(target.Invoke(info));
                }

                return list;
            }
            finally
            {
                EndSend();
            }
        }
    }
}
