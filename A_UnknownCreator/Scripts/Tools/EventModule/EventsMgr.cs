using System.Collections.Generic;
using System;
namespace UnknownCreator.Modules
{
    public sealed class EventsMgr : IEventsMgr
    {
        internal Dictionary<(int, string), List<IEvent>> delegateDict = new();

        /// <summary>
        /// 是否中断事件发送,默认不中断
        /// </summary>
        public bool interrupt { get; set; } = false;

        // private EventsMgr() { }

        void IDearMgr.WorkWork()
        {
            delegateDict ??= new();
        }

        void IDearMgr.DoNothing()
        {
            ClearAllEvent();
            interrupt = false;
            delegateDict = null;
        }

        public void ClearAllEvent()
        {
            foreach (var result1 in delegateDict.Values)
                foreach (var result2 in result1)
                    Mgr.RPool.Release(result2);
            delegateDict.Clear();
        }

        public void ClearEvent(string key, int id = -1)
        {
            if (delegateDict.Remove((id, key), out var list))
                foreach (var evt in list)
                    Mgr.RPool.Release(evt);
        }

        public void Remove<T>(Delegate del, string key, int id = -1) where T : class, IEvent, new()
        {
            if (del == null || !delegateDict.TryGetValue((id, key), out var list)) return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].IsSameDelegate(del))
                {
                    Mgr.RPool.Release(list[i]);
                    list.RemoveAt(i);
                    break;
                }
            }

            if (list.Count == 0) delegateDict.Remove((id, key));
        }

        private void AddInternal(string key, int id, IEvent evt)
        {
            var compositeKey = (id, key);
            if (!delegateDict.TryGetValue(compositeKey, out var list))
                delegateDict[compositeKey] = list = new();

            int i = list.Count - 1;
            while (i >= 0 && list[i].priority < evt.priority) i--;
            list.Insert(i + 1, evt);
        }


        //==========================================================================================================================//


        #region 添加无返回事件

        public void Add(Action action, string s, int id = -1, int priority = 0)
        {
            if (action is null) return;

            AddInternal(s, id, Mgr.RPool.Load<CAction>().SetDelegate(action, priority));
        }

        public void Add<U>(Action<U> action, string s, int id = -1, int priority = 0)
        {
            if (action is null) return;

            AddInternal(s, id, Mgr.RPool.Load<CAction<U>>().SetDelegate(action, priority));
        }


        public void AddOnce(Action action, string s, int id = -1, int priority = 0)
        {
            if (action is null) return;

            AddInternal(s, id, Mgr.RPool.Load<CAction>().SetDelegate(action, priority, true));
        }

        public void AddOnce<T>(Action<T> action, string s, int id = -1, int priority = 0)
        {
            if (action is null) return;

            AddInternal(s, id, Mgr.RPool.Load<CAction<T>>().SetDelegate(action, priority, true));
        }

        #endregion


        #region 移除无返回值事件




        public void Remove(Action action, string s, int id = -1)
        {
            Remove<CAction>(action, s, id);
        }

        public void Remove<U>(Action<U> action, string s, int id = -1)
        {
            Remove<CAction<U>>(action, s, id);
        }




        #endregion


        #region 发送无返回值事件

        public void Send(string s, int id = -1)
        {
            if (interrupt || !delegateDict.TryGetValue((id, s), out var result)) return;

            for (int i = 0; i < result.Count;)
            {
                if (result[i] is CAction action)
                {
                    action.target?.Invoke();
                    if (action.once)
                    {
                        Mgr.RPool.Release(result[i]);
                        result.RemoveAt(i);
                        continue;
                    }
                }
                i++;
            }

            if (result.Count == 0) delegateDict.Remove((id, s));
        }

        public void Send<U>(U info, string s, int id = -1)
        {
            if (interrupt || !delegateDict.TryGetValue((id, s), out var result)) return;

            for (int i = 0; i < result.Count;)
            {
                if (result[i] is CAction<U> action)
                {
                    action.target?.Invoke(info);
                    if (action.once)
                    {
                        Mgr.RPool.Release(result[i]);
                        result.RemoveAt(i);
                        continue;
                    }
                }
                i++;
            }

            if (result.Count == 0) delegateDict.Remove((id, s));
        }

        #endregion



        //==========================================================================================================================//



        #region 加有返回值事件

        public void AddR<X>(Func<X> func, string s, int id = -1, int priority = 0)
        {
            if (func is null) return;

            AddInternal(s, id, Mgr.RPool.Load<CFunc<X>>().SetDelegate(func, priority));

        }

        public void AddR<X, X1>(Func<X, X1> func, string s, int id = -1, int priority = 0)
        {
            if (func is null) return;

            AddInternal(s, id, Mgr.RPool.Load<CFunc<X, X1>>().SetDelegate(func, priority));
        }

        #endregion


        #region 移除有返回值事件

        public void RemoveR<X>(Func<X> func, string s, int id = -1)
        {
            Remove<CFunc<X>>(func, s, id);
        }

        public void RemoveR<X, X1>(Func<X, X1> func, string s, int id = -1)
        {
            Remove<CFunc<X, X1>>(func, s, id);
        }

        #endregion


        #region 发送有返回值事件

        public X SendR<X>(string s, int id = -1)
        {
            if (interrupt || !delegateDict.TryGetValue((id, s), out var result) || result.Count == 0)
                return default(X);

            var func = result[0] as CFunc<X>;
            return func?.target != null ? func.target() : default(X);
        }

        public X1 SendR<X, X1>(X info, string s, int id = -1)
        {
            if (interrupt || !delegateDict.TryGetValue((id, s), out var result) || result.Count == 0)
                return default(X1);

            var func = result[0] as CFunc<X, X1>;
            return func?.target != null ? func.target(info) : default(X1);
        }

        public List<X> SendAllR<X>(string s, int id = -1)
        {
            var list = new List<X>();

            if (interrupt || !delegateDict.TryGetValue((id, s), out var result) || result.Count == 0)
                return null;

            for (int i = result.Count - 1; i >= 0; i--)
            {
                IEvent evt = result[i];
                if (evt is CFunc<X> funcEvt)
                    list.Add(funcEvt.target());
            }
            return list;
        }

        public List<X1> SendAllR<X, X1>(X info, string s, int id = -1)
        {
            var list = new List<X1>();

            if (interrupt || !delegateDict.TryGetValue((id, s), out var result) || result.Count == 0)
                return null;

            for (int i = result.Count - 1; i >= 0; i--)
            {
                IEvent evt = result[i];
                if (evt is CFunc<X, X1> funcEvt)
                    list.Add(funcEvt.target(info));
            }
            return list;
        }

        #endregion


    }
}