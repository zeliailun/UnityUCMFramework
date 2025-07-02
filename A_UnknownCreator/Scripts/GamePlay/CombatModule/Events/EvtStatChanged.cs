namespace UnknownCreator.Modules
{
    public readonly struct EvtStatChanged
    {
        public readonly Unit target;
        public readonly double oldValue;
        public readonly StatData stat;

        public EvtStatChanged(Unit target, double oldValue, StatData stat)
        {
            this.target = target;
            this.oldValue = oldValue;
            this.stat = stat;
        }
    }
}
