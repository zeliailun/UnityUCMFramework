namespace UnknownCreator.Modules
{
    public readonly struct EvtAbilityAdded : IBusEvent
    {
        public readonly AbilityBase ability;
        public readonly Unit owner;

        public EvtAbilityAdded(AbilityBase ability, Unit owner)
        {
            this.ability = ability;
            this.owner = owner;
        }
    }
}
