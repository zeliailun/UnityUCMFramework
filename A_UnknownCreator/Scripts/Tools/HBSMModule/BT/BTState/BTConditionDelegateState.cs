using System;
namespace UnknownCreator.Modules
{
    public class BTConditionDelegateState : BTConditionState
    {
        public Func<bool> func;

        protected override bool GetCondition()
        => func();
    }
}