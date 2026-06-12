
using System;

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
        => abilityCfg.dataKV.TryGetValue(name, out var result) ? result as T : null;

        public double GetValue(string name)
        => GetValue(name, level);

        public double GetValue(string name, int lv)
        {
            if (lv <= 0 ||
                string.IsNullOrWhiteSpace(name) ||
                !abilityCfg.baseKV.TryGetValue(name, out var akv) ||
                akv.baseValue == null ||
                lv > akv.baseValue.Count)
                return 0;

            double baseValue = akv.baseValue[lv - 1];

            if (string.IsNullOrWhiteSpace(akv.talentName))
                return baseValue;

            var talent = owner.talentC.GetTalent(akv.talentName);
            if (talent == null || talent.isRelease)
                return baseValue;

            double addValue = 0;

            if (akv.isOverrideValue)
            {
                if (akv.talentValues != null && lv <= akv.talentValues.Count)
                    addValue = akv.talentValues[lv - 1];
            }
            else
            {
                addValue = akv.isBaseOrStat
                    ? talent.GetStatValue(akv.talentValueName)
                    : talent.GetValue(akv.talentValueName);
            }

            return akv.calcType switch
            {
                TalentCalcType.PercentAdd => baseValue * (1 + addValue),
                TalentCalcType.LinearAdd => baseValue + addValue,
                _ => baseValue
            };
        }

        public double GetStatValue(string valueName)
        => GetStatValue(valueName, level);

        public double GetStatValue(string valueName, int lv)
        {
            if (lv <= 0 ||
                !statsKVDict.TryGetValue(valueName, out var stats) ||
                lv > stats.Count) return 0F;

            var baseValue = stats[lv - 1].finalValue;

            if (!abilityCfg.statsKV.TryGetValue(valueName, out var askv))
                return baseValue;

            var talent = owner.talentC.GetTalent(askv.talentName);
            if (talent == null || talent.isRelease)
                return baseValue;

            double addValue = 0;

            if (askv.isOverrideValue)
            {
                if (askv.talentValues != null && lv <= askv.talentValues.Count)
                    addValue = askv.talentValues[lv - 1];
            }
            else
            {
                addValue = askv.isBaseOrStat
                    ? talent.GetStatValue(askv.talentValueName)
                    : talent.GetValue(askv.talentValueName);
            }

            return askv.calcType switch
            {
                TalentCalcType.PercentAdd => baseValue * (1 + addValue),
                TalentCalcType.LinearAdd => baseValue + addValue,
                _ => baseValue
            };
        }

        public void ChangeStatBaseValue(string statsName, double value, bool isReplace)
        {
            if (statsKVDict.TryGetValue(statsName, out var akv))
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
                GameEvtBus.Send<EvtAbilityCooldownCalculate>(new(this, owner, currentCd));
            }

            var charge = GetCharge();
            if (IsEnableCharge())//&& currentCharge < charge
                currentCharge = (int)charge;

        }

        public void ModifyCurrentCooldown(double value)
        {
            if (value == 0)
                return;

            var chargeLimit = GetCharge(level);
            if (currentCharge > chargeLimit)
                currentCharge = chargeLimit;

            var enableCharge = IsEnableCharge();
            var hasChargeLogic = enableCharge && currentCharge < chargeLimit;

            if (enableCharge)
            {
                // 充能满了，没有正在恢复的 currentCd，不修改
                if (!hasChargeLogic)
                    return;

                currentCd = Math.Max(0, currentCd + value);
            }
            else
            {
                // 非充能技能已经冷却好了
                if (isCooldownReady)
                {
                    // 减少冷却没有意义
                    if (value < 0)
                        return;

                    // 增加冷却时，需要重新启动冷却
                    ResetCooldown(value);
                    return;
                }

                // 非充能技能正在冷却中，直接修改当前冷却
                currentCd = Math.Max(0, currentCd + value);
            }

            GameEvtBus.Send<EvtAbilityCooldownCalculate>(new(this, owner, currentCd));
        }
        public void ReduceCurrentCooldown(double value)
        {
            ModifyCurrentCooldown(-Math.Abs(value));
        }

        public void AddCurrentCooldown(double value)
        {
            ModifyCurrentCooldown(Math.Abs(value));
        }

        public void AddFrozenCooldown()
        => ++frozenCooldown;

        public void RemoveFrozenCooldown()
        {
            frozenCooldown = Math.Max(0, frozenCooldown - 1);
        }

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
            GameEvtBus.Send<EvtAbilityCooldownStart>(new EvtAbilityCooldownStart(this, owner, oldCooldown, currentCd));
        }
    }
}