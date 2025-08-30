using System;
using UnityEngine;

namespace UnknownCreator.Modules
{
    [HideScriptField]
    public class SoundCfgSO : CustomScriptableObject
    {
        [field: SerializeField]
        public SoundCfg cfg { internal set; get; } = new();


    }


}