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

        public void AddOrUpdateBuff(BuffBase buff, CalcType calcType, double value, bool isStatsStacked)
        {
            StatsCalc sc = null;

            var keyName = new StatsKeyByName(buff.buffName, calcType);

            if (!isStatsStacked && nameKeys.TryGetValue(keyName, out var result1))
            {
                sc = result1;
            }
            else
            {
                var keyBuff = new StatsKeyByBuff(buff, calcType);

                if (!buffKeys.TryGetValue(keyBuff, out var result2))
                {
                    var newCalc = Mgr.RPool.Load<StatsCalc>();
                    newCalc.buff = buff;
                    newCalc.name = buff.buffName;
                    newCalc.calcType = calcType;
                    newCalc.value = value;

                    calcList.Add(newCalc);
                    buffList.Add(buff);

                    nameKeys[keyName] = newCalc;
                    buffKeys[keyBuff] = newCalc;

                    CalcStatsValue();
                }
                else
                {
                    sc = result2;
                }
            }

            if (sc != null && Math.Abs(sc.value - value) > 0.0001)
            {
                sc.value = value;
                CalcStatsValue();
            }
        }

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

        public bool Remove(BuffBase buff, CalcType calcType, bool isStatsStacked)
        {
            var key = new StatsKeyByBuff(buff, calcType);

            if (buffKeys.Remove(key, out var result))
            {
                buffList.Remove(buff);

                if (!isStatsStacked || buffList.Count < 1)
                    nameKeys.Remove(new StatsKeyByName(buff.buffName, calcType));

                calcList.Remove(result);
                Mgr.RPool.Release(result);

                CalcStatsValue();
                return true;
            }
            return false;
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

            calcList.Sort((a, b) => a.order.CompareTo(b.order));

            double oldFinalValue = finalValue;

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

            Mgr.Event.Send<EvtStatChanged>(new(self, oldFinalValue, this), UCMGE.OnStatChanged);

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