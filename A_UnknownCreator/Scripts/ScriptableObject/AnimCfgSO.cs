using System;
using UnityEngine;
using System.Collections.Generic;
using Animancer;

namespace UnknownCreator.Modules
{
    public class AnimCfgSO : CustomScriptableObject
    {
        [SerializeField]
        internal List<AnimCfgInfo> cfg = new();
    }


}