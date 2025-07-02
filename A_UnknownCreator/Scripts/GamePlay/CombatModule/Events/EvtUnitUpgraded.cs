namespace UnknownCreator.Modules
{
    public readonly struct EvtUnitUpgraded
    {

        public readonly Unit target;
        public readonly int oldLv;
        public readonly int newLv;
        public readonly double currentXp;
        public readonly bool isDirectUpgrade;

        public EvtUnitUpgraded(Unit target, int oldLv, int newLv, double currentXp, bool isDirectUpgrade)
        {
            this.target = target;
            this.oldLv = oldLv;
            this.newLv = newLv;
            this.currentXp = currentXp;
            this.isDirectUpgrade = isDirectUpgrade;
        }

    }
}
