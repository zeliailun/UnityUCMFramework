namespace UnknownCreator.Modules
{
    public readonly struct EvtStateUpdated:IBusEvent
    {
        public readonly Unit target;
        public readonly int typeID;
        public readonly bool isState;

        public EvtStateUpdated(Unit target, int typeID, bool isState)
        {
            this.target = target;
            this.typeID = typeID;
            this.isState = isState;
        }
    }
}