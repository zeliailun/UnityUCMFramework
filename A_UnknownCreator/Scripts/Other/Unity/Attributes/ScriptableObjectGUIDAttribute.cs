using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnknownCreator.Modules
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ScriptableObjectGUIDAttribute : PropertyAttribute
    {
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ScriptableObjectGUIDAttribute))]
    public class ScriptableObjectGUIDDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            UCMDebug.Log("ScriptableObjectGUIDDrawer");
            if (string.IsNullOrEmpty(property.stringValue))
                property.stringValue = Guid.NewGuid().ToString();
            var propertyRect = new Rect(position);
            propertyRect.xMax -= 100;
            var buttonRect = new Rect(position)
            {
                xMin = position.xMax - 100
            };
            EditorGUI.PropertyField(propertyRect, property, label, true);

            if (GUI.Button(buttonRect, "刷新ID"))
                property.stringValue = Guid.NewGuid().ToString();
        }
    }
#endif
}