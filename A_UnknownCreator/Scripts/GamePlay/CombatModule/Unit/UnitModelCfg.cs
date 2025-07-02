using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnknownCreator.Modules
{
    [Serializable]
    public class UnitModelCfg
    {
        /// <summary>
        /// ���ݵ�λģ�����������ظ��ļ�
        /// "·����Model��ʼ����,ע��+(Clone)"
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