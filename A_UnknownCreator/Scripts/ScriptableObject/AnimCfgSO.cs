using UnityEngine;
using System.Collections.Generic;

namespace UnknownCreator.Modules
{
    [HideScriptField]
    public class AnimCfgSO : CustomScriptableObject
    {
        [SerializeField]
        internal List<AnimCfgInfo> cfg = new();
    }


}