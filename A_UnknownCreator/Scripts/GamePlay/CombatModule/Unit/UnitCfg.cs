using UnityEngine;
using System.Collections.Generic;
using System;

namespace UnknownCreator.Modules
{
    [Serializable]
    public class UnitCfg
    {
        [field: SerializeField]
        public string defaultModel { internal set; get; }


        [field: SerializeField]
        public string statsGroup { internal set; get; }

        [field: SerializeField]
        public string defaultAnimGroup { internal set; get; }

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