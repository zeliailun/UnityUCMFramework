using UnityEngine;
using System.Diagnostics;
using UnityEngine.UIElements;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif


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
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            // 字段本体
            var field = new PropertyField(property);
            field.style.flexGrow = 1;
            root.Add(field);

            // 仅对 ObjectReference 显示按钮
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                var button = new Button(() =>
                {
                    if (property.objectReferenceValue != null)
                        EditorUtility.OpenPropertyEditor(property.objectReferenceValue);
                })
                {
                    text = "编辑"
                };

                button.style.minWidth = 50;
                button.style.flexShrink = 0;

                // 初始化禁用状态
                button.SetEnabled(property.objectReferenceValue != null);

                // 监听引用变化，自动刷新按钮可用性
                field.RegisterValueChangeCallback(evt =>
                {
                    button.SetEnabled(property.objectReferenceValue != null);
                });

                root.Add(button);
            }

            return root;
        }
    }
#endif
}


