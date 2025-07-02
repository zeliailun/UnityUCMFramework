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
                    Mgr.Event.Send<EvtAbilityLevelChanged>(new EvtAbilityLevelChanged(this, owner, oldLv, lv), CombatEvtGlobals.OnAbilityLevelChanged);
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
            private set
            {
                var oldCharge = currCharge;
                var newCharge = value;
                if (newCharge != oldCharge)
                {
                    currCharge = value;
                    Mgr.Event.Send<EvtAbilityChargeChanged>(new EvtAbilityChargeChanged(this, owner, currCharge), CombatEvtGlobals.OnAbilityChargeChanged);
                }
            }
        }

        public int index { private set; get; }

        public double currentCd
        {
            get;
            private set;
        }

        public bool isFirstChargeCooldown { private set; get; }

        public bool isRelease { private set; get; }

        private Dictionary<string, List<StatData>> statsKV = new();
        private Texture2D icon;
        private AnimPlayer ap;
        private AnimancerState castAnimState;
        private string castAnimName, newCastAnimName, passiveName;
        private BuffBase passiveBuff;
        private Action<TimerCountCycle> castPointAct, castBackswingAct;
        private ITimer timerCastPoint, timerCastBackswing;
        private int frozenCooldown, lv, maxlv, currCharge;
        private bool isDie;


        internal void InitAbility(Unit owner, int index, string abName, string cfgName)
        {
            this.owner = owner;
            this.abName = abName;
            this.index = index;
            abilityCfg = Mgr.JD.GetData<Dictionary<string, AbilityCfg>>(JsonCfgNameGlobals.AbilityJson)[string.IsNullOrWhiteSpace(cfgName) ? abName : cfgName];

            level = abilityCfg.startLevel;
            maxlevel = abilityCfg.maxLevel;

            //加载默认施法动画
            ap = Mgr.RPool.Load<AnimPlayer>();
            if (abilityCfg.castAnimAsset != null)
            {
                ap.SetPlayAnim(abilityCfg.castAnimAsset, false);
                castAnimName = abilityCfg.castAnimAsset.name;
            }
            else
            {
                castAnimName = string.Empty;
            }

            //添加统计到组件
            if (abilityCfg.statsKV.Count > 0)
            {
                foreach (var kv in abilityCfg.statsKV)
                {
                    var stCfg = Mgr.JD.GetData<Dictionary<string, StatsCfg>>(JsonCfgNameGlobals.StatsJson)[kv.Key];

                    if (!statsKV.TryGetValue(kv.Key, out var statsList))
                    {
                        statsList = new List<StatData>();
                        statsKV[kv.Key] = statsList;
                    }

                    if (kv.Value.abilityKV.value == null || kv.Value.abilityKV.value.Count < 1)
                        statsList.Add(owner.statsC.AddStats(stCfg, 0, this));
                    else
                        foreach (var value in kv.Value.abilityKV.value)
                            statsList.Add(owner.statsC.AddStats(stCfg, value, this));
                }
            }


            currentCharge = GetCharge(level);
            castPointAct = EndCastPoint;
            castBackswingAct = EndCastBackswing;
            currentCd = frozenCooldown = 0;
            isDie = owner.isAlive;
            isRelease = isFirstChargeCooldown = false;

            OnInitialized();
        }

        internal void UpdateAbility()
        {
            if (isRelease) return;

            //能力点和CD计算
            if (canCalcCooldown)
            {
                if (abName == nameof(AbilityJetpack)) UCMDebug.Log(isCooldownReady);
                currentCd = Math.Max(0, currentCd - CustomTime.DeltaTime());
                Mgr.Event.Send<AbilityBase>(this, CombatEvtGlobals.OnAbilityCooldownCalc);
                var count = GetCharge(level);
                if (IsEnableCharge() && currentCd <= 0 && currentCharge < count)
                {

                    if (++currentCharge != count)
                    {
                        currentCd = GetCooldown(level);
                        Mgr.Event.Send<EvtAbilityCooldownStart>(new EvtAbilityCooldownStart(this, owner, 0, currentCd), CombatEvtGlobals.OnAbilityCooldownStart);
                    }
                    else
                    {
                        isFirstChargeCooldown = false;
                    }
                }
            }

            //被动检查设置
            passiveName = GetCurrentPassiveName();
            if (string.IsNullOrWhiteSpace(passiveName))
            {
                RemovePassiveBuff();
            }
            else if (passiveBuff == null || passiveBuff.buffName != passiveName)
            {
                RemovePassiveBuff();
                passiveBuff = owner.buffC.AddPermanentBuff(passiveName, this, owner);
                if (passiveBuff != null)
                    passiveBuff.isPassive = true;
            }


            OnUpdate();

            //死亡判断
            if (!owner.isAlive && !isDie)
            {
                isDie = true;
                owner.abilityC?.InterruptAbility(this);
                OnOwnerDead();

            }
            else if (owner.isAlive && isDie)
            {
                isDie = false;
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

            foreach (var list in statsKV.Values)
                foreach (var sd in list)
                    owner.statsC.RemoveStats(sd);
            statsKV.Clear();

            if (icon != null)
            {
                UnityGlobals.Release(icon);
                icon = null;
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