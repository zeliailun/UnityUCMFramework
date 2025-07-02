using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Unity.Properties;
using UnityEngine;
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
                if (bonusV != value)
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
                if (finalV != value)
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
        private bool customMinStats, customMaxStats;
        private UStatsComp cntlr;
        private Unit self;
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
            minName = cfg.minStatsName;
            maxName = cfg.maxStatsName;
            minV = cfg.minValue;
            maxV = cfg.maxValue;
            customMinStats = !string.IsNullOrWhiteSpace(minName);
            customMaxStats = !string.IsNullOrWhiteSpace(maxName);
            defaultValue = finalV = baseV = newV;
        }

        /// <summary>
        /// 添加或更新BUFF统计
        /// </summary>
        /// <param name="buff">BUFF对象</param>
        /// <param name="calcType">计算类型</param>
        /// <param name="value">数值</param>
        /// <param name="isStatsStacked">按名称处理(可堆叠BUFF)，按对象处理(不可堆叠BUFF)</param>
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

            if (sc != null && sc.value != value)
            {
                sc.value = value;
                CalcStatsValue();
            }
        }

        public void AddByName(string name, CalcType calcType, double value)
        {
            var key = new StatsKeyByName(name, calcType);
            if (!nameKeys.TryGetValue(key, out _))
            {
                var sd = Mgr.RPool.Load<StatsCalc>();
                sd.buff = null;
                sd.name = name;
                sd.calcType = calcType;
                sd.value = value;
                calcList.Add(sd);
                nameKeys.Add(key, sd);
                CalcStatsValue();
            }
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

        public bool Remove(string name, CalcType calcType)
        {
            var key = new StatsKeyByName(name, calcType);
            if (nameKeys.Remove(key, out var result))
            {
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

            StatsCalc sc;
            for (int i = calcList.Count - 1; i >= 0; i--)
            {
                sc = calcList[i];
                calcList.RemoveAt(i);
                if (sc != null) Mgr.RPool.Release(sc);
            }

            bonusValue = 0;
            finalValue = baseValue;
        }

        private void CalcStatsValue()
        {
            if (!canCalcValue)
            {
                finalValue = baseValue;
                return;
            }

            calcList.Sort(SortOrder);

            double oldValue = finalValue;
            double value = baseValue;
            double linearAdd = 0;
            double percLinearSum = 0;
            double percNonlinearSum = 0;

            StatsCalc calc;
            for (int i = 0; i < calcList.Count; i++)
            {
                calc = calcList[i];
                switch (calc.calcType)
                {
                    case CalcType.LinearAdd:
                        linearAdd += calc.value;
                        break;

                    case CalcType.PercLinearAdd:
                        percLinearSum += calc.value;
                        break;

                    case CalcType.PercNonlinearAdd:
                        percNonlinearSum += calc.value;
                        break;
                }
            }

            // 先加上所有常量加成
            value += linearAdd;

            // 再乘上线性百分比
            value *= (100 + percLinearSum) / 100;

            // 再加上非线性百分比（注意：基于未完成增长部分）
            value += (100 - value) * percNonlinearSum / 100;

            // 保留2位小数，限制范围
            finalValue = Math.Round(Math.Clamp(value, minValue, maxValue), 2, MidpointRounding.AwayFromZero);

            bonusValue = finalValue - baseValue;

            Mgr.Event.Send<EvtStatChanged>(new(self, oldValue, this), idName);


        }

        private int SortOrder(StatsCalc sc1, StatsCalc sc2)
        {

            if (sc1.order < sc2.order) return -1;

            if (sc1.order > sc2.order) return 1;

            return 0;
        }

        void IReference.ObjRelease()
        {
            Clear();
            baseV = bonusV = 0;
            cntlr = null;
            self = null;
        }
    }


    #region Key Structures

    public readonly struct StatsKeyByBuff : IEquatable<StatsKeyByBuff>
    {
        public readonly BuffBase buff;
        public readonly CalcType type;

        public StatsKeyByBuff(BuffBase buff, CalcType type)
            => (this.buff, this.type) = (buff, type);

        public bool Equals(StatsKeyByBuff other)
            => ReferenceEquals(buff, other.buff) && type == other.type;

        public override bool Equals(object obj)
            => obj is StatsKeyByBuff other && Equals(other);

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

        public override bool Equals(object obj)
            => obj is StatsKeyByName other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(name, (int)type);
    }

    public interface IStatsKey { }

    #endregion
}


