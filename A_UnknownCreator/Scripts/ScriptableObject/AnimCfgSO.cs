using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace UnknownCreator.Modules
{
    [HideScriptField]
    public class AnimCfgSO : CustomScriptableObject
    {
        [SerializeField]
        internal List<AnimCfgInfo> cfg = new();
    }

    [Serializable]
    public class AnimCfgInfo
    {
        public bool isRandom;
        //public bool isUseLayer;
        public string animName;
        [SerializeField]
        public List<AnimAsset> assets = new();
    }

    [Serializable]
    public class AnimAsset
    {
        [OpenInspector]
        public TransitionAsset asset;

        public string animKey;
        public float baseSpeed = 1;
    }
}