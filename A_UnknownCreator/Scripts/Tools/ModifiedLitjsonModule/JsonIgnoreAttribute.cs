using System;

namespace UnknownCreator.Modules
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class JsonIgnoreAttribute : Attribute
    {

    }
}
