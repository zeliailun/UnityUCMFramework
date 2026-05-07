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
        /// 当前正在发送事件的嵌套深度。
        /// 大于 0 时，禁止 Add / Remove / Clear，防止修改正在遍历的 List。
        /// </summary>
        private int sendingDepth;

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
        // 安全检查
        //==========================================================================================================================//

        private bool CanModifyWhileSending(string operation)
        {
            if (sendingDepth <= 0)
                return true;

            string message =
                $"EventsMgr 正在 Send / SendR / SendAllR 期间执行了 {operation}。" +
                "当前事件系统不允许在事件回调中 Add / Remove / Clear。" +
                "如果是 Destroy / SetActive(false) 间接触发 OnDisable 退订，请改成延迟销毁或延迟退订。";

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            throw new InvalidOperationException(message);
#else
            UCMDebug.LogError(message);
            return false;
#endif
        }

        private void EnterSending()
        {
            sendingDepth++;
        }

        private void ExitSending()
        {
            sendingDepth--;

            if (sendingDepth < 0)
                sendingDepth = 0;
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

        //==========================================================================================================================//
        // 清理事件
        //==========================================================================================================================//

        public void ClearAllEvent()
        {
            if (delegateDict == null)
                return;

            if (!CanModifyWhileSending(nameof(ClearAllEvent)))
                return;

            foreach (var result1 in delegateDict.Values)
            {
                if (result1 == null)
                    continue;

                for (int i = 0; i < result1.Count; i++)
                {
                    if (result1[i] != null)
                        Mgr.RPool.Release(result1[i]);
                }
            }

            delegateDict.Clear();
        }

        public void ClearEvent(string key, EntityId id = default)
        {
            if (delegateDict == null)
                return;

            if (!CanModifyWhileSending(nameof(ClearEvent)))
                return;

            var compositeKey = (id, key);

            if (!delegateDict.Remove(compositeKey, out var list))
                return;

            if (list == null)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                    Mgr.RPool.Release(list[i]);
            }
        }

        //==========================================================================================================================//
        // 通用添加 / 移除
        //==========================================================================================================================//

        public void Remove<T>(Delegate del, string key, EntityId id = default)
            where T : class, IEvent, new()
        {
            if (del == null || delegateDict == null)
                return;

            if (!CanModifyWhileSending(nameof(Remove)))
                return;

            var compositeKey = (id, key);

            if (!delegateDict.TryGetValue(compositeKey, out var list) || list == null)
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                IEvent evt = list[i];

                if (evt == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                if (evt.IsSameDelegate(del))
                {
                    Mgr.RPool.Release(evt);
                    list.RemoveAt(i);
                    break;
                }
            }

            RemoveKeyIfEmpty(compositeKey, list);
        }

        private void AddInternal(string key, EntityId id, IEvent evt)
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

            while (i >= 0 && list[i].priority < evt.priority)
            {
                i--;
            }

            list.Insert(i + 1, evt);
        }

        public bool HasEvent(string key, EntityId id = default)
        {
            return delegateDict != null &&
                   delegateDict.TryGetValue((id, key), out var list) &&
                   list != null &&
                   list.Count > 0;
        }

        //==========================================================================================================================//
        // 添加无返回事件
        //==========================================================================================================================//

        public void Add(Action action, string s, EntityId id = default, int priority = 0)
        {
            if (action is null)
                return;

            if (!CanModifyWhileSending(nameof(Add)))
                return;

            AddInternal(s, id, Mgr.RPool.Load<CAction>().SetDelegate(action, priority));
        }

        public void Add<U>(Action<U> action, string s, EntityId id = default, int priority = 0)
        {
            if (action is null)
                return;

            if (!CanModifyWhileSending(nameof(Add)))
                return;

            AddInternal(s, id, Mgr.RPool.Load<CAction<U>>().SetDelegate(action, priority));
        }

        public void AddOnce(Action action, string s, EntityId id = default, int priority = 0)
        {
            if (action is null)
                return;

            if (!CanModifyWhileSending(nameof(AddOnce)))
                return;

            AddInternal(s, id, Mgr.RPool.Load<CAction>().SetDelegate(action, priority, true));
        }

        public void AddOnce<T>(Action<T> action, string s, EntityId id = default, int priority = 0)
        {
            if (action is null)
                return;

            if (!CanModifyWhileSending(nameof(AddOnce)))
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

            EnterSending();

            try
            {
                for (int i = 0; i < result.Count;)
                {
                    IEvent evt = result[i];

                    if (evt == null)
                    {
                        result.RemoveAt(i);
                        continue;
                    }

                    if (evt is CAction action)
                    {
                        if (action.target == null)
                        {
                            Mgr.RPool.Release(evt);
                            result.RemoveAt(i);
                            continue;
                        }

                        try
                        {
                            action.target.Invoke();
                        }
                        catch (Exception e)
                        {
                            UCMDebug.LogException(e);
                        }

                        if (action.once)
                        {
                            Mgr.RPool.Release(evt);
                            result.RemoveAt(i);
                            continue;
                        }
                    }

                    i++;
                }
            }
            finally
            {
                ExitSending();
            }

            RemoveKeyIfEmpty(compositeKey, result);
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

            EnterSending();

            try
            {
                for (int i = 0; i < result.Count;)
                {
                    IEvent evt = result[i];

                    if (evt == null)
                    {
                        result.RemoveAt(i);
                        continue;
                    }

                    if (evt is CAction<U> action)
                    {
                        if (action.target == null)
                        {
                            Mgr.RPool.Release(evt);
                            result.RemoveAt(i);
                            continue;
                        }

                        try
                        {
                            action.target.Invoke(info);
                        }
                        catch (Exception e)
                        {
                            UCMDebug.LogException(e);
                        }

                        if (action.once)
                        {
                            Mgr.RPool.Release(evt);
                            result.RemoveAt(i);
                            continue;
                        }
                    }

                    i++;
                }
            }
            finally
            {
                ExitSending();
            }

            RemoveKeyIfEmpty(compositeKey, result);
        }

        //==========================================================================================================================//
        // 添加有返回值事件
        //==========================================================================================================================//

        public void AddR<X>(Func<X> func, string s, EntityId id = default, int priority = 0)
        {
            if (func is null)
                return;

            if (!CanModifyWhileSending(nameof(AddR)))
                return;

            AddInternal(s, id, Mgr.RPool.Load<CFunc<X>>().SetDelegate(func, priority));
        }

        public void AddR<X, X1>(Func<X, X1> func, string s, EntityId id = default, int priority = 0)
        {
            if (func is null)
                return;

            if (!CanModifyWhileSending(nameof(AddR)))
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
            if (interrupt ||
                delegateDict == null ||
                !delegateDict.TryGetValue((id, s), out var result) ||
                result == null ||
                result.Count == 0)
            {
                return default;
            }

            EnterSending();

            try
            {
                // priority 高的在前面，所以从 0 开始找。
                for (int i = 0; i < result.Count; i++)
                {
                    if (result[i] is not CFunc<X> func)
                        continue;

                    if (func.target == null)
                        continue;

                    try
                    {
                        return func.target.Invoke();
                    }
                    catch (Exception e)
                    {
                        UCMDebug.LogException(e);
                    }
                }

                return default;
            }
            finally
            {
                ExitSending();
            }
        }

        public X1 SendR<X, X1>(X info, string s, EntityId id = default)
        {
            if (interrupt ||
                delegateDict == null ||
                !delegateDict.TryGetValue((id, s), out var result) ||
                result == null ||
                result.Count == 0)
            {
                return default;
            }

            EnterSending();

            try
            {
                // priority 高的在前面，所以从 0 开始找。
                for (int i = 0; i < result.Count; i++)
                {
                    if (result[i] is not CFunc<X, X1> func)
                        continue;

                    if (func.target == null)
                        continue;

                    try
                    {
                        return func.target.Invoke(info);
                    }
                    catch (Exception e)
                    {
                        UCMDebug.LogException(e);
                    }
                }

                return default;
            }
            finally
            {
                ExitSending();
            }
        }

        public List<X> SendAllR<X>(string s, EntityId id = default)
        {
            if (interrupt ||
                delegateDict == null ||
                !delegateDict.TryGetValue((id, s), out var result) ||
                result == null ||
                result.Count == 0)
            {
                return null;
            }

            List<X> list = new List<X>();

            EnterSending();

            try
            {
                // priority 高的在前面，所以从 0 开始执行。
                for (int i = 0; i < result.Count; i++)
                {
                    if (result[i] is not CFunc<X> funcEvt)
                        continue;

                    if (funcEvt.target == null)
                        continue;

                    try
                    {
                        list.Add(funcEvt.target.Invoke());
                    }
                    catch (Exception e)
                    {
                        UCMDebug.LogException(e);
                    }
                }

                return list;
            }
            finally
            {
                ExitSending();
            }
        }

        public List<X1> SendAllR<X, X1>(X info, string s, EntityId id = default)
        {
            if (interrupt ||
                delegateDict == null ||
                !delegateDict.TryGetValue((id, s), out var result) ||
                result == null ||
                result.Count == 0)
            {
                return null;
            }

            List<X1> list = new List<X1>();

            EnterSending();

            try
            {
                // priority 高的在前面，所以从 0 开始执行。
                for (int i = 0; i < result.Count; i++)
                {
                    if (result[i] is not CFunc<X, X1> funcEvt)
                        continue;

                    if (funcEvt.target == null)
                        continue;

                    try
                    {
                        list.Add(funcEvt.target.Invoke(info));
                    }
                    catch (Exception e)
                    {
                        UCMDebug.LogException(e);
                    }
                }

                return list;
            }
            finally
            {
                ExitSending();
            }
        }
    }
}