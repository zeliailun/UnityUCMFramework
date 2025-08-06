using System.Collections.Generic;
using System;
using UnityEngine;

namespace UnknownCreator.Modules
{
    [Serializable]
    public struct AbilityKV
    {
        public List<double> value;
        public TalentCalcType calcType;

        public bool isOverrideValue;
        public List<double> talentValues;

        public string talentName;
        public bool isBaseOrStat;
        public string talentkey;
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