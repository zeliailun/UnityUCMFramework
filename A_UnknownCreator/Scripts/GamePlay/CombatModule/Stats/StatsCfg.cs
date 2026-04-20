using System;
using System.Collections.Generic;

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
        public bool isRoundToInt = false;
        public string minStatsName;
        public double minValue;
        public string maxStatsName;
        public double maxValue;

        public List<string> linkNames = new();
    }
}