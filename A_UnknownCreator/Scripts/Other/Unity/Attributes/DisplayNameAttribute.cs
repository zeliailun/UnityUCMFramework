using System.Diagnostics;
using System;
using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnknownCreator.Modules
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class DisplayNameAttribute : PropertyAttribute
    {
        public string displayName;
        public DisplayNameAttribute(string displayName)
        {
            this.displayName = displayName;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(DisplayNameAttribute))]
    public class DisplayNameDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DisplayNameAttribute displayNameAttribute = (DisplayNameAttribute)attribute;
            label.text = displayNameAttribute.displayName;
            EditorGUI.PropertyField(position, property, label, false);
        }
    }

#endif

}