using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Properties;
using UnityEngine.UIElements;

namespace UnknownCreator.Modules
{
    public sealed class StatData : IReference, INotifyBindablePropertyChanged
    {
        public object holder { private set; get; }
        public string idName { private set; get; }
        public bool canCalcValue { private set; get; }
        public bool canChangeValue { private set; get; }
        public double defaultValue { private set; get; }

        [CreateProperty]
        public double baseValue
        {
            get => baseV;
            set
            {
                if (!canChangeValue)
                {
                    UCMDebug.LogWarning(idName + ">该值被设定为不允许直接修改");
                    return;
                }

                var v = Math.Round(Math.Clamp(value, minValue, maxValue), 2, MidpointRounding.AwayFromZero);

                if (Math.Abs(baseV - v) < 0.0001)
                    return;

                baseV = v;
                CalcStatsValue();
                Notify();
            }
        }

        [CreateProperty]
        public double bonusValue
        {
            get => bonusV;
            private set
            {
                if (Math.Abs(bonusV - value) > 0.0001)
                {
                    bonusV = value;
                    Notify();
                }
            }
        }

        [CreateProperty]
        public double finalValue
        {
            get => finalV;
            private set
            {
                if (Math.Abs(finalV - value) > 0.0001)
                {
                    finalV = value;
                    Notify();
                }
            }
        }

        public double minValue
        {
            get
            {
                if (!customMinStats || cntlr == null)
                    return minV;

                StatData stat = cntlr.GetHolderStats(minName, holder);

                return stat?.finalValue ?? minV;
            }
        }

        public double maxValue
        {
            get
            {
                if (!customMaxStats || cntlr == null)
                    return maxV;

                StatData stat = cntlr.GetHolderStats(maxName, holder);

                return stat?.finalValue ?? maxV;
            }
        }

        private double baseV, bonusV, finalV, minV, maxV;
        private string minName, maxName;
        private bool customMinStats, customMaxStats, isLinking, isRoundToInt;

        // ========================= SoftCap 默认参数 =========================

        // PercSoftCap：百分比软上限。
        // 100 表示原始百分比加成 100% 以内不压缩，超过后开始递减收益。
        private const double PercSoftCapStart = 100d;

        // PercSoftCap 递减力度。
        // 0.35 = 压得轻
        // 0.45 = 中等，推荐
        // 0.65 = 压得重
        private const double PercSoftCapPower = 0.45d;

        // SoftCapAdd：数值软上限。
        // 50 表示原始数值加成 50 点以内不压缩，超过后开始递减收益。
        private const double SoftCapAddStart = 50d;

        // SoftCapAdd 递减力度。
        // 0.35 = 压得轻
        // 0.45 = 中等，推荐
        // 0.65 = 压得重
        private const double SoftCapAddPower = 0.45d;

        private UStatsComp cntlr;
        private Unit self;

        private List<string> linkNames = new();
        private List<StatsCalc> calcList = new();
        private List<BuffBase> buffList = new();

        private readonly Dictionary<StatsKeyByBuff, StatsCalc> buffKeys = new();
        private readonly Dictionary<StatsKeyByName, StatsCalc> nameKeys = new();

        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        void Notify([CallerMemberName] string property = "")
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }

        internal void Init(StatsCfg cfg, double newV, UStatsComp cntlr, Unit self, object holder)
        {
            this.cntlr = cntlr;
            this.self = self;
            this.holder = holder;

            idName = cfg.idName;
            canCalcValue = cfg.canCalcValue;
            canChangeValue = cfg.canChangeValue;
            isRoundToInt = cfg.isRoundToInt;

            minName = cfg.minStatsName;
            maxName = cfg.maxStatsName;

            minV = cfg.minValue;
            maxV = cfg.maxValue;

            linkNames.Clear();
            linkNames.AddRange(cfg.linkNames);

            customMinStats = !string.IsNullOrWhiteSpace(minName);
            customMaxStats = !string.IsNullOrWhiteSpace(maxName);

            defaultValue = finalV = baseV = newV;
        }

        // ========================= BUFF =========================

        public void AddByName(string name, CalcType calcType, double value)
        {
            var key = new StatsKeyByName(name, calcType);

            if (!nameKeys.TryGetValue(key, out var result))
            {
                var sd = Mgr.RPool.Load<StatsCalc>();
                sd.name = name;
                sd.calcType = calcType;
                sd.value = value;

                calcList.Add(sd);
                nameKeys.Add(key, sd);
            }
            else
            {
                result.value = value;
                result.calcType = calcType;
            }

            CalcStatsValue();
        }

        public void AddOrUpdateBuff(BuffBase buff, CalcType calcType, double value, bool isStatsStacked)
        {
            if (buff == null)
            {
                UCMDebug.LogWarning($"{idName}>添加属性修改失败，buff 为空");
                return;
            }

            string buffName = buff.buffName;
            var nameKey = new StatsKeyByName(buffName, calcType);

            // =========================================================
            // 不可堆叠：
            // 同名 + 同计算类型，只保留一条 StatsCalc。
            // 重复添加时只更新数值，并把归属 buff 切换成最新 buff。
            // =========================================================
            if (!isStatsStacked && nameKeys.TryGetValue(nameKey, out var existByName))
            {
                bool changed = false;

                if (!ReferenceEquals(existByName.buff, buff))
                {
                    if (existByName.buff != null)
                    {
                        var oldBuffKey = new StatsKeyByBuff(existByName.buff, calcType);
                        buffKeys.Remove(oldBuffKey);
                        buffList.Remove(existByName.buff);
                    }

                    existByName.buff = buff;
                    existByName.name = buffName;
                    existByName.calcType = calcType;

                    var newBuffKey = new StatsKeyByBuff(buff, calcType);
                    buffKeys[newBuffKey] = existByName;

                    if (!buffList.Contains(buff))
                        buffList.Add(buff);
                }

                if (Math.Abs(existByName.value - value) > 0.0001)
                {
                    existByName.value = value;
                    changed = true;
                }

                if (changed)
                    CalcStatsValue();

                return;
            }

            // =========================================================
            // 可堆叠：
            // 按 buff 实例 + 计算类型区分。
            // 同一个 buff 重复添加时更新数值。
            // 不同 buff 即使同名，也可以各自生效。
            // =========================================================
            var buffKey = new StatsKeyByBuff(buff, calcType);

            if (buffKeys.TryGetValue(buffKey, out var existByBuff))
            {
                if (Math.Abs(existByBuff.value - value) > 0.0001)
                {
                    existByBuff.value = value;
                    CalcStatsValue();
                }

                return;
            }

            var newCalc = Mgr.RPool.Load<StatsCalc>();
            newCalc.buff = buff;
            newCalc.name = buffName;
            newCalc.calcType = calcType;
            newCalc.value = value;

            calcList.Add(newCalc);
            buffList.Add(buff);

            buffKeys[buffKey] = newCalc;

            if (!isStatsStacked)
                nameKeys[nameKey] = newCalc;

            CalcStatsValue();
        }

        public bool Remove(BuffBase buff, CalcType calcType, bool isStatsStacked)
        {
            if (buff == null)
                return false;

            var buffKey = new StatsKeyByBuff(buff, calcType);

            if (!buffKeys.Remove(buffKey, out var result))
                return false;

            buffList.Remove(buff);

            var nameKey = new StatsKeyByName(buff.buffName, calcType);

            // 只在 nameKeys 当前指向的就是这条 StatsCalc 时才删除。
            // 避免误删 AddByName 或其他同名但不同来源的数据。
            if (nameKeys.TryGetValue(nameKey, out var existByName) &&
                ReferenceEquals(existByName, result))
            {
                nameKeys.Remove(nameKey);
            }

            calcList.Remove(result);
            Mgr.RPool.Release(result);

            CalcStatsValue();
            return true;
        }

        public void Clear()
        {
            buffKeys.Clear();
            nameKeys.Clear();
            buffList.Clear();

            for (int i = calcList.Count - 1; i >= 0; i--)
            {
                var sc = calcList[i];
                calcList.RemoveAt(i);

                if (sc != null)
                    Mgr.RPool.Release(sc);
            }

            bonusValue = 0;
            finalValue = baseValue;
        }

        // ========================= 核心计算 =========================

        /// <summary>
        /// 无限递减收益。
        ///
        /// softStart 以内不压缩。
        /// 超过 softStart 后开始递减。
        /// 不会有固定上限，可以无限叠加，只是越叠收益越低。
        /// </summary>
        private static double ApplyInfiniteDiminishing(double raw, double softStart, double power)
        {
            if (Math.Abs(raw) < 0.0001)
                return 0;

            double sign = Math.Sign(raw);
            double abs = Math.Abs(raw);

            if (abs <= softStart)
                return raw;

            double excess = abs - softStart;

            double effectiveExcess =
                excess / Math.Pow(1d + excess / softStart, power);

            return sign * (softStart + effectiveExcess);
        }

        internal void CalcStatsValue()
        {
            if (!canCalcValue)
            {
                finalValue = baseValue;
                bonusValue = 0;
                return;
            }

            double oldFinalValue = finalValue;
            double oldBonusValue = bonusValue;

            // ===== 记录联动目标状态 =====

            int linkCount = linkNames.Count;

            double[] oldLinkedValues = null;
            bool[] canChangeLinked = null;
            StatData[] linkedStats = null;

            if (linkCount > 0)
            {
                oldLinkedValues = new double[linkCount];
                canChangeLinked = new bool[linkCount];
                linkedStats = new StatData[linkCount];

                for (int i = 0; i < linkCount; i++)
                {
                    var target = cntlr.GetStat(linkNames[i]);

                    linkedStats[i] = target;

                    if (target != null)
                    {
                        oldLinkedValues[i] = target.baseValue;
                        canChangeLinked[i] = target.canChangeValue;
                    }
                }
            }

            // =========================================================
            // 收集所有属性修改
            // =========================================================

            double value = baseValue;

            double linearAdd = 0;
            double softCapAddSum = 0;

            double hyperbolicAddPositiveRemain = 1;
            double hyperbolicAddNegativeRemain = 1;

            double percLinearSum = 0;
            double percMulFactor = 1;

            double hyperbolicPositiveRemain = 1;
            double hyperbolicNegativeRemain = 1;

            double percSoftCapSum = 0;

            double constantValue = double.NaN;

            for (int i = calcList.Count - 1; i >= 0; i--)
            {
                StatsCalc calc = calcList[i];
                switch (calc.calcType)
                {
                    // =================================================
                    // 常量覆盖
                    // =================================================

                    case CalcType.Constant:
                        {
                            constantValue = calc.value;
                            break;
                        }

                    // =================================================
                    // 线性加法
                    // =================================================

                    case CalcType.LinearAdd:
                        {
                            linearAdd += calc.value;
                            break;
                        }

                    // =================================================
                    // 数值 SoftCap 加法
                    // 默认 0 的属性也能生效
                    // 可以无限叠加，但越叠收益越低
                    // =================================================

                    case CalcType.SoftCapAdd:
                        {
                            softCapAddSum += calc.value;
                            break;
                        }

                    // =================================================
                    // 双曲递减加法
                    // 默认 0 的属性也能生效
                    // 有效值趋近 100
                    // =================================================

                    case CalcType.HyperbolicAdd:
                        {
                            double p = Math.Clamp(calc.value / 100d, -0.9999d, 0.9999d);

                            if (p >= 0)
                                hyperbolicAddPositiveRemain *= 1 - p;
                            else
                                hyperbolicAddNegativeRemain *= 1 - Math.Abs(p);

                            break;
                        }

                    // =================================================
                    // 百分比线性加法
                    // 10% + 10% = 20%
                    // =================================================

                    case CalcType.PercLinearAdd:
                        {
                            percLinearSum += calc.value;
                            break;
                        }

                    // =================================================
                    // 百分比乘算
                    // 10% * 10% = 1.1 * 1.1
                    // =================================================

                    case CalcType.PercMul:
                        {
                            percMulFactor *= 1 + calc.value / 100d;
                            break;
                        }

                    // =================================================
                    // 百分比双曲递减
                    // 正数趋近 +100%
                    // 负数趋近 -100%
                    // 最后作为倍率乘到 value 上
                    // =================================================

                    case CalcType.PercHyperbolic:
                        {
                            double p = Math.Clamp(calc.value / 100d, -0.9999d, 0.9999d);

                            if (p >= 0)
                                hyperbolicPositiveRemain *= 1 - p;
                            else
                                hyperbolicNegativeRemain *= 1 - Math.Abs(p);

                            break;
                        }

                    // =================================================
                    // 百分比 SoftCap
                    // 作为百分比倍率乘到 value 上
                    // 可以无限叠加，但越叠收益越低
                    // =================================================

                    case CalcType.PercSoftCap:
                        {
                            percSoftCapSum += calc.value;
                            break;
                        }
                }
            }

            // =========================================================
            // Constant
            // =========================================================

            if (!double.IsNaN(constantValue))
            {
                // Constant 是绝对覆盖，不再吃其他加成。
                value = constantValue;
            }
            else
            {
                // =====================================================
                // 1. 线性加法
                // Base + LinearAdd
                // =====================================================

                value += linearAdd;

                // =====================================================
                // 2. 数值 SoftCap 加法
                //
                // 适合默认值为 0 的属性：
                // 暴击率、闪避率、吸血、冷却缩减等。
                //
                // 可以无限叠加，但越叠收益越低。
                // =====================================================

                if (Math.Abs(softCapAddSum) > 0.0001)
                {
                    double effective = ApplyInfiniteDiminishing(
                        softCapAddSum,
                        SoftCapAddStart,
                        SoftCapAddPower);

                    value += effective;
                }

                // =====================================================
                // 3. 双曲递减加法
                //
                // 适合默认值为 0，并且最终希望趋近 100 的属性：
                // 暴击率、闪避率、减伤率、冷却缩减、抗性等。
                //
                // 例如：
                // +10 和 +10 不是 +20，而是 +19。
                // =====================================================

                if (hyperbolicAddPositiveRemain < 1 || hyperbolicAddNegativeRemain < 1)
                {
                    double positivePercent = 1 - hyperbolicAddPositiveRemain;
                    double negativePercent = 1 - hyperbolicAddNegativeRemain;

                    double hyperbolicAddValue =
                        (positivePercent - negativePercent) * 100d;

                    value += hyperbolicAddValue;
                }

                // =====================================================
                // 4. 百分比线性加法
                // 多个百分比先相加，再统一乘算。
                // =====================================================

                if (Math.Abs(percLinearSum) > 0.0001)
                    value *= (100d + percLinearSum) / 100d;

                // =====================================================
                // 5. 百分比乘算
                // 每条百分比独立乘算。
                // =====================================================

                if (Math.Abs(percMulFactor - 1) > 0.0001)
                    value *= percMulFactor;

                // =====================================================
                // 6. 百分比双曲递减
                //
                // 正数有效收益趋近 +100%。
                // 负数有效收益趋近 -100%。
                //
                // 最后作为倍率乘到 value 上。
                // =====================================================

                if (hyperbolicPositiveRemain < 1 || hyperbolicNegativeRemain < 1)
                {
                    double positivePercent = 1 - hyperbolicPositiveRemain;
                    double negativePercent = 1 - hyperbolicNegativeRemain;

                    double hyperbolicPercent = positivePercent - negativePercent;

                    value *= 1 + hyperbolicPercent;
                }

                // =====================================================
                // 7. 百分比 SoftCap
                //
                // 适合攻击力%、移速%、范围%、射速% 等有基础值的属性。
                // 可以无限叠加，但越叠收益越低。
                //
                // 最后作为倍率乘到 value 上。
                // =====================================================

                if (Math.Abs(percSoftCapSum) > 0.0001)
                {
                    double effective = ApplyInfiniteDiminishing(
                        percSoftCapSum,
                        PercSoftCapStart,
                        PercSoftCapPower);

                    value *= (100d + effective) / 100d;
                }
            }

            // =========================================================
            // Clamp + Round
            // =========================================================

            double clamped = Math.Clamp(value, minValue, maxValue);

            double newFinalValue =isRoundToInt
                    ? Math.Round(clamped, 0, MidpointRounding.AwayFromZero)
                    : Math.Round(clamped, 2, MidpointRounding.AwayFromZero);


            if (idName == KeyGlobals.Stats.FiringInterval)
                UCMDebug.Log(cntlr.GetStat(minName));

            // =========================================================
            // 更新自身
            // =========================================================

            finalValue = newFinalValue;
            bonusValue = finalValue - baseValue;

            GameEvtBus.Send<EvtStatChanged>(new(self, oldFinalValue, oldBonusValue, this));

            // =========================================================
            // 联动修改
            // =========================================================

            if (linkCount > 0 && !isLinking)
            {
                double delta = newFinalValue - oldFinalValue;

                if (Math.Abs(delta) > 0.0001)
                {
                    isLinking = true;

                    for (int i = 0; i < linkCount; i++)
                    {
                        var target = linkedStats[i];

                        if (target == null)
                            continue;

                        if (target == this)
                        {
                            UCMDebug.LogWarning($"{idName} 不能联动自己");
                            continue;
                        }

                        if (!canChangeLinked[i])
                        {
                            UCMDebug.LogWarning(
                                $"{idName} 联动目标 {target.idName} 不允许修改");

                            continue;
                        }

                        double oldVal = oldLinkedValues[i];
                        double newVal = oldVal;

                        if (Math.Abs(oldFinalValue) > 0.0001)
                            newVal = oldVal * (newFinalValue / oldFinalValue);
                        else
                            newVal = oldVal + delta;

                        if (Math.Abs(newVal - oldVal) > 0.0001)
                            target.baseValue = newVal;
                    }

                    isLinking = false;
                }
            }
        }

        void IReference.ObjRelease()
        {
            Clear();

            baseV = 0;
            bonusV = 0;
            finalV = 0;
            minV = 0;
            maxV = 0;

            minName = string.Empty;
            maxName = string.Empty;
            idName = string.Empty;

            customMinStats = false;
            customMaxStats = false;
            isLinking = false;
            isRoundToInt = false;

            canCalcValue = false;
            canChangeValue = false;
            defaultValue = 0;

            linkNames.Clear();

            propertyChanged = null;
            holder = null;
            cntlr = null;
            self = null;
        }
    }

    // ========================= KEY =========================

    public readonly struct StatsKeyByBuff : IEquatable<StatsKeyByBuff>
    {
        public readonly BuffBase buff;
        public readonly CalcType type;

        public StatsKeyByBuff(BuffBase buff, CalcType type)
            => (this.buff, this.type) = (buff, type);

        public bool Equals(StatsKeyByBuff other)
            => ReferenceEquals(buff, other.buff) && type == other.type;

        public override int GetHashCode()
            => HashCode.Combine(RuntimeHelpers.GetHashCode(buff), (int)type);
    }

    public readonly struct StatsKeyByName : IEquatable<StatsKeyByName>
    {
        public readonly string name;
        public readonly CalcType type;

        public StatsKeyByName(string name, CalcType type)
            => (this.name, this.type) = (name, type);

        public bool Equals(StatsKeyByName other)
            => name == other.name && type == other.type;

        public override int GetHashCode()
            => HashCode.Combine(name, (int)type);
    }
}
