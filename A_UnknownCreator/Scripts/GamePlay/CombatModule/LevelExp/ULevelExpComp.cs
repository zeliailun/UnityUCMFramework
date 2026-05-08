using System;
using System.Collections.Generic;

namespace UnknownCreator.Modules
{
    [Serializable]
    public sealed class ULevelExpComp : StateComp
    {
        private List<double> unitExpList = new();
        private IReadOnlyList<double> expList => Mgr.Unit.isUseGlobalLevelExp ? Mgr.Unit.unitExpList : unitExpList;

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
            if (Mgr.Unit.isUseGlobalLevelExp)
                return;

            if (expBuilder == null)
                return;

            maxLv = Math.Max(0, value);
            unitExpList = expBuilder.ExpBuilder(maxLv, self);
        }

        public void SetFormula(IUnitExpBuilder expBuilder)
        {
            if (expBuilder == null)
                return;

            if (maxLv <= 0)
                return;

            unitExpList = expBuilder.ExpBuilder(maxLv, self);
        }

        public void AddExp(double value)
        {
            if (value <= 0 || isMaxLv || !Mgr.Unit.FilterExpAdd((self, value)))
                return;

            double oldExp = currentExp;
            currentExp += value;

            GameEvtBus.Send<EvtUnitExpAdded>(new(self, oldExp, currentExp));

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
                    GameEvtBus.Send<EvtUnitUpgraded>(new(self, oldLevel, currentLevel, currentExp, false));
                }
                else
                {
                    break;
                }
            }

            // 如果达到最大等级，清除多余经验
            if (currentLevel >= maxLevel) currentExp = 0;
        }

        public void Upgrade(int targetLevel)
        {
            targetLevel = Math.Clamp(targetLevel, 0, maxLevel);

            if (targetLevel <= currentLevel)
                return;

            int oldLevel = currentLevel;
            currentLevel = targetLevel;
            currentExp = 0;

            GameEvtBus.Send<EvtUnitUpgraded>(new(self, oldLevel, currentLevel, currentExp, true));
        }

        public void AddLevel(int value)
        {
            if (value <= 0 || isMaxLv)
                return;

            int targetLevel = Math.Min(currentLevel + value, maxLevel);
            Upgrade(targetLevel);
        }

        public double GetExpToNextLevel()
        {
            if (isMaxLv)
                return 0;

            if (expList == null || expList.Count == 0)
                return 0;

            if (currentLevel < 0 || currentLevel >= expList.Count)
                return 0;

            return Math.Max(0, expList[currentLevel] - currentExp);
        }

        public double GetTotalExpToLevel(int targetLevel)
        {
            if (targetLevel <= currentLevel || targetLevel > maxLevel)
                return 0;

            if (expList == null || expList.Count == 0)
                return 0;

            if (targetLevel > expList.Count)
            {
                UCMDebug.LogWarning("超出了最大等级默认返回0");
                return 0;
            }

            double total = 0;

            for (int level = currentLevel; level < targetLevel; level++)
            {
                total += expList[level];
            }

            return Math.Max(0, total - currentExp);
        }

        public void ResetLevelExp()
        {
            currentLevel = 0;
            currentExp = 0;
        }

    }
}
