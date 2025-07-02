using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace UnknownCreator.Modules
{
    [Serializable]
    public class AbilityCfg
    {
        public string cfgName { internal set; get; }

        [field: OpenInspector]
        [field: SerializeField]
        public string icon { internal set; get; }

        [field: SerializeField]
        public int abilityType { internal set; get; }

        [field: SerializeField]
        public AbBehavior abilityCastType { internal set; get; } = AbBehavior.None;

        [field: SerializeField]
        public AbTargetTeam abilityTargetTeamType { internal set; get; } = AbTargetTeam.None;

        [field: SerializeField]
        public AbFlags abilityFlags { internal set; get; } = AbFlags.None;

        [field: SerializeField]
        public AbTriggerMode useMode { internal set; get; } = AbTriggerMode.Pressed;

        [field: SerializeField]
        public int[] stateID { internal set; get; } = Array.Empty<int>();

        [field: OpenInspector]
        [field: SerializeField]
        public TransitionAsset castAnimAsset { internal set; get; }

        [field: OpenInspector]
        [field: SerializeField]
        public AvatarMask mask { internal set; get; }

        [field: SerializeField]
        public int animLayers { internal set; get; } = 1;

        [field: SerializeField]
        public int animEndWeight { internal set; get; } = 0;

        [field: SerializeField]
        public float animStartFadeDuration { internal set; get; } = 0.25F;

        [field: SerializeField]
        public float animEndFadeDuration { internal set; get; } = 0.25F;

        [field: SerializeField]
        public float animTriggerTime { internal set; get; }

        [field: SerializeField]
        public bool isForceAnimSp { internal set; get; }

        [field: SerializeField]
        public float animSp { internal set; get; } = 1F;

        [field: SerializeField]
        public string defaultPassiveName { internal set; get; }

        [field: SerializeField]
        public bool isRefreshable { internal set; get; } = true;

        [field: SerializeField]
        public bool isForceCastDir { internal set; get; } = false;


        [field: SerializeField]
        public int startLevel { internal set; get; } = 1;

        [field: SerializeField]
        public int maxLevel { internal set; get; } = 1;

        [field: SerializeField]
        public bool isEnableCharge { internal set; get; }

        [field: SerializeField]
        public bool isGamePauseCast { internal set; get; }

        [JsonMark]
        [field: SerializeField]
        public SerializableDictionary<string, AbilityKV> baseKV { internal set; get; } = new();
        [JsonMark]
        [field: SerializeField]
        public SerializableDictionary<string, AbilityStatsKV> statsKV { internal set; get; } = new();
        [JsonMark]
        [field: SerializeField]
        public SerializableDictionary<string, AbilityObjectKV> dataKV { internal set; get; } = new();


    }

}