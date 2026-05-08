namespace UnknownCreator.Modules
{
    public readonly struct EvtStatChanged:IBusEvent
    {
        public readonly Unit target;
        public readonly double oldFinalValue;
        public readonly double oldBonusValue;
        public readonly StatData stat;

        public EvtStatChanged(Unit target, double oldFinalValue, double oldBonusValue,StatData stat)
        {
            this.target = target;
            this.oldFinalValue = oldFinalValue;
            this.oldBonusValue = oldBonusValue;
            this.stat = stat;
        }
    }
}
