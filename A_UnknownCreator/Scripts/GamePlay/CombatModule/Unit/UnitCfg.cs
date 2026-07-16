using UnityEngine;
using System.Collections.Generic;
using System;

namespace UnknownCreator.Modules
{
    [Serializable]
    public class UnitCfg
    {
        public string cfgName { internal set; get; }

        [field: SerializeField]
        public string unitName { internal set; get; }

        [field: SerializeField]
        public string modelName { internal set; get; }


        [field: SerializeField]
        public string statsGroupName { internal set; get; }

        [field: SerializeField]
        public string defaultAnimGroupName { internal set; get; }

        [JsonMark]
        [SerializeField]
        internal SerializableDictionary<string, string> animGroup = new();

        [JsonMark]
        [SerializeReference, ShowSerializeReference]
        internal List<IUnitBuilder> builders = new();


        [JsonMark]
        [SerializeField,HideInInspector]
        internal SerializableDictionary<string, IUnitBuilder> builderDict = new();

        public UnitCfg()
        {

        }
    }

}