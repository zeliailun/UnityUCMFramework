using UnityEngine;
using System;
using System.Diagnostics;


namespace UnknownCreator.Modules
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class EnableIfAttribute : PropertyAttribute
    {
        public string conditionName { get; private set; }
        public bool expectedBool { get; private set; }
        public string compareValue { get; private set; }
        public bool hasCompareValue { get; private set; }

        /// <summary>
        /// 根据 bool 字段 / 属性 / 方法决定是否启用。
        /// </summary>
        public EnableIfAttribute(string conditionName)
        {
            this.conditionName = conditionName;
            this.expectedBool = true;
            this.compareValue = null;
            this.hasCompareValue = false;
        }

        /// <summary>
        /// 根据 bool 字段 / 属性 / 方法决定是否启用，可指定期望值。
        /// </summary>
        public EnableIfAttribute(string conditionName, bool expectedBool)
        {
            this.conditionName = conditionName;
            this.expectedBool = expectedBool;
            this.compareValue = null;
            this.hasCompareValue = false;
        }

        /// <summary>
        /// 根据 enum / string / 数值的字符串结果判断。
        /// 例如 [EnableIf(nameof(spawnPosType), "Around")]
        /// </summary>
        public EnableIfAttribute(string conditionName, string compareValue)
        {
            this.conditionName = conditionName;
            this.expectedBool = true;
            this.compareValue = compareValue;
            this.hasCompareValue = true;
        }
    }

}
