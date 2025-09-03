using UnityEditor;
using UnityEngine;

namespace UnknownCreator.Modules
{
    [HideScriptField]
    public class AbilityCfgSO : CustomScriptableObject
    {
#if UNITY_EDITOR

        [SerializeReference]
        public MonoScript cfgScript;

        internal Texture2D icon;

#endif
        [field: SerializeField]
        public AbilityCfg cfg { internal set; get; } = new();







        public override void OnEnable()
        {
            ChangeValue();
        }

        public override void OnValidate()
        {
            base.OnValidate();
            ChangeValue();
        }

        private void ChangeValue()
        {
            if (cfg.startLevel > cfg.maxLevel)
                cfg.maxLevel = cfg.startLevel;

            cfg.cfgName = CachedSoName;

            cfg.baseKV ??= new();
            cfg.statsKV ??= new();
            cfg.dataKV ??= new();


            //添加默认统计
            SetStatsKV(AbilityGlobals.StatCooldown);
            SetStatsKV(AbilityGlobals.StatCastRange);
            SetStatsKV(AbilityGlobals.StatCastRangeBuffer);
            SetStatsKV(AbilityGlobals.StatCastPoint);
            SetStatsKV(AbilityGlobals.StatCharge);

#if UNITY_EDITOR

            if (cfg == null || string.IsNullOrWhiteSpace(cfg.icon))
            {
                icon = null;
                return;
            }

            if (icon == null || icon.name != cfg.icon) 
                icon = UnityEditorGlobals.GetAsset<Texture2D>(cfg.icon);
#endif
        }


        private void SetStatsKV(string soName)
        {
            if (!cfg.statsKV.TryGetValue(soName, out _))
                cfg.statsKV[soName] = new(); ;
        }

    }



}
