using System.Collections.Generic;
using System;
using UnityEngine;

namespace UnknownCreator.Modules
{
    [Serializable]
    public struct AbilityKV
    {
        [Header("基础值")]
        public List<double> value;

        [Header("天赋名称")]
        public string talentName;

        [Header("天赋值计算类型")]
        public TalentCalcType calcType;

        [Header("覆盖天赋原有数值？")]
        [Tooltip("isOverrideValue为true时，会使用talentValues的值而不是天赋本身的值")]
        public bool isOverrideValue;

        [Header("覆盖的值")]
        public List<double> talentValues;

        [Header("使用基础值还是统计值？")]
        [Tooltip("isOverrideValue为false时该选项才会生效，然后使用下面talentValueName获取值")]
        public bool isBaseOrStat;

        [Header("获取值的名称")]
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