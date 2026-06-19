using UnityEngine;
using System.Diagnostics;
using System;



namespace UnknownCreator.Modules
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class OpenInspectorAttribute : PropertyAttribute
    {
        public OpenInspectorAttribute() { }
    }


}


