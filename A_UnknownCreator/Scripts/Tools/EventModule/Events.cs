using System;
namespace UnknownCreator.Modules
{
    public abstract class EventBase<TDelegate> : IEvent where TDelegate : Delegate
    {
        public TDelegate target { get; protected set; }

        public int priority { get; protected set; }

        public bool once { get; protected set; }

        private object targetObject;
        private IntPtr methodPtr;

        public int Compare(IEvent x, IEvent y)
            => y.priority.CompareTo(x.priority);

        public bool IsSameDelegate(Delegate d)
            => targetObject == d.Target && methodPtr == d.Method.MethodHandle.Value;

        public IEvent SetDelegate(Delegate value, int priority, bool onceFlag = false)
        {
            target = (TDelegate)value;
            targetObject = target.Target;
            methodPtr = target.Method.MethodHandle.Value;
            this.priority = priority;
            this.once = onceFlag;
            return this;
        }

        public void ObjRelease()
        {
            target = null;
            targetObject = null;
            methodPtr = IntPtr.Zero;
            OnRelease();
        }

        protected virtual void OnRelease() { }
    }

    public class CAction : EventBase<Action> { }
    public class CAction<T> : EventBase<Action<T>> { }
    public class CFunc<T> : EventBase<Func<T>> { }
    public class CFunc<T1, T2> : EventBase<Func<T1, T2>> { }

}
