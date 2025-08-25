
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Diagnostics;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace UnknownCreator.Modules
{
    /*
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class DisplayNameAttribute : PropertyAttribute
    {
        public string name;
        public string colorHex;

        public DisplayNameAttribute(string name, string colorHex = "#A9A9A9")
        {
            this.name = name;
            this.colorHex = colorHex;
        }

        public Color color
        {
            get
            {
                ColorUtility.TryParseHtmlString(colorHex, out var c);
                return c;
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ScriptableObject), true, isFallback = true)]
    public class DisplayNameEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            if (target == null) return root;

            var so = serializedObject;
            var iterator = so.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                var field = new PropertyField(iterator);

                var type = target.GetType();
                var member = type.GetField(iterator.name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (member != null)
                {
                    if (System.Attribute.GetCustomAttribute(member, typeof(DisplayNameAttribute)) is DisplayNameAttribute attr)
                    {
                        field.label = attr.name;
                        field.style.color = attr.color;
                    }
                }

                field.Bind(so);
                root.Add(field);
            }

            return root;
        }
    }
#endif
    */
}