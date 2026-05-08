namespace UnknownCreator.Modules
{
    public readonly struct EvtAbilityFullyCast : IBusEvent
    {
        public readonly AbilityBase ability;
        public readonly Unit owner;

        public EvtAbilityFullyCast(AbilityBase ability, Unit owner)
        {
            this.ability = ability;
            this.owner = owner;
        }
    }

}
