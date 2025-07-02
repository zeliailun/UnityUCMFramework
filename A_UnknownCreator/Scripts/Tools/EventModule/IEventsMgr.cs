using System;
using System.Collections.Generic;
namespace UnknownCreator.Modules
{
    public interface IEventsMgr : IDearMgr
    {
        bool interrupt { set; get; }

        void Add(Action action, string s, int id = -1, int priority = 0);

        void Add<U>(Action<U> action, string s, int id = -1, int priority = 0);
        void AddOnce(Action action, string s, int id = -1, int priority = 0);
        void AddOnce<T>(Action<T> action, string s, int id = -1, int priority = 0);

        void Remove(Action action, string s, int id = -1);

        void Remove<U>(Action<U> action, string s, int id = -1);

        void Send(string s, int id = -1);

        void Send<U>(U info, string s, int id = -1);

        void AddR<X>(Func<X> func, string s, int id = -1, int priority = 0);

        void AddR<X, X1>(Func<X, X1> func, string s, int id = -1, int priority = 0);

        void RemoveR<X>(Func<X> func, string s, int id = -1);

        void RemoveR<X, X1>(Func<X, X1> func, string s, int id = -1);

        X SendR<X>(string s, int id = -1);

        X1 SendR<X, X1>(X info, string s, int id = -1);

        List<X> SendAllR<X>(string s, int id = -1);
        List<X1> SendAllR<X, X1>(X info, string s, int id = -1);

        void ClearEvent(string s, int id = -1);

        void ClearAllEvent();

        void Remove<T>(Delegate value, string s, int id = -1) where T : class, IEvent, new();
    }
}
