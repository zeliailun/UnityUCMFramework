using System.Collections.Generic;
using System;
using UnityEngine;

namespace UnknownCreator.Modules
{
    [Serializable]
    public struct AbilityKV
    {
        [Tooltip("默认值")]
        public List<double> baseValue;

        [Tooltip("天赋名称")]
        public string talentName;

        [Tooltip("天赋值计算类型")]
        public TalentCalcType calcType;

        [Tooltip("isOverrideValue为true时，会使用talentValues的值而不是天赋本身的值")]
        public bool isOverrideValue;

        [Tooltip("覆盖的值")]
        public List<double> talentValues;


        [Tooltip("isOverrideValue为false时该选项才会生效，然后使用下面talentValueName获取值")]
        public bool isBaseOrStat;

        [Tooltip("获取值的名称")]
        public string talentValueName;
    }

    [Serializable]
    public struct AbilityStatsKV
    {
        public AbilityKV abilityKV;
    }

    [Serializable]
    public struct AbilityObjectKV
    {
        [SerializeReference, ShowSerializeReference]
        public object data;
    }

    public enum TalentCalcType
    {
        LinearAdd,    // 直接加
        PercentAdd    // 按百分比加
    }
}