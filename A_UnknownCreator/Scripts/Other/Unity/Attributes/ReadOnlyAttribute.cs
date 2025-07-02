
using System;
using UnityEngine;
using System.Diagnostics;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnknownCreator.Modules
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ReadOnlyAttribute : PropertyAttribute
    {

    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false; // 使属性在Inspector中变为只读
            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = true; // 重新启用GUI
        }
    }
#endif
}
