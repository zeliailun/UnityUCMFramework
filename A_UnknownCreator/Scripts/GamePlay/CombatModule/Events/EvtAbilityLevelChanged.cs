namespace UnknownCreator.Modules
{
    public readonly struct EvtAbilityLevelChanged
    {
        public readonly AbilityBase ability;
        public readonly Unit owner;
        public readonly int oldLv;
        public readonly int newLv;

        public EvtAbilityLevelChanged(AbilityBase ability, Unit owner, int oldLv, int newLv)
        {
            this.ability = ability;
            this.owner = owner;
            this.newLv = newLv;
            this.oldLv = oldLv;
        }
    }
}
