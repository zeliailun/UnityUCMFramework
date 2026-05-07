using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnknownCreator.Modules
{
    public interface IEventsMgr : IDearMgr
    {
        bool interrupt { set; get; }

        void Add(Action action, string s, EntityId id = default, int priority = 0);

        void Add<U>(Action<U> action, string s, EntityId id = default, int priority = 0);
        void AddOnce(Action action, string s, EntityId id = default, int priority = 0);
        void AddOnce<T>(Action<T> action, string s, EntityId id = default, int priority = 0);

        void Remove(Action action, string s, EntityId id = default);

        void Remove<U>(Action<U> action, string s, EntityId id = default);

        void Send(string s, EntityId id = default);

        void Send<U>(U info, string s, EntityId id = default);

        void AddR<X>(Func<X> func, string s, EntityId id = default, int priority = 0);

        void AddR<X, X1>(Func<X, X1> func, string s, EntityId id = default, int priority = 0);

        void RemoveR<X>(Func<X> func, string s, EntityId id = default);

        void RemoveR<X, X1>(Func<X, X1> func, string s, EntityId id = default);

        X SendR<X>(string s, EntityId id = default);

        X1 SendR<X, X1>(X info, string s, EntityId id = default);

        List<X> SendAllR<X>(string s, EntityId id = default);
        List<X1> SendAllR<X, X1>(X info, string s, EntityId id = default);

        void ClearEvent(string s, EntityId id = default);

        void ClearAllEvent();

        void Remove<T>(Delegate value, string s, EntityId id = default) where T : class, IEvent, new();

        bool HasEvent(string s, EntityId id = default);
    }
}
