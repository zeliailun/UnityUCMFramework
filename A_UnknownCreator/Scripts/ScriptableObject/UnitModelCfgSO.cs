using UnityEngine;

namespace UnknownCreator.Modules
{
    public class UnitModelCfgSO : CustomScriptableObject
    {
        [SerializeField]
        internal UnitModelCfg cfg = new();
    }

}