using System;
using System.Collections.Generic;

namespace UnknownCreator.Modules
{

    public interface IEvent : IReference, IComparer<IEvent>
    {
        int priority { get; }

        IEvent SetDelegate(Delegate value, int priority, bool onceFlag);

        bool IsSameDelegate(Delegate @delegate);
    }
}