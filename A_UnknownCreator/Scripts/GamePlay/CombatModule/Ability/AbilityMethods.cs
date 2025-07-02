using System;
using System.Collections.Generic;
using Animancer;

namespace UnknownCreator.Modules
{
    public abstract partial class AbilityBase
    {
        public void ResetCurrentTriggerMode()
        {
            if (modeCache == AbTriggerMode.Released)
                modeCache = AbTriggerMode.Pressed;
        }


        public T GetDataValue<T>(string name) where T : class
        => abilityCfg.dataKV.TryGetValue(name, out var result) ? result.data as T : null;

        public double GetValue(string name)
        => GetValue(name, level);

        public double GetValue(string name, int lv)
        {
            if (!abilityCfg.baseKV.TryGetValue(name, out var akv) ||
                lv <= 0 ||
                akv.value is null ||
                lv > akv.value.Count)
            {
                UCMDebug.Log("获取不到:" + name + "的值");
                return 0F;
            }

            double baseValue = akv.value[lv - 1];

            if (owner.talentC.HasTalent(akv.talentKV.talentName))
                baseValue += akv.talentKV.talentValue;

            return baseValue;
        }

        public double GetStatValue(string valueName)
        => GetStatValue(valueName, level);

        public double GetStatValue(string valueName, int lv)
        {
            if (lv <= 0 || !statsKV.TryGetValue(valueName, out var akv) || lv > akv.Count) return 0F;
            var askv = abilityCfg.statsKV[valueName];
            if (owner.talentC.HasTalent(askv.abilityKV.talentKV.talentName))
            {
                foreach (var sd in akv)
                    sd.AddByName(askv.abilityKV.talentKV.talentName, CalcType.LinearAdd, askv.abilityKV.talentKV.talentValue);
            }
            else
            {
                foreach (var sd in akv)
                    sd.Remove(askv.abilityKV.talentKV.talentName, CalcType.LinearAdd);
            }
            return akv[lv - 1].finalValue;
        }

        public void ChangeStatValue(string statsName, double value, bool isReplace)
        {
            if (statsKV.TryGetValue(statsName, out var akv))
            {
                StatData result;
                for (int i = 0; i < akv.Count; i++)
                {
                    result = akv[i];
                    if (isReplace)
                        result.baseValue = value;
                    else
                        result.baseValue += value;
                }
            }
        }

        public double GetCastPoint()
        => GetCastPoint(level);

        public double GetCastRange()
        => GetCastRange(level);

        public double GetCastRangeBuffer()
        => GetCastRangeBuffer(level);

        public double GetCastRangeAndBuffer()
        => GetCastRange() + GetCastRangeBuffer();

        public double GetCooldown()
        => GetCooldown(level);



        public double GetCharge()
        => GetCharge(level);

        public void StartCooldown()
        {
            StartCooldown(GetCooldown());
        }

        public void StartCooldown(double cooldown)
        {
            if (isFrozenCooldown) return;

            if (IsEnableCharge())
            {
                if (cooldown <= 0) return;

                if (currentCharge > 0)
                    --currentCharge;

                if (!isFirstChargeCooldown)
                {
                    isFirstChargeCooldown = true;
                    ResetCooldown(cooldown);
                }
            }
            else
            {
                ResetCooldown(cooldown);
            }
        }

        public void EndCooldown()
        {
            if (isFrozenCooldown) return;

            if (!isCooldownReady)
            {
                currentCd = 0;
                Mgr.Event.Send<AbilityBase>(this, CombatEvtGlobals.OnAbilityCooldownCalc);
            }

            var charge = GetCharge();
            if (IsEnableCharge() && currentCharge < charge)
                currentCharge = (int)charge;

        }

        public void AddFrozenCooldown()
        => ++frozenCooldown;

        public void RemoveFrozenCooldown()
        => --frozenCooldown;

        public bool HasBehavior(AbBehavior behavior)
        => (GetBehaviorType() & behavior) == behavior;

        public bool HasOnlyBehavior(AbBehavior behavior)
        {
            var behaviorType = GetBehaviorType();
            return behaviorType == behavior && behaviorType != 0;
        }

        public bool HasTargetTeam(AbTargetTeam targetTeam)
        => (GetTargetTeamType() & targetTeam) == targetTeam;

        public bool HasOnlyTargetTeam(AbTargetTeam targetTeam)
        {
            var targetTeamType = GetTargetTeamType();
            return targetTeamType == targetTeam && targetTeamType != 0;
        }

        public bool HasFlags(AbFlags flag)
        => (GetFlagsType() & flag) == flag;

        public bool HasOnlyFlags(AbFlags flag)
        {
            var flags = GetFlagsType();
            return flags == flag && flags != 0;
        }

        public bool IsEnoughCastRange(float distance)
        {
            return distance < GetCastRange(level);
        }

        public bool canCalcCooldown
        => !isFrozenCooldown && (!isCooldownReady || (IsEnableCharge() && currentCharge < 1));


        public bool isIgnoreCastRange
        => HasBehavior(AbBehavior.Immediate) || HasBehavior(AbBehavior.NotTarget) ||
           (!HasBehavior(AbBehavior.Target) && !HasBehavior(AbBehavior.Point));

        public bool isStunnedCast
        => !owner.stateC.BeState(StateGlobals.Stunned) ||
            (owner.stateC.BeState(StateGlobals.Stunned) && HasFlags(AbFlags.IgnoreStunned));

        public bool isSilencedCast
        => !owner.stateC.BeState(StateGlobals.Silenced) ||
            (owner.stateC.BeState(StateGlobals.Silenced) && HasFlags(AbFlags.IgnoreSilence));

        public bool isCooldownReady
        => currentCd <= 0;

        public bool isFrozenCooldown
        => frozenCooldown > 0;

        public bool isLevelReady
        => level > 0;

        public bool isChargeReady
        => IsEnableCharge() &&
           currentCharge > 0;

        public bool isFullyCastable
        => owner.isAlive && isLevelReady && (IsEnableCharge() ? currentCharge > 0 : isCooldownReady);


        public bool isFaceTargetPoint
        => IsForceCastDir() &&
           !HasBehavior(AbBehavior.Immediate) &&
           !HasBehavior(AbBehavior.NotTarget) &&
           (HasBehavior(AbBehavior.Target) || HasBehavior(AbBehavior.Point));

        public bool isCastAnimPlaying
        => ap.isPlaying;


        public bool isNullAbility => abName == AbilityGlobals.AbilityNull;

        public bool hasPassive => passiveBuff != null;


        private void ApplyState(int value)
        {
            if (GetDisableState() == null) return;
            foreach (var item in GetDisableState())
                owner.stateC.UpdateState(item, value);
        }

        private void RemovePassiveBuff()
        {
            if (passiveBuff != null)
            {
                owner.buffC.RemoveBuff(passiveBuff);
                passiveBuff = null;
                passiveName = string.Empty;
            }
        }

        private void ResetCooldown(double cooldown)
        {
            var oldCooldown = currentCd;
            currentCd = cooldown;
            Mgr.Event.Send<EvtAbilityCooldownStart>(new EvtAbilityCooldownStart(this, owner, oldCooldown, currentCd), CombatEvtGlobals.OnAbilityCooldownStart);
        }
    }
}