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
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class DisplayNameAttribute : PropertyAttribute
    {
        public string name;
        public string colorHex;
        public int fontSize;
        public float labelWidthPercent;
        public FontStyle fontStyle;
        

        public DisplayNameAttribute(
            string name,
            string colorHex = "#D2D2D2",
            int fontSize = 12,
            float labelWidthPercent = 33f,
            FontStyle fontStyle = FontStyle.Normal)
        {
            this.name = name;
            this.colorHex = colorHex;
            this.fontSize = fontSize;
            this.fontStyle = fontStyle;
            this.labelWidthPercent = labelWidthPercent;
        }

        public UnityEngine.Color color
        {
            get
            {
                ColorUtility.TryParseHtmlString(colorHex, out var c);
                return c;
            }
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(DisplayNameAttribute))]
    public class DisplayNameDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;

            var displayAttr = attribute as DisplayNameAttribute;

            var label = new Label(displayAttr.name);
            var field = new PropertyField(property, "");
            label.style.color = displayAttr.color;
            label.style.fontSize = displayAttr.fontSize;
            label.style.unityFontStyleAndWeight = displayAttr.fontStyle;
            label.style.width = new StyleLength(new Length(displayAttr.labelWidthPercent, LengthUnit.Percent));
            field.style.width = new StyleLength(new Length(100 - displayAttr.labelWidthPercent, LengthUnit.Percent));
            label.style.alignSelf = Align.Center;
            field.style.alignSelf = Align.Center;

            root.Add(label);
            root.Add(field);
            return root;
        }
    }
#endif
}
