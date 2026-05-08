namespace UnknownCreator.Modules
{
    public readonly struct EvtUnitTeamChanged:IBusEvent
    {
        public readonly Unit target;
        public readonly int oldTeam;
        public readonly int newTeam;

        public EvtUnitTeamChanged(Unit target, int oldTeam, int newTeam)
        {
            this.target = target;
            this.oldTeam = oldTeam;
            this.newTeam = newTeam;

        }
    }
}
