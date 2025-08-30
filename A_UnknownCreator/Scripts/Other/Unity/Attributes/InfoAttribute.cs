using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Diagnostics;

namespace UnknownCreator.Modules
{
    public enum InfoMessageType
    {
        /// <summary>
        ///   <para>Neutral message.</para>
        /// </summary>
        None,
        /// <summary>
        ///   <para>Info message.</para>
        /// </summary>
        Info,
        /// <summary>
        ///   <para>Warning message.</para>
        /// </summary>
        Warning,
        /// <summary>
        ///   <para>Error message.</para>
        /// </summary>
        Error,
    }

    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class InfoAttribute : PropertyAttribute
    {
        public string message { get; private set; }
        public InfoMessageType type { get; private set; }

        public InfoAttribute(string message, InfoMessageType type = InfoMessageType.Info)
        {
            this.message = message;
            this.type = type;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(InfoAttribute))]
    public class InfoAttributeDrawer : DecoratorDrawer
    {
        public override float GetHeight()
        {
            InfoAttribute infoBoxAttribute = (InfoAttribute)attribute;
            string[] lines = infoBoxAttribute.message.Split('\n');
            return EditorGUIUtility.singleLineHeight * lines.Length;
        }

        public override void OnGUI(Rect position)
        {
            InfoAttribute infoBoxAttribute = (InfoAttribute)attribute;
            EditorGUI.HelpBox(position, infoBoxAttribute.message, (MessageType)infoBoxAttribute.type);
        }
    }
#endif
}