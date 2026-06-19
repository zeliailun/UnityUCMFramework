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
}

