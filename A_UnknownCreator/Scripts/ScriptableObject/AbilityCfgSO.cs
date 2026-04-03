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


#if UNITY_EDITOR
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




            if (cfg != null)
            {
                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

                if (settings != null)
                {
                    cfg.icon = GetAddress(settings, icon);
                    cfg.animKey = GetAddress(settings, animClip);
                    cfg.maskKey = GetAddress(settings, mask);
                }
            }


        }

        private string GetAddress(AddressableAssetSettings settings, AssetReference reference)
        {
            if (reference == null)
                return null;

            var guid = reference.AssetGUID;
            if (string.IsNullOrEmpty(guid))
                return null;

            var entry = settings.FindAssetEntry(guid, true);
            return entry != null ? entry.address : null;
        }


        private void SetStatsKV(string soName)
        {
            if (!cfg.statsKV.TryGetValue(soName, out _))
                cfg.statsKV[soName] = new AbilityKV();
        }

#endif

    }



}
