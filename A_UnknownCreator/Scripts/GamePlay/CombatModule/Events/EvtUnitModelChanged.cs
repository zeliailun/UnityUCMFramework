namespace UnknownCreator.Modules
{
    public struct EvtUnitModelChanged
    {
        public string oldName;
        public string newName;
        public Unit target;

        public EvtUnitModelChanged(string oldName, string newName, Unit target)
        {
            this.oldName = oldName;
            this.newName = newName;
            this.target = target;
        }
    }
}