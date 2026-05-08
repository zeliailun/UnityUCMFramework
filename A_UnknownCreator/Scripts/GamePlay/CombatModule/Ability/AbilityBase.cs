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
        private ITimer timerCastPoint, timerCastBackswing;
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

            //添加统计到组件
            foreach (var kv in abilityCfg.statsKV)
            {
                var stCfg = Mgr.JD.GetData<Dictionary<string, StatsCfg>>(JsonCfgKeyGlobals.StatsJson)[kv.Key];

                if (!statsKVDict.TryGetValue(kv.Key, out var statsList))
                {
                    statsList = new List<StatData>();
                    statsKVDict[kv.Key] = statsList;
                }


                if (kv.Value.baseValue == null || kv.Value.baseValue.Count < 1)
                {
                    var stat = owner.statsC.AddStats(stCfg, 0, this);


                    statsList.Add(stat);
                    statsKVList.Add(stat);
                }
                else
                {
                    for (int x = 0; x < kv.Value.baseValue.Count; x++)
                    {
                        double value = kv.Value.baseValue[x];
                        var stat = owner.statsC.AddStats(stCfg, value, this);
                        statsList.Add(stat);
                        statsKVList.Add(stat);
                    }

                }
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
    }
}