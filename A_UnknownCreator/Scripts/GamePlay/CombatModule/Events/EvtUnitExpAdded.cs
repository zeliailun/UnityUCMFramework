namespace UnknownCreator.Modules
{
    public readonly struct EvtUnitExpAdded
    {
        public readonly Unit target;
        public readonly double oldExp;
        public readonly double currentExp;

        public EvtUnitExpAdded(Unit target, double oldExp, double currentExp)
        {
            this.target = target;
            this.oldExp = oldExp;
            this.currentExp = currentExp;
        }
    }
}
