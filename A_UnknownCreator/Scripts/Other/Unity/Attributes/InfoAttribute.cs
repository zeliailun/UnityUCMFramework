using System;
using UnityEngine;
#if UNITY_EDITOR
#endif
using System.Diagnostics;

namespace UnknownCreator.Modules
{
    public enum InfoMessageType
    {
        None,
        Info,
        Warning,
        Error,
    }

    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class InfoAttribute : PropertyAttribute
    {
        public string message { get; private set; }
        public InfoMessageType type { get; private set; }

        public InfoAttribute(string message, InfoMessageType type = InfoMessageType.Info)
        {
            this.message = message;
            this.type = type;
        }
    }


}