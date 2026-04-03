using UnityEngine;
using System.Collections.Generic;

namespace UnknownCreator.Modules
{
    [HideScriptField]
    public class StatsCfgSO : CustomScriptableObject
    {
        [Space(5)]
        [Info("ID必须唯一 ,警惕互相Link")]
        [SerializeField]
        internal StatsCfg cfg = new();

        public bool isPInf;
        public bool isNInf;

        // 编辑器下用于缓存所有配置实例（用于循环检测）
#if UNITY_EDITOR
        private static List<StatsCfgSO> allConfigs = new();
#endif

        public override void OnEnable()
        {
            base.OnEnable();
            Default();

#if UNITY_EDITOR
            if (!allConfigs.Contains(this))
                allConfigs.Add(this);
#endif
        }

        public  void OnDisable()
        {
#if UNITY_EDITOR
            if (allConfigs.Contains(this))
                allConfigs.Remove(this);
#endif
        }

        public override void OnValidate()
        {
            base.OnValidate();
            Default();

            if (isPInf)
                cfg.maxValue = double.PositiveInfinity;

            if (isNInf)
                cfg.minValue = double.NegativeInfinity;

#if UNITY_EDITOR
            // 延迟检测，避免频繁触发
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                ValidateLinks();
            };
#endif
        }

        private void Default()
        {
            if (cfg == null) return;

            if (!ReferenceEquals(name, cfg.idName))
                cfg.idName = name;

            if (cfg.minStatsName == cfg.idName)
                cfg.minStatsName = string.Empty;

            if (cfg.maxStatsName == cfg.idName)
                cfg.maxStatsName = string.Empty;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 验证联动配置
        /// </summary>
        private void ValidateLinks()
        {
            // 检查自己联动自己
            if (cfg.linkNames.Contains(cfg.idName))
            {
                UCMDebug.LogError($"[StatsCfgSO] {cfg.idName} 不能联动自己");
            }

            // 检查联动的目标是否存在（在所有配置中查找）
            var allIds = GetAllConfigIds();

            foreach (var linkName in cfg.linkNames)
            {
                if (!allIds.Contains(linkName))
                {
                    UCMDebug.LogWarning($"[StatsCfgSO] {cfg.idName} 联动的目标 {linkName} 不存在");
                }
            }

            // 检测循环联动
            DetectCircularLinks();
        }

        /// <summary>
        /// 获取所有配置的ID
        /// </summary>
        private HashSet<string> GetAllConfigIds()
        {
            var ids = new HashSet<string>();
            foreach (var so in allConfigs)
            {
                if (so != null && so.cfg != null && !string.IsNullOrEmpty(so.cfg.idName))
                {
                    ids.Add(so.cfg.idName);
                }
            }
            return ids;
        }

        /// <summary>
        /// 检测循环联动
        /// </summary>
        private void DetectCircularLinks()
        {
            // 构建 id -> StatsCfg 映射
            var cfgDict = new Dictionary<string, StatsCfg>();
            foreach (var so in allConfigs)
            {
                if (so != null && so.cfg != null && !string.IsNullOrEmpty(so.cfg.idName))
                {
                    cfgDict[so.cfg.idName] = so.cfg;
                }
            }

            // 只检测当前配置的联动链
            foreach (var linkName in cfg.linkNames)
            {
                if (IsCircular(cfg.idName, linkName, cfgDict))
                {
                    UCMDebug.LogError($"[StatsCfgSO] 检测到循环联动: {cfg.idName} -> {linkName} -> ... -> {cfg.idName}");
                    break; // 找到一个循环就够了
                }
            }
        }

        /// <summary>
        /// DFS检测循环
        /// </summary>
        private bool IsCircular(string start, string current, Dictionary<string, StatsCfg> cfgDict, HashSet<string> visited = null)
        {
            visited ??= new HashSet<string>();

            if (visited.Contains(current)) return false;
            visited.Add(current);

            if (current == start) return true;

            if (!cfgDict.TryGetValue(current, out var cfg)) return false;

            foreach (var next in cfg.linkNames)
            {
                if (IsCircular(start, next, cfgDict, visited))
                    return true;
            }
            return false;
        }
#endif
    }
}