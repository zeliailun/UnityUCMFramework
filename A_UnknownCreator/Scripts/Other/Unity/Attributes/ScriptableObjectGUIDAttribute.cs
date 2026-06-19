using System;
using UnityEngine;
namespace UnknownCreator.Modules
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ScriptableObjectGUIDAttribute : PropertyAttribute
    {
    }

}