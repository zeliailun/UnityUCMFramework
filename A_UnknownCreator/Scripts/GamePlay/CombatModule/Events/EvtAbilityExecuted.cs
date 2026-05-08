namespace UnknownCreator.Modules
{
    public readonly struct EvtAbilityExecuted : IBusEvent
    {
        public readonly AbilityBase ability;
        public readonly Unit owner;

        public EvtAbilityExecuted(AbilityBase ability, Unit owner)
        {
            this.ability = ability;
            this.owner = owner;
        }
    }
}
