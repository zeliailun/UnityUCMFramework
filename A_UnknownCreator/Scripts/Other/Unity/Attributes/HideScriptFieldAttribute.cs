using System.Diagnostics;
using System;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace UnknownCreator.Modules
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class HideScriptFieldAttribute : Attribute { }


#if UNITY_EDITOR

    [CustomEditor(typeof(UnityEngine.ScriptableObject), true, isFallback = true)]
    public class HideScriptFieldEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            if (target == null) return root;

            var type = target.GetType();
            bool hideScript = Attribute.IsDefined(type, typeof(HideScriptFieldAttribute));

            var so = serializedObject;
            var iterator = so.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (hideScript && iterator.name == "m_Script")
                    continue;

                var field = new PropertyField(iterator);

                if (!hideScript && iterator.name == "m_Script")
                    field.SetEnabled(false);

                field.Bind(so);
                root.Add(field);
            }

            return root;
        }
    }
#endif
}