using System.Collections;
using UnityEngine;

namespace UnknownCreator.Modules
{
    public interface ILevelExp
    {
        int maxLevel { get; }
        int currentLevel { get; }
        int currentExp { get; }
        void SetLevel(int value);
        void SetFormula(IUnitExpBuilder expBuilder);
        void UpdateMaxLevelAndFormula(IUnitExpBuilder expBuilder, int value);
        void AddExp(int value);
        int GetExpBetweenLevel(int value);
        void ResetLevelExp();
    }
}