using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    [Serializable]
    public class UnitModelCfg
    {
        /// <summary>
        /// 根据名称来加载该文件
        /// "路径从Model开始遍历,生成的模型对象尾名需要+(Clone)"
        /// </summary>

        [field: SerializeField]
        public string model { private set; get; }

        [field: SerializeField]
        public HitVfxInfo hitVfx { private set; get; }

        [field: SerializeField]
        public string hitSound { private set; get; }

        public List<string> hitBoxList = new();
        public List<BodyPartInfo> bodyPartsList = new();
    }

    [Serializable]
    public class BodyPartInfo
    {
#if UNITY_EDITOR
        public string desc;
#endif
        public int id;
        public string path;
    }
}