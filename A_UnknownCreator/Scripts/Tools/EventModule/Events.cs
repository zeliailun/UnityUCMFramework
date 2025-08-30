using System;
namespace UnknownCreator.Modules
{
    public abstract class EventBase<TDelegate> : IEvent where TDelegate : Delegate
    {
        public TDelegate target;
        public int priority { get; private set; }

        public bool once { get; private set; }  

        public int Compare(IEvent x, IEvent y)
            => y.priority.CompareTo(x.priority);

        public bool IsSameDelegate(Delegate @delegate)
            => target?.Target == @delegate.Target && target?.Method == @delegate.Method;

        public IEvent SetDelegate(Delegate value, int priority, bool onceFlag = false)
        {
            target = (TDelegate)value;
            this.priority = priority;
            this.once = onceFlag;
            return this;
        }

        public void ObjRelease()
        {
            target = null;
            OnRelease();
        }

        protected virtual void OnRelease() { }
    }

    public class CAction : EventBase<Action> { }
    public class CAction<T> : EventBase<Action<T>> { }
    public class CFunc<T> : EventBase<Func<T>> { }
    public class CFunc<T1, T2> : EventBase<Func<T1, T2>> { }

}
