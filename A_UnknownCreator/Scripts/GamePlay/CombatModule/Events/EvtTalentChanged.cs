namespace UnknownCreator.Modules
{
    public readonly struct EvtTalentChanged
    {
        public readonly Unit owner;
        public readonly string talentName;

        public EvtTalentChanged(Unit owner, string talentName)
        {
            this.owner = owner;
            this.talentName = talentName;
        }
    }
}
