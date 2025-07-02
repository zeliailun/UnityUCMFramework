using System.Collections.Generic;
using System;
using UnityEngine;

namespace UnknownCreator.Modules
{
    [Serializable]
    public struct AbilityKV
    {
        public List<double> value;
        public TalentKV talentKV;
    }

    [Serializable]
    public struct AbilityStatsKV
    {
        public AbilityKV abilityKV;
    }

    [Serializable]
    public struct TalentKV
    {
        public string talentName;
        public float talentValue;
    }


    [Serializable]
    public struct AbilityObjectKV
    {
        [SerializeReference, ShowSerializeReference]
        public object data;
    }
}