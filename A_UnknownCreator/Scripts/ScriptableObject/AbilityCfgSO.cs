#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using UnityEngine.AddressableAssets;
using UnityEngine;
using Animancer;


namespace UnknownCreator.Modules
{
    [HideScriptField]
    public class AbilityCfgSO : CustomScriptableObject
    {
#if UNITY_EDITOR

        /// <summary>
        /// 指定的技能脚本，用于编辑器打开
        /// </summary>
        [SerializeReference]
        public MonoScript cfgScript;

        /// <summary>
        /// 用于指定的技能图标,动画，骨骼遮罩，会储存其Addressable的key用于运行时加载
        /// </summary>

        public AssetReferenceT<Texture2D> icon;

        public AssetReferenceT<TransitionAsset> animClip;

        public AssetReferenceT<AvatarMask> mask;



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

            if (cfg != null)
            {
                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

                if (settings != null)
                {
                    cfg.icon = settings.FindAssetEntry(icon?.AssetGUID, true)?.address ?? null;
                    cfg.animKey = settings.FindAssetEntry(animClip?.AssetGUID, true)?.address ?? null;
                    cfg.maskKey = settings.FindAssetEntry(mask?.AssetGUID, true)?.address ?? null;
                }
            }

#endif
        }


        private void SetStatsKV(string soName)
        {
            if (!cfg.statsKV.TryGetValue(soName, out _))
                cfg.statsKV[soName] = new AbilityKV();
        }



    }



}
