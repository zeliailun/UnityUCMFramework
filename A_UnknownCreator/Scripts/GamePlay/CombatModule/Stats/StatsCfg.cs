using System;

namespace UnknownCreator.Modules
{
    [Serializable]
    public struct OverrideStats
    {
        public string baseCfgName;
        public double baseValue;
    }

    [Serializable]
    public class StatsCfg
    {
        [ReadOnly] public string idName;
        public bool canCalcValue = true;
        public bool canChangeValue = true;
        public string minStatsName;
        public double minValue;
        public string maxStatsName;
        public double maxValue;
    }
}