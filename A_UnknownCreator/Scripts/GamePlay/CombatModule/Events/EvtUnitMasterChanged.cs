namespace UnknownCreator.Modules
{
    public readonly struct EvtUnitMasterChanged : IBusEvent
    {
        public readonly Unit target;
        public readonly Unit oldMaster;
        public readonly Unit newMaster;

        public EvtUnitMasterChanged(Unit target, Unit oldMaster, Unit newMaster)
        {
            this.target = target;
            this.oldMaster = oldMaster;
            this.newMaster = newMaster;
        }
    }
}
