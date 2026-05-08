namespace UnknownCreator.Modules
{
    public readonly struct EvtStatWillUpdate:IBusEvent
    {

        public readonly Unit target;
        public readonly BuffBase buff;
        public readonly string statName;
        public readonly CalcType calcType;
        public readonly double value;
        public readonly bool isStatsStacked;

        public EvtStatWillUpdate(Unit target, BuffBase buff, string statsName, CalcType calcType, double value, bool isStatsStacked)
        {
            this.target = target;
            this.buff = buff;
            this.statName = statsName;
            this.calcType = calcType;
            this.value = value;
            this.isStatsStacked = isStatsStacked;
        }

    }
}

