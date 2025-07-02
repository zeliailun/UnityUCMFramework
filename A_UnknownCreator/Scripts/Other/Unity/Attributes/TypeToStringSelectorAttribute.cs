using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Linq;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnknownCreator.Modules
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class TypeToStringSelectorAttribute : PropertyAttribute
    {
        public System.Type TargetType { get; private set; }

        public TypeToStringSelectorAttribute(System.Type targetType)
        {
            TargetType = targetType;
        }
    }


#if UNITY_EDITOR

#endif
}