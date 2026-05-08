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

                if (Math.Abs(baseV - v) < 0.0001) return;

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

        public double minValue => customMinStats ? cntlr.GetStats(minName).finalValue : minV;
        public double maxValue => customMaxStats ? cntlr.GetStats(maxName).finalValue : maxV;

        private double baseV, bonusV, finalV, minV, maxV;
        private string minName, maxName;
        private bool customMinStats, customMaxStats, isLinking, isRoundToInt;

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
            // 同名 + 同计算类型，只保留一条 StatsCalc
            // 重复添加时只更新数值，并把归属 buff 切换成最新 buff
            // =========================================================
            if (!isStatsStacked && nameKeys.TryGetValue(nameKey, out var existByName))
            {
                bool changed = false;

                // 如果这次传进来的 buff 不是旧 buff，
                // 说明是同名 buff 重新刷新 / 替换了来源。
                // 这时要把 buffKeys 从旧 buff 改绑到新 buff。
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
                if (sc != null) Mgr.RPool.Release(sc);
            }

            bonusValue = 0;
            finalValue = baseValue;
        }

        // ========================= 核心计算 =========================

        private void CalcStatsValue()
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
                    var target = cntlr.GetStats(linkNames[i]);
                    linkedStats[i] = target;
                    if (target != null)
                    {
                        oldLinkedValues[i] = target.baseValue;
                        canChangeLinked[i] = target.canChangeValue;
                    }
                }
            }

            // ===== 数值计算 =====
            double value = baseValue;
            double linearAdd = 0;
            double percLinearSum = 0;
            double percNonlinearSum = 0;
            double constantValue = double.NaN;

            foreach (var calc in calcList)
            {
                switch (calc.calcType)
                {
                    case CalcType.Constant: constantValue = calc.value; break;
                    case CalcType.LinearAdd: linearAdd += calc.value; break;
                    case CalcType.PercLinearAdd: percLinearSum += calc.value; break;
                    case CalcType.PercNonlinearAdd: percNonlinearSum += calc.value; break;

                }
            }

            if (!double.IsNaN(constantValue))
            {
                value = constantValue;
            }
            else
            {
                value += linearAdd;
                value *= (100 + percLinearSum) / 100;
                value += (100 - value) * percNonlinearSum / 100;
            }

            double clamped = Math.Clamp(value, minValue, maxValue);
            double newFinalValue = isRoundToInt
                ? Math.Round(clamped, 0, MidpointRounding.AwayFromZero)
                : Math.Round(clamped, 2, MidpointRounding.AwayFromZero);

            // ===== 更新自身 =====
            finalValue = newFinalValue;
            bonusValue = finalValue - baseValue;

            GameEvtBus.Send<EvtStatChanged>(new(self, oldFinalValue, oldBonusValue, this));

            // ===== 联动修改统计 =====
            if (linkCount > 0 && !isLinking)
            {
                double delta = newFinalValue - oldFinalValue;
                if (Math.Abs(delta) > 0.0001)
                {
                    isLinking = true;

                    for (int i = 0; i < linkCount; i++)
                    {
                        var target = linkedStats[i];
                        if (target == null) continue;
                        if (target == this)
                        {
                            UCMDebug.LogWarning($"{idName} 不能联动自己");
                            continue;
                        }
                        if (!canChangeLinked[i])
                        {
                            UCMDebug.LogWarning($"{idName} 联动目标 {target.idName} 不允许修改");
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
            baseV = bonusV = 0;
            idName = string.Empty;
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