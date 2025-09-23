using System.Collections.Generic;
using System;
using UnityEngine;

namespace UnknownCreator.Modules
{


    public enum TalentCalcType
    {
        LinearAdd,    // 直接加
        PercentAdd    // 按百分比加
    }

    public interface IAbilityKV
    {
        List<double> baseValue { get; set; }
        string talentName { get; set; }
        TalentCalcType calcType { get; set; }
        bool isOverrideValue { get; set; }
        List<double> talentValues { get; set; }
        bool isBaseOrStat { get; set; }
        string talentValueName { get; set; }
    }


    [Serializable]
    public class AbilityKV:IAbilityKV
    {
        [Tooltip("默认值")]
        [field: SerializeField]
        public List<double> baseValue { get; set; }

        [Tooltip("天赋名称")]
        [field:SerializeField]
        public string talentName { get; set; }

        [Tooltip("天赋值计算类型")]
        [field: SerializeField]
        public TalentCalcType calcType { get; set; }

        [Tooltip("isOverrideValue为true时，会使用talentValues的值而不是天赋本身的值")]
        [field: SerializeField]
        public bool isOverrideValue { get; set; }

        [Tooltip("覆盖的值")]
        [field: SerializeField]
        public List<double> talentValues { get; set; }


        [Tooltip("isOverrideValue为false时该选项才会生效，然后使用下面talentValueName获取值")]
        [field: SerializeField]
        public bool isBaseOrStat { get; set; }

        [Tooltip("获取值的名称")]
        [field: SerializeField]
        public string talentValueName { get; set; }
    }

    [Serializable]
    public class AbilityStatsKV
    {
        public AbilityKV abilityKV;
    }


}