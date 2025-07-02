using System;
using System.Collections.Generic;

namespace UnknownCreator.Modules
{
    public sealed class ULevelExpComp : StateComp
    {
        private List<double> unitExpList = new();
        private List<double> expList => Mgr.Unit.isUseGlobalLevelExp ? Mgr.Unit.unitExpList : unitExpList;

        private int maxLv;
        public int maxLevel => Mgr.Unit.isUseGlobalLevelExp ? Mgr.Unit.unitMaxLevel : maxLv;

        public int currentLevel { private set; get; }
        public double currentExp { private set; get; }
        public bool isMaxLv => currentLevel >= maxLevel;

        private Unit self;

        public override void InitComp()
        {
            self = kv.GetValue<Unit>();
        }

        public override void ReleaseComp()
        {
            ResetLevelExp();
            self = null;
        }

        public void UpdateMaxLevelAndFormula(IUnitExpBuilder expBuilder, int value)
        {
            maxLv = value;
            unitExpList = expBuilder.ExpBuilder(maxLv, self);
        }

        public void SetFormula(IUnitExpBuilder expBuilder)
        {
            unitExpList = expBuilder.ExpBuilder(maxLv, self);
        }

        public void AddExp(double value)
        {
            if (value <= 0 || isMaxLv || !Mgr.Unit.FilterExpAdd((self, value)))
                return;

            double oldExp = currentExp;
            currentExp += value;

            Mgr.Event.Send(new EvtUnitExpAdded(self, oldExp, currentExp), CombatEvtGlobals.OnUnitExpAdded);


            while (currentLevel < maxLevel)
            {
                if (currentLevel >= expList.Count)
                    break;

                double requiredExp = expList[currentLevel];

                if (currentExp >= requiredExp)
                {
                    int oldLevel = currentLevel;
                    currentExp -= requiredExp;
                    currentLevel++;
                    Mgr.Event.Send(new EvtUnitUpgraded(self, oldLevel, currentLevel, currentExp,false), CombatEvtGlobals.OnUnitUpgraded);
                }
                else
                {
                    break;
                }
            }

            // 如果达到最大等级，清除多余经验
            if (currentLevel >= maxLevel)
            {
                currentExp = 0;
            }
        }

        public void Upgrade(int targetLevel)
        {
            targetLevel = Math.Clamp(targetLevel, 0, maxLevel);

            if (targetLevel <= currentLevel)
                return;

            int oldLevel = currentLevel;
            currentLevel = targetLevel;
            currentExp = 0;

            Mgr.Event.Send(new EvtUnitUpgraded(self, oldLevel, currentLevel, currentExp,true), CombatEvtGlobals.OnUnitUpgraded);
        }

        public void AddLevel(int value)
        {
            if (value <= 0 || isMaxLv)
                return;

            int targetLevel = Math.Min(currentLevel + value, maxLevel);
            Upgrade(targetLevel);
        }

        public double GetExpToLevel(int targetLevel)
        {
            if (targetLevel > maxLevel || targetLevel <= currentLevel)
                return 0;

            return expList[targetLevel - 1] - currentExp;
        }

        public void ResetLevelExp()
        {
            currentLevel = 0;
            currentExp = 0;
        }

    }
}
