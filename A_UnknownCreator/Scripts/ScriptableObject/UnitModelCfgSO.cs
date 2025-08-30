using UnityEngine;

namespace UnknownCreator.Modules
{
    [HideScriptField]
    public class UnitModelCfgSO : CustomScriptableObject
    {
        [SerializeField]
        internal UnitModelCfg cfg = new();
    }

}