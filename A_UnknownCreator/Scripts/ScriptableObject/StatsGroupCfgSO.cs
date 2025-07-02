using System.Collections.Generic;
using UnityEngine;
namespace UnknownCreator.Modules
{
    public class StatsGroupCfgSO : CustomScriptableObject
    {
        [SerializeField]
        internal List<OverrideStats> cfg = new();
    }
}





