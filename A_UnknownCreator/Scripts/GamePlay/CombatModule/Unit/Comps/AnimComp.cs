using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;
namespace UnknownCreator.Modules
{
    public class AnimComp : StateComp
    {
        public AnimancerComponent anim { private set; get; }

        public string currentGroup { get; private set; }

        public bool isAnimancerReady
        => anim != null && anim.isActiveAndEnabled;

        private Dictionary<string, List<AnimCfgInfo>> groupCfgDict = new();
        private Dictionary<string, AnimPlayer> apDict = new();
        private List<AnimPlayer> apList = new();
        private Unit self;

        public override void InitComp()
        {
            self = kv.GetValue<Unit>();

            if (self.unitCfg.animGroup?.Count > 0)
            {
                foreach (var item in self.unitCfg.animGroup)
                {
                    groupCfgDict.TryAdd(item.Key, Mgr.JD.GetData<Dictionary<string, List<AnimCfgInfo>>>(JsonCfgKeyGlobals.AnimJson)[item.Value]);
                }
            }
            ChangeAnimGroup(self.unitCfg.defaultAnimGroupName);
        }

        public override void ReleaseComp()
        {
            groupCfgDict.Clear();
            apDict.Clear();

            for (int i = 0; i < apList.Count; i++)
                Mgr.RPool.Release(apList[i]);
            apList.Clear();

            currentGroup = string.Empty;
            anim = null;
            self = null;
        }

        public AnimPlayer GetAnimationPlayer(string name)
        => apDict.TryGetValue(name, out var result) ? result : null;

        public void ChangeAnimGroup(string group)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                UCMDebug.LogWarning("没有动画配置文件");
                return;
            }

            if (groupCfgDict.TryGetValue(group, out var result) &&
                (string.IsNullOrWhiteSpace(currentGroup) || currentGroup != group))
            {
                foreach (AnimCfgInfo item in result)
                {
                    if (apDict.TryGetValue(item.animName, out var ap))
                    {
                        ConfigureAnimationPlayer(ap, item);
                    }
                    else
                    {

                        var apNew = Mgr.RPool.Load<AnimPlayer>();
                        ConfigureAnimationPlayer(apNew, item);
                        apDict.Add(item.animName, apNew);
                        apList.Add(apNew);
                    }

                }
                currentGroup = group;
            }
        }

        public void AddAnimGroup(string name, List<AnimCfgInfo> cfg)
        {
            groupCfgDict.TryAdd(name, cfg);
        }

        public void RemoveAnimGroup(string name)
        {
            groupCfgDict.Remove(name);
        }

        internal void SetAnimComp(AnimancerComponent animC)
        {
            this.anim = animC;
            if (anim != null &&
                anim.Layers != null &&
                anim.Layers.Count > 0)
            {
                AnimancerLayer layer = anim.Layers[0];
                if (!layer.ApplyAnimatorIK)
                    layer.ApplyAnimatorIK = true;
                if (!layer.ApplyFootIK)
                    layer.ApplyFootIK = true;
            }

        }

        private void ConfigureAnimationPlayer(AnimPlayer ap, AnimCfgInfo cfg)
        {
            ap.isRandom = cfg.isRandom;
            if (ap.isRandom)
            {
                ap.SetAnimAsset(cfg.assets);
                ap.SetRandomPlayAnim();
            }
            else
            {
                if (cfg.assets == null || cfg.assets.Count == 0)
                {
                    UCMDebug.LogWarning($"动画配置没有资源: {cfg.animName}");
                    return;
                }
                var result = cfg.assets[0];
                ap.SetAnimAsset(result.animKey, result.baseSpeed);
            }
        }
    }




}
