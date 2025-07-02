using UnityEngine;
using System.Diagnostics;


#if UNITY_EDITOR
using UnityEditor;
#endif
using System;

namespace UnknownCreator.Modules
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class OpenInspectorAttribute : PropertyAttribute
    {
        public OpenInspectorAttribute() { }
    }



#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(OpenInspectorAttribute))]
    public class OpenInInspectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.PropertyField(position, property, label, false);
                return;
            }
            Rect propertyRect = new(position.x, position.y, position.width - 60, position.height);
            EditorGUI.PropertyField(propertyRect, property, label);
            Rect buttonRect = new(position.x + position.width - 55, position.y, 50, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(buttonRect, "±à¼­"))
                EditorUtility.OpenPropertyEditor(property.objectReferenceValue);

        }
    }
#endif

}