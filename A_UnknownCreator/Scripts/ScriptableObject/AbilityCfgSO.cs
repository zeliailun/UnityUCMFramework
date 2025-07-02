using UnityEngine;

namespace UnknownCreator.Modules
{
    public class AbilityCfgSO : CustomScriptableObject
    {
        [TextArea]
        public string desc;

        [field: SerializeField]
        public AbilityCfg cfg { internal set; get; } = new();

        internal Texture2D icon;




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

            cfg.cfgName = cfgName;

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

            if (icon != null && icon.name == cfg.icon) return;

            icon = EditorUtils.GetAsset<Texture2D>(cfg.icon);
#endif
        }


        private void SetStatsKV(string soName)
        {
            if (!cfg.statsKV.TryGetValue(soName, out _))
                cfg.statsKV[soName] = new(); ;
        }

    }



}
