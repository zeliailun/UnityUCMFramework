namespace UnknownCreator.Modules
{
    public readonly struct EvtAbilityStart : IBusEvent
    {
        public readonly AbilityBase ability;
        public readonly Unit owner;

        public EvtAbilityStart(AbilityBase ability, Unit owner)
        {
            this.ability = ability;
            this.owner = owner;
        }
    }
}
