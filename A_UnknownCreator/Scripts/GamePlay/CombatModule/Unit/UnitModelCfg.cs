using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    [Serializable]
    public class UnitModelCfg
    {
        /// <summary>
        /// 根据单位模型名称来加载该文件
        /// "路径从Model开始遍历,注意+(Clone)"
        /// </summary>
        
        public List<string> hitBoxList = new();
        public List<BodyPartInfo> bodyPartsList = new();
    }

    [Serializable]
    public class BodyPartInfo
    {
        public int id;
        public string path;
    }
}