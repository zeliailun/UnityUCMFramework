using UnityEngine;
using System;
using System.Diagnostics;

namespace UnknownCreator.Modules
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class ShowIfAttribute : PropertyAttribute
    {
        public string conditionName { get; private set; }
        public bool expectedBool { get; private set; }
        public string compareValue { get; private set; }
        public bool hasCompareValue { get; private set; }

        public ShowIfAttribute(string conditionName)
        {
            this.conditionName = conditionName;
            this.expectedBool = true;
            this.compareValue = null;
            this.hasCompareValue = false;
        }

        public ShowIfAttribute(string conditionName, bool expectedBool)
        {
            this.conditionName = conditionName;
            this.expectedBool = expectedBool;
            this.compareValue = null;
            this.hasCompareValue = false;
        }

        public ShowIfAttribute(string conditionName, string compareValue)
        {
            this.conditionName = conditionName;
            this.expectedBool = true;
            this.compareValue = compareValue;
            this.hasCompareValue = true;
        }
    }
}