namespace UnknownCreator.Modules
{
    public readonly struct EvtTalentChanged
    {
        public readonly Unit owner;
        public readonly AbilityBase talent;

        public EvtTalentChanged(Unit owner, AbilityBase talent)
        {
            this.owner = owner;
            this.talent = talent;
        }
    }
}
