using System;
namespace UnknownCreator.Modules
{
    public static class DelegateGlobals
    {
        public static int Count(this Delegate action)
        => action is null ? 0 : action.GetInvocationList().Length;

    }
}