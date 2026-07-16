using System.Collections.Generic;
using UnityEngine;
using Animancer;
using System;
using Unity.Properties;
namespace UnknownCreator.Modules
{
    public abstract partial class AbilityBase : IReference
    {

        public AbilityCfg abilityCfg { get; private set; }

        public Unit owner { get; private set; }

        public Unit selectedTarget { get; private set; }

        public Vector3 selectedPos { get; private set; }

        public AbTriggerMode modeCache { get; private set; }

        public long abilityID { get; private set; }

        public string abName { get; private set; }

        [CreateProperty]
        public int level
        {
            get => lv;
            set
            {
                var oldLv = lv;
                var newLv = Math.Clamp(value, 0, maxlevel + 1);
                if (newLv != lv)
                {
                    lv = newLv;
                    GameEvtBus.Send<EvtAbilityLevelChanged>(new EvtAbilityLevelChanged(this, owner, oldLv, lv));
                }
            }
        }

        public int maxlevel
        {
            get => maxlv;
            set
            {
                maxlv = value < 1 ? 1 : value;
                if (maxlv < level) level = maxlv;
            }
        }

        public int currentCharge
        {
            get => currCharge;
            set
            {
                var oldCharge = currCharge;
                var newCharge = value;
                if (newCharge != oldCharge)
                {
                    currCharge = value;
                    GameEvtBus.Send<EvtAbilityChargeChanged>(new(this, owner, currCharge));
                }
            }
        }


        public int index { private set; get; }

        public double currentCd { private set; get; }

        public bool isFirstChargeCooldown { private set; get; }

        public bool isRelease { private set; get; }

        private Dictionary<string, List<StatData>> statsKVDict = new();
        private List<StatData> statsKVList = new();
        private Texture2D icon;
        private AnimPlayer ap;
        private AnimancerState castAnimState;
        private AvatarMask avatarMask;
        private string castAnimName, newCastAnimName, passiveName;
        private BuffBase passiveBuff;
        private Action<TimerCountCycle> castPointAct, castBackswingAct;
        private TimerHandle<TimerCountCycle> timerCastPoint, timerCastBackswing;
        private int frozenCooldown, lv, maxlv, currCharge;
        private bool lastOwnerAliveState;
        private bool hasCheckedOwnerAliveState;


        internal void InitAbility(Unit owner, int index, string abName, string cfgName)
        {
            abilityID = GlobalID.GetUniqueID();

            this.owner = owner;
            this.abName = abName;
            this.index = index;
            abilityCfg = Mgr.JD.GetData<Dictionary<string, AbilityCfg>>(JsonCfgKeyGlobals.AbilityJson)[string.IsNullOrWhiteSpace(cfgName) ? abName : cfgName];

            maxlevel = abilityCfg.maxLevel;
            level = abilityCfg.startLevel;

            //加载默认施法动画
            ap = Mgr.RPool.Load<AnimPlayer>();
            if (!string.IsNullOrWhiteSpace(abilityCfg.animKey))
            {
                ap.SetAnimAsset(abilityCfg.animKey);
                castAnimName = abilityCfg.animKey;
            }
            else
            {
                castAnimName = string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(abilityCfg.maskKey))
                avatarMask = UnityGlobals.LoadSync<AvatarMask>(abilityCfg.maskKey);
            else
                avatarMask = null;


            var statsCfgDict =
                Mgr.JD.GetData<Dictionary<string, StatsCfg>>(
                    JsonCfgKeyGlobals.StatsJson);

            var addedStatsNames = new HashSet<string>();
            var addingStatsNames = new HashSet<string>();

            foreach (var kv in abilityCfg.statsKV)
            {
                AddAbilityStatsByDependency(
                    kv.Key,
                    statsCfgDict,
                    addedStatsNames,
                    addingStatsNames);
            }


            currentCharge = GetCharge(level);
            castPointAct = EndCastPoint;
            castBackswingAct = EndCastBackswing;
            currentCd = frozenCooldown = 0;
            lastOwnerAliveState = false;
            hasCheckedOwnerAliveState = false;
            isRelease = isFirstChargeCooldown = false;
        }

        internal void UpdateAbility()
        {
            if (isRelease) return;

            UpdateCooldown();
            UpdatePassive();
            OnUpdate();
            UpdateDeathState();
        }

        private void UpdateCooldown()
        {
            if (isFrozenCooldown)
                return;

            var chargeLimit = GetCharge(level);
            if (currentCharge > chargeLimit)
                currentCharge = chargeLimit;

            var enableCharge = IsEnableCharge();
            var hasChargeLogic = enableCharge && currentCharge < chargeLimit;

            if (!isCooldownReady || hasChargeLogic)
            {
                currentCd = Math.Max(0, currentCd - CustomTime.DeltaTime());
                GameEvtBus.Send<EvtAbilityCooldownCalculate>(new(this, owner, currentCd));
                if (currentCd <= 0 && hasChargeLogic)
                {
                    ++currentCharge;

                    if (currentCharge != chargeLimit)
                    {
                        currentCd = GetCooldown(level);
                        GameEvtBus.Send<EvtAbilityCooldownStart>(new(this, owner, 0, currentCd));
                    }
                    else
                    {
                        isFirstChargeCooldown = false;
                    }
                }
            }
        }


        private void UpdatePassive()
        {
            passiveName = GetCurrentPassiveName();

            if (string.IsNullOrWhiteSpace(passiveName))
            {
                RemovePassiveBuff();
                return;
            }

            if (passiveBuff != null && passiveBuff.buffName == passiveName)
                return;

            RemovePassiveBuff();

            passiveBuff = owner.buffC.AddPermanentBuff(passiveName, this, owner);

            if (passiveBuff != null)
                passiveBuff.isPassive = true;
        }

        private void UpdateDeathState()
        {
            bool currentAlive = owner.isAlive;

            // 第一次检查
            if (!hasCheckedOwnerAliveState)
            {
                hasCheckedOwnerAliveState = true;
                lastOwnerAliveState = currentAlive;

                if (currentAlive)
                {
                    OnOwnerRespawn();
                }
                else
                {
                    OnOwnerDead();
                }

                return;
            }

            // 没变化，不处理
            if (currentAlive == lastOwnerAliveState)
                return;

            lastOwnerAliveState = currentAlive;

            if (!currentAlive)
            {
                owner.abilityC?.InterruptAbility(this);
                OnOwnerDead();
            }
            else
            {
                OnOwnerRespawn();
            }
        }

        /// <summary>
        /// 根据 StatsCfg 的最小值、最大值依赖顺序添加统计。
        /// </summary>
        private void AddAbilityStatsByDependency(string statsName,Dictionary<string, StatsCfg> statsCfgDict,HashSet<string> addedStatsNames,HashSet<string> addingStatsNames)
        {
            if (string.IsNullOrWhiteSpace(statsName))
                return;

            // 当前能力已经完整添加过这个统计。
            if (addedStatsNames.Contains(statsName))
                return;

            // 当前 AbilityCfg 没有配置该统计。
            //
            // 例如 StatsCfg 设置了 minStatsName，
            // 但当前能力没有 MinFiringInterval 数据，
            // 那么不创建动态最小统计，StatData 会使用固定 minValue。
            if (!abilityCfg.statsKV.TryGetValue(
                    statsName,
                    out var abilityKV))
            {
                return;
            }

            if (!statsCfgDict.TryGetValue(
                    statsName,
                    out StatsCfg statsCfg))
            {
                UCMDebug.LogWarning(
                    $"{abName} 添加统计失败，没有找到 StatsCfg：{statsName}");

                return;
            }

            // 防止配置循环：
            //
            // A.minStatsName = B
            // B.maxStatsName = A
            if (!addingStatsNames.Add(statsName))
            {
                UCMDebug.LogWarning(
                    $"{abName} 检测到统计循环依赖：{statsName}");

                return;
            }

            // =========================================================
            // 1. 优先添加最小值统计
            // =========================================================

            AddAbilityStatsByDependency(
                statsCfg.minStatsName,
                statsCfgDict,
                addedStatsNames,
                addingStatsNames);

            // =========================================================
            // 2. 优先添加最大值统计
            // =========================================================

            // 最小值和最大值统计名称相同时，不重复处理。
            if (!string.Equals(
                    statsCfg.maxStatsName,
                    statsCfg.minStatsName,
                    StringComparison.Ordinal))
            {
                AddAbilityStatsByDependency(
                    statsCfg.maxStatsName,
                    statsCfgDict,
                    addedStatsNames,
                    addingStatsNames);
            }

            // =========================================================
            // 3. 最后添加当前统计
            // =========================================================

            if (!statsKVDict.TryGetValue(
                    statsName,
                    out var statsList))
            {
                statsList = new List<StatData>();
                statsKVDict.Add(statsName, statsList);
            }

            if (abilityKV.baseValue == null ||
                abilityKV.baseValue.Count < 1)
            {
                AddAbilityStat(
                    statsCfg,
                    0,
                    statsList);
            }
            else
            {
                for (int i = 0;
                     i < abilityKV.baseValue.Count;
                     i++)
                {
                    AddAbilityStat(
                        statsCfg,
                        abilityKV.baseValue[i],
                        statsList);
                }
            }

            addingStatsNames.Remove(statsName);
            addedStatsNames.Add(statsName);
        }

        /// <summary>
        /// 添加一条能力统计，并记录到能力自身的统计集合中。
        /// </summary>
        private void AddAbilityStat(StatsCfg statsCfg,double baseValue,List<StatData> statsList)
        {
            StatData stat = owner.statsC.AddStats(
                statsCfg,
                baseValue,
                this);

            if (stat == null)
                return;

            statsList.Add(stat);
            statsKVList.Add(stat);
        }

        void IReference.ObjRelease()
        {
            if (isRelease) return;
            isRelease = true;

            owner.abilityC.InterruptAbility(this);

            OnRelease();

            ResetCurrentTriggerMode();

            RemovePassiveBuff();

            for (int i = 0; i < statsKVList.Count; i++)
                owner.statsC.RemoveStats(statsKVList[i]);
            statsKVList.Clear();
            statsKVDict.Clear();

            if (icon != null)
            {
                UnityGlobals.Release(icon);
                icon = null;
            }

            if (avatarMask != null)
            {
                UnityGlobals.Release(avatarMask);
                avatarMask = null;
            }

            Mgr.RPool.Release(ap);
            ap = null;

            castAnimState = null;
            castAnimName = string.Empty;
            newCastAnimName = string.Empty;
            passiveName = string.Empty;
            castPointAct = null;
            castBackswingAct = null;
            selectedTarget = null;
            owner = null;
            abilityCfg = null;
        }

        void IReference.ObjDestroy() { OnPoolObjDestroy(); }
    }
}